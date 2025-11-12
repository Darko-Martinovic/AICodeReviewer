import React, { useState } from "react";
import { trainingApi } from "../services/api";
import styles from "./AITrainer.module.css";

interface ValidationResult {
  isValid: boolean;
  wrappedCode: string;
  originalCode: string;
  errors: string[];
  wasWrapped: boolean;
  language: string;
}

interface ReviewResult {
  validation: ValidationResult;
  review: {
    reviewedFiles: string[];
    allIssues: string[];
    detailedIssues: Array<{
      fileName: string;
      category: string;
      severity: string;
      title: string;
      description: string;
      recommendation: string;
      lineNumber: number | null;
      codeSnippet: string;
    }>;
    metrics: {
      startTime: string;
      endTime: string;
      duration: string;
      filesReviewed: number;
      issuesFound: number;
      totalLinesOfCode: number;
      tokensUsed: number;
      inputTokens: number;
      outputTokens: number;
      estimatedCost: number;
      reviewType: string;
    };
    issueCount: number;
    hasIssues: boolean;
  };
}

interface PromptSuggestion {
  language: string;
  feedbackType: string;
  suggestedAddition: string;
  currentCustomPrompt: string;
  reasoning: string;
}

const LANGUAGES = [
  { value: "csharp", label: "C#" },
  { value: "vbnet", label: "VB.NET" },
  { value: "java", label: "Java" },
  { value: "javascript", label: "JavaScript" },
  { value: "typescript", label: "TypeScript" },
  { value: "react", label: "React/TSX" },
  { value: "tsql", label: "T-SQL" },
];

const CODE_EXAMPLES: Record<string, { snippet: string; complete: string }> = {
  csharp: {
    snippet: `// Simple snippet - will be auto-wrapped
Console.WriteLine("Hello World");
var numbers = new[] { 1, 2, 3, 4, 5 };
var sum = numbers.Sum();`,
    complete: `// Complete program
using System;
using System.Linq;

namespace TrainingExample
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World");
            var numbers = new[] { 1, 2, 3, 4, 5 };
            var sum = numbers.Sum();
            Console.WriteLine($"Sum: {sum}");
        }
    }
}`,
  },
  vbnet: {
    snippet: `' Simple snippet - will be auto-wrapped
Console.WriteLine("Hello World")
Dim numbers As Integer() = {1, 2, 3, 4, 5}
Dim sum As Integer = numbers.Sum()`,
    complete: `' Complete program
Imports System
Imports System.Linq

Module Program
    Sub Main(args As String())
        Console.WriteLine("Hello World")
        Dim numbers As Integer() = {1, 2, 3, 4, 5}
        Dim sum As Integer = numbers.Sum()
        Console.WriteLine("Sum: " & sum.ToString())
    End Sub
End Module`,
  },
  java: {
    snippet: `// Simple snippet - will be auto-wrapped
System.out.println("Hello World");
int[] numbers = {1, 2, 3, 4, 5};
int sum = Arrays.stream(numbers).sum();`,
    complete: `// Complete Java program
import java.util.Arrays;

public class HelloWorld {
    public static void main(String[] args) {
        System.out.println("Hello World");
        int[] numbers = {1, 2, 3, 4, 5};
        int sum = Arrays.stream(numbers).sum();
        System.out.println("Sum: " + sum);
    }
}`,
  },
  javascript: {
    snippet: `// Simple snippet
console.log("Hello World");
const numbers = [1, 2, 3, 4, 5];
const sum = numbers.reduce((a, b) => a + b, 0);`,
    complete: `// Complete JavaScript with validation
'use strict';

function calculateSum(numbers) {
  if (!Array.isArray(numbers)) {
    throw new Error("Input must be an array");
  }
  return numbers.reduce((a, b) => a + b, 0);
}

const numbers = [1, 2, 3, 4, 5];
const sum = calculateSum(numbers);
console.log(\`Sum: \${sum}\`);`,
  },
  typescript: {
    snippet: `// Simple snippet
const greet = (name: string) => {
  console.log(\`Hello, \${name}!\`);
};
greet("World");`,
    complete: `// Complete TypeScript with types
interface User {
  id: number;
  name: string;
  email: string;
}

function greetUser(user: User): string {
  return \`Hello, \${user.name}!\`;
}

const user: User = {
  id: 1,
  name: "John Doe",
  email: "john@example.com"
};

console.log(greetUser(user));`,
  },
  react: {
    snippet: `// Simple component snippet
const Greeting = ({ name }) => {
  return <h1>Hello, {name}!</h1>;
};`,
    complete: `// Complete React component with TypeScript
import React, { useState } from 'react';

interface GreetingProps {
  name: string;
}

export const Greeting: React.FC<GreetingProps> = ({ name }) => {
  const [count, setCount] = useState<number>(0);

  const handleClick = () => {
    setCount(prevCount => prevCount + 1);
  };

  return (
    <div className="greeting-container">
      <h1>Hello, {name}!</h1>
      <p>You clicked {count} times</p>
      <button onClick={handleClick} type="button">
        Click me
      </button>
    </div>
  );
};

export default Greeting;`,
  },
  tsql: {
    snippet: `-- Simple query snippet
SELECT TOP 10 
    CustomerID, 
    OrderDate, 
    TotalAmount
FROM Orders
WHERE OrderDate > '2024-01-01'
ORDER BY TotalAmount DESC;`,
    complete: `-- Complete stored procedure
CREATE PROCEDURE GetTopCustomerOrders
    @StartDate DATE,
    @TopCount INT = 10
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        SELECT TOP (@TopCount)
            c.CustomerID,
            c.CustomerName,
            o.OrderDate,
            o.TotalAmount
        FROM Orders o
        INNER JOIN Customers c ON o.CustomerID = c.CustomerID
        WHERE o.OrderDate >= @StartDate
        ORDER BY o.TotalAmount DESC;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END;`,
  },
};

export const AITrainer: React.FC = () => {
  const [selectedLanguage, setSelectedLanguage] = useState("csharp");
  const [code, setCode] = useState("");
  const [isValidating, setIsValidating] = useState(false);
  const [isReviewing, setIsReviewing] = useState(false);
  const [validationResult, setValidationResult] =
    useState<ValidationResult | null>(null);
  const [reviewResult, setReviewResult] = useState<ReviewResult | null>(null);
  const [showFeedback, setShowFeedback] = useState(false);
  const [promptSuggestion, setPromptSuggestion] =
    useState<PromptSuggestion | null>(null);
  const [showConfirmation, setShowConfirmation] = useState(false);
  const [isUpdatingPrompt, setIsUpdatingPrompt] = useState(false);
  const [showExamples, setShowExamples] = useState(false);
  const [isLeftPanelCollapsed, setIsLeftPanelCollapsed] = useState(false);

  const loadExample = (exampleType: "snippet" | "complete") => {
    const example = CODE_EXAMPLES[selectedLanguage];
    if (example) {
      setCode(example[exampleType]);
      setShowExamples(false);
      // Clear previous results
      setValidationResult(null);
      setReviewResult(null);
      setShowFeedback(false);
      setPromptSuggestion(null);
    }
  };

  const handleValidateCode = async () => {
    if (!code.trim()) {
      alert("Please enter some code to validate");
      return;
    }

    setIsValidating(true);
    setValidationResult(null);
    setReviewResult(null);
    setShowFeedback(false);
    setPromptSuggestion(null);

    try {
      const result = await trainingApi.validateCode(code, selectedLanguage);
      setValidationResult(result.data);
    } catch (error) {
      console.error("Validation error:", error);
      alert("Failed to validate code. Please try again.");
    } finally {
      setIsValidating(false);
    }
  };

  const handleReviewCode = async (reviewAnyway = false) => {
    if (!code.trim()) {
      alert("Please enter some code to review");
      return;
    }

    setIsReviewing(true);
    setReviewResult(null);
    setShowFeedback(false);
    setPromptSuggestion(null);

    try {
      const result = await trainingApi.reviewCode(
        code,
        selectedLanguage,
        reviewAnyway
      );
      setReviewResult(result.data);
      setShowFeedback(true);
      // Collapse left panel after successful review
      setIsLeftPanelCollapsed(true);
    } catch (error) {
      console.error("Review error:", error);

      if (
        (
          error as {
            response?: {
              status?: number;
              data?: { validation?: ValidationResult };
            };
          }
        ).response?.status === 400 &&
        (error as { response?: { data?: { validation?: ValidationResult } } })
          .response?.data?.validation
      ) {
        setValidationResult(
          (error as { response: { data: { validation: ValidationResult } } })
            .response.data.validation
        );
        const shouldReview = window.confirm(
          "Code validation failed. Do you want to review it anyway?"
        );
        if (shouldReview) {
          handleReviewCode(true);
        }
      } else {
        alert("Failed to review code. Please try again.");
      }
    } finally {
      setIsReviewing(false);
    }
  };

  const handleFeedback = async (feedbackType: string) => {
    if (!reviewResult) return;

    try {
      // Generate a summary from the review data
      const summary = `Found ${
        reviewResult.review.metrics.issuesFound
      } issues. ${
        reviewResult.review.detailedIssues.length > 0
          ? reviewResult.review.detailedIssues
              .map((i) => `${i.severity}: ${i.title}`)
              .join("; ")
          : "No detailed issues."
      }`;

      const result = await trainingApi.suggestPromptImprovement(
        selectedLanguage,
        feedbackType,
        code,
        summary
      );

      setPromptSuggestion(result.data);
      setShowConfirmation(true);
    } catch (error) {
      console.error("Error getting prompt suggestion:", error);
      alert("Failed to generate prompt suggestion. Please try again.");
    }
  };

  const handleConfirmPromptUpdate = async () => {
    if (!promptSuggestion) return;

    const firstConfirm = window.confirm(
      `Are you sure you want to add this to the ${promptSuggestion.language} custom prompt?\n\n"${promptSuggestion.suggestedAddition}"`
    );

    if (!firstConfirm) return;

    const secondConfirm = window.confirm(
      "This will permanently modify the custom prompt. Are you absolutely sure?"
    );

    if (!secondConfirm) return;

    setIsUpdatingPrompt(true);

    try {
      await trainingApi.updateCustomPrompt(
        selectedLanguage,
        promptSuggestion.suggestedAddition
      );
      alert("Custom prompt updated successfully!");
      setShowConfirmation(false);
      setPromptSuggestion(null);
      setShowFeedback(false);
      setReviewResult(null);
    } catch (error) {
      console.error("Error updating prompt:", error);
      alert("Failed to update custom prompt. Please try again.");
    } finally {
      setIsUpdatingPrompt(false);
    }
  };

  const handleCancelPromptUpdate = () => {
    setShowConfirmation(false);
    setPromptSuggestion(null);
  };

  const getSeverityColor = (severity: string) => {
    switch (severity.toLowerCase()) {
      case "critical":
        return "#dc2626";
      case "high":
        return "#ea580c";
      case "medium":
        return "#f59e0b";
      case "low":
        return "#84cc16";
      default:
        return "#6b7280";
    }
  };

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <h1 className={styles.title}>Train AI Code Reviewer</h1>
        <p className={styles.subtitle}>
          Test AI behavior, provide feedback, and improve custom prompts
        </p>
      </div>

      <div className={styles.content}>
        {/* Left Panel - Code Input */}
        <div className={`${styles.leftPanel} ${isLeftPanelCollapsed ? styles.collapsed : ''}`}>
          {isLeftPanelCollapsed && (
            <button 
              onClick={() => setIsLeftPanelCollapsed(false)}
              className={styles.expandButton}
              title="Expand code panel"
            >
              ⬅️ Show Code
            </button>
          )}
          <div className={styles.controls}>
            <div className={styles.languageSelector}>
              <label htmlFor="language" className={styles.label}>
                Programming Language
              </label>
              <select
                id="language"
                value={selectedLanguage}
                onChange={(e) => {
                  setSelectedLanguage(e.target.value);
                  setShowExamples(false);
                }}
                className={styles.select}
              >
                {LANGUAGES.map((lang) => (
                  <option key={lang.value} value={lang.value}>
                    {lang.label}
                  </option>
                ))}
              </select>
            </div>

            <div className={styles.buttonGroup}>
              <button
                onClick={() => setShowExamples(!showExamples)}
                className={styles.examplesButton}
                title="Show code examples for selected language"
              >
                {showExamples ? "Hide Examples" : "📘 Show Examples"}
              </button>
              <button
                onClick={handleValidateCode}
                disabled={isValidating || !code.trim()}
                className={styles.validateButton}
              >
                {isValidating ? "Validating..." : "Validate Code"}
              </button>
              <button
                onClick={() => handleReviewCode(false)}
                disabled={isReviewing || !code.trim()}
                className={styles.reviewButton}
              >
                {isReviewing ? "Reviewing..." : "Review Code"}
              </button>
            </div>
          </div>

          {showExamples && (
            <div className={styles.examplesPanel}>
              <h3 className={styles.examplesTitle}>
                Code Examples for{" "}
                {LANGUAGES.find((l) => l.value === selectedLanguage)?.label}
              </h3>
              <p className={styles.examplesDescription}>
                Click an example to load it into the editor:
              </p>
              <div className={styles.exampleButtons}>
                <button
                  onClick={() => loadExample("snippet")}
                  className={styles.exampleButton}
                >
                  <strong>Simple Snippet</strong>
                  <span className={styles.exampleHint}>
                    Will be auto-wrapped into complete code
                  </span>
                </button>
                <button
                  onClick={() => loadExample("complete")}
                  className={styles.exampleButton}
                >
                  <strong>Complete Code</strong>
                  <span className={styles.exampleHint}>
                    Ready-to-compile full example
                  </span>
                </button>
              </div>
              <div className={styles.examplePreview}>
                <div className={styles.exampleSection}>
                  <h4>Snippet Preview:</h4>
                  <pre className={styles.exampleCode}>
                    {CODE_EXAMPLES[selectedLanguage]?.snippet}
                  </pre>
                </div>
                <div className={styles.exampleSection}>
                  <h4>Complete Preview:</h4>
                  <pre className={styles.exampleCode}>
                    {CODE_EXAMPLES[selectedLanguage]?.complete}
                  </pre>
                </div>
              </div>
            </div>
          )}

          <div className={styles.editorContainer}>
            <textarea
              className={styles.codeEditor}
              value={code}
              onChange={(e) => setCode(e.target.value)}
              placeholder={`Paste your ${
                LANGUAGES.find((l) => l.value === selectedLanguage)?.label
              } code here...`}
              spellCheck={false}
            />
          </div>

          {validationResult && (
            <div
              className={`${styles.validationResult} ${
                validationResult.isValid ? styles.valid : styles.invalid
              }`}
            >
              <h3>
                {validationResult.isValid ? "✅ Valid" : "❌ Invalid"} Code
              </h3>
              {validationResult.wasWrapped && (
                <p className={styles.wrappedNotice}>
                  ℹ️ Code was automatically wrapped for compilation
                </p>
              )}
              {validationResult.errors.length > 0 && (
                <div className={styles.errors}>
                  <strong>Errors:</strong>
                  <ul>
                    {validationResult.errors.map((error, index) => (
                      <li key={index}>{error}</li>
                    ))}
                  </ul>
                </div>
              )}
            </div>
          )}
        </div>

        {/* Right Panel - Review Results */}
        {reviewResult && (
          <div className={`${styles.rightPanel} ${isLeftPanelCollapsed ? styles.expanded : ''}`}>
            <div className={styles.reviewResults}>
              <h2 className={styles.reviewTitle}>Review Results</h2>

              {reviewResult.review.metrics && (
                <div className={styles.metrics}>
                  <div className={styles.metricItem}>
                    <span className={styles.metricLabel}>Total Issues:</span>
                    <span className={styles.metricValue}>
                      {reviewResult.review.metrics.issuesFound}
                    </span>
                  </div>
                  <div className={styles.metricItem}>
                    <span className={styles.metricLabel}>Critical:</span>
                    <span
                      className={styles.metricValue}
                      style={{ color: "#dc2626" }}
                    >
                      {
                        reviewResult.review.detailedIssues.filter(
                          (i) => i.severity === "Critical"
                        ).length
                      }
                    </span>
                  </div>
                  <div className={styles.metricItem}>
                    <span className={styles.metricLabel}>Warnings:</span>
                    <span
                      className={styles.metricValue}
                      style={{ color: "#f59e0b" }}
                    >
                      {
                        reviewResult.review.detailedIssues.filter(
                          (i) =>
                            i.severity === "Major" || i.severity === "Minor"
                        ).length
                      }
                    </span>
                  </div>
                  <div className={styles.metricItem}>
                    <span className={styles.metricLabel}>Tokens Used:</span>
                    <span className={styles.metricValue}>
                      {reviewResult.review.metrics.tokensUsed.toLocaleString()}
                    </span>
                  </div>
                  <div className={styles.metricItem}>
                    <span className={styles.metricLabel}>Cost:</span>
                    <span className={styles.metricValue}>
                      ${reviewResult.review.metrics.estimatedCost.toFixed(4)}
                    </span>
                  </div>
                </div>
              )}

              <div className={styles.summary}>
                <h3>Summary</h3>
                <p>
                  Review completed in {reviewResult.review.metrics.duration}.
                  Found {reviewResult.review.metrics.issuesFound} issue(s)
                  across {reviewResult.review.metrics.filesReviewed} file(s).
                </p>
              </div>

              {reviewResult.review.detailedIssues &&
                reviewResult.review.detailedIssues.length > 0 && (
                  <div className={styles.issues}>
                    <h3>Issues Found</h3>
                    {reviewResult.review.detailedIssues.map((issue, index) => (
                      <div key={index} className={styles.issue}>
                        <div className={styles.issueHeader}>
                          <span
                            className={styles.severity}
                            style={{
                              backgroundColor: getSeverityColor(issue.severity),
                            }}
                          >
                            {issue.severity}
                          </span>
                          <span className={styles.category}>
                            {issue.category}
                          </span>
                          {issue.lineNumber && issue.lineNumber > 0 && (
                            <span className={styles.lineNumber}>
                              Line {issue.lineNumber}
                            </span>
                          )}
                        </div>
                        <h4 className={styles.issueTitle}>{issue.title}</h4>
                        <p className={styles.issueDescription}>
                          {issue.description}
                        </p>
                        {issue.recommendation && (
                          <div className={styles.issueRecommendation}>
                            <strong>💡 Recommendation:</strong>
                            <p>{issue.recommendation}</p>
                          </div>
                        )}
                      </div>
                    ))}
                  </div>
                )}

            </div>

            {/* Sticky Action Bar at Bottom */}
            {showFeedback && !promptSuggestion && (
              <div className={styles.stickyActionBar}>
                <div className={styles.feedbackSection}>
                  <h3>How was the AI's review?</h3>
                  <div className={styles.feedbackButtons}>
                    <button
                      onClick={() => handleFeedback("too strict")}
                      className={styles.feedbackButton}
                    >
                      😤 Too Strict
                    </button>
                    <button
                      onClick={() => handleFeedback("too lenient")}
                      className={styles.feedbackButton}
                    >
                      😊 Too Lenient
                    </button>
                    <button
                      onClick={() => handleFeedback("too generous")}
                      className={styles.feedbackButton}
                    >
                      🎁 Too Generous
                    </button>
                  </div>
                </div>
              </div>
            )}

            {promptSuggestion && showConfirmation && (
              <div className={styles.stickyActionBar}>
                <div className={styles.promptSuggestion}>
                  <h3>Suggested Prompt Addition</h3>
                  <div className={styles.suggestionContent}>
                    <p className={styles.reasoning}>
                      {promptSuggestion.reasoning}
                    </p>
                    <div className={styles.suggestionBox}>
                      <strong>Suggested Addition:</strong>
                      <p>{promptSuggestion.suggestedAddition}</p>
                    </div>
                    {promptSuggestion.currentCustomPrompt && (
                      <div className={styles.currentPrompt}>
                        <strong>Current Custom Prompt:</strong>
                        <p>{promptSuggestion.currentCustomPrompt}</p>
                      </div>
                    )}
                  </div>
                  <div className={styles.confirmationButtons}>
                    <button
                      onClick={handleConfirmPromptUpdate}
                      disabled={isUpdatingPrompt}
                      className={styles.confirmButton}
                    >
                      {isUpdatingPrompt
                        ? "Updating..."
                        : "✅ Add to Custom Prompt"}
                    </button>
                    <button
                      onClick={handleCancelPromptUpdate}
                      disabled={isUpdatingPrompt}
                      className={styles.cancelButton}
                    >
                      ❌ Cancel
                    </button>
                  </div>
                </div>
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
};
