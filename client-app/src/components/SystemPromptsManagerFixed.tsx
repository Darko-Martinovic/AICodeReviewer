import React, { useState, useEffect } from "react";
import { Modal, Alert } from "./UI";
import { Copy, Check } from "lucide-react";
import styles from "./SystemPromptsManagerFixed.module.css";

interface SystemPrompt {
  language: string;
  systemPrompt: string;
  customAdditions: string;
  lastModified: string;
}

interface PreviewData {
  combinedPrompt: string;
  previewGeneratedAt: string;
}

interface Templates {
  [category: string]: string[];
}

const SystemPromptsManager: React.FC = () => {
  const [prompts, setPrompts] = useState<{ [key: string]: SystemPrompt }>({});
  const [templates, setTemplates] = useState<Templates | null>(null);
  const [activeLanguage, setActiveLanguage] = useState<string>("CSharp");
  const [loading, setLoading] = useState<boolean>(false);
  const [saving, setSaving] = useState<string | null>(null);
  const [previewMode, setPreviewMode] = useState<{ [key: string]: boolean }>(
    {}
  );
  const [previewData, setPreviewData] = useState<{
    [key: string]: PreviewData;
  }>({});

  // Copy to clipboard state
  const [copiedField, setCopiedField] = useState<string | null>(null);

  // Add Language Modal state
  const [showAddLanguageModal, setShowAddLanguageModal] =
    useState<boolean>(false);
  const [newLanguageForm, setNewLanguageForm] = useState({
    name: "",
    key: "",
    icon: "",
  });

  const languages = [
    { key: "CSharp", label: "C#", icon: "🟢" },
    { key: "VbNet", label: "VB.NET", icon: "🔵" },
    { key: "Sql", label: "T-SQL", icon: "🗄️" },
    { key: "JavaScript", label: "JavaScript", icon: "🟨" },
    { key: "TypeScript", label: "TypeScript", icon: "🔷" },
    { key: "React", label: "React", icon: "⚛️" },
  ];

  useEffect(() => {
    loadSystemPrompts();
    loadPromptTemplates();
  }, []);

  const loadSystemPrompts = async () => {
    try {
      setLoading(true);
      const response = await fetch("https://localhost:7001/api/systemprompts");
      const data = await response.json();
      setPrompts(data);
    } catch (error) {
      console.error("Failed to load system prompts:", error);
    } finally {
      setLoading(false);
    }
  };

  const loadPromptTemplates = async () => {
    try {
      const response = await fetch(
        "https://localhost:7001/api/systemprompts/templates"
      );
      const data = await response.json();
      setTemplates(data);
    } catch (error) {
      console.error("Failed to load prompt templates:", error);
    }
  };

  const updateCustomAdditions = async (
    language: string,
    customAdditions: string
  ) => {
    try {
      setSaving(language);
      await fetch(
        `https://localhost:7001/api/systemprompts/${language}/custom`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ customAdditions }),
        }
      );

      setPrompts((prev) => ({
        ...prev,
        [language]: {
          ...prev[language],
          customAdditions,
          lastModified: new Date().toISOString(),
        },
      }));
    } catch (error) {
      console.error("Failed to update custom additions:", error);
    } finally {
      setSaving(null);
    }
  };

  const previewCombinedPrompt = async (language: string) => {
    try {
      const response = await fetch(
        `https://localhost:7001/api/systemprompts/preview/${language}`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            customAdditions: prompts[language]?.customAdditions || "",
          }),
        }
      );
      const data = await response.json();

      setPreviewData((prev) => ({
        ...prev,
        [language]: data,
      }));

      setPreviewMode((prev) => ({
        ...prev,
        [language]: true,
      }));
    } catch (error) {
      console.error("Failed to preview combined prompt:", error);
    }
  };

  const insertTemplate = (language: string, template: string) => {
    setPrompts((prev) => ({
      ...prev,
      [language]: {
        ...prev[language],
        customAdditions:
          (prev[language]?.customAdditions || "") + "\n" + template,
      },
    }));
  };

  // Copy to clipboard functionality
  const copyToClipboard = async (text: string, fieldId: string) => {
    try {
      await navigator.clipboard.writeText(formatTextForCopy(text));
      setCopiedField(fieldId);
      // Reset the copied state after 2 seconds
      setTimeout(() => setCopiedField(null), 2000);
    } catch (error) {
      console.error("Failed to copy to clipboard:", error);
    }
  };

  // Format text for better readability when copying
  const formatTextForCopy = (text: string): string => {
    // Add line breaks before numbered items (1., 2., 3., etc.)
    return text.replace(/(\d+\.)/g, "\n$1");
  };

  const resetCustomAdditions = (language: string) => {
    setPrompts((prev) => ({
      ...prev,
      [language]: {
        ...prev[language],
        customAdditions: "",
      },
    }));
  };

  const handleAddLanguage = () => {
    // This is a placeholder function for the "Add Language" feature
    // In a real implementation, this would call an API to add a new language
    setShowAddLanguageModal(false);
    setNewLanguageForm({ name: "", key: "", icon: "" });

    // Show a comprehensive "Under Development" message
    const message = `🚧 Feature Under Development 🚧

The "Add Programming Language" feature is currently being developed and will include:

🎯 Core Features:
• Custom language configuration and prompts
• File extension mapping and syntax rules  
• Specialized AI analysis patterns
• Language-specific best practices

⚙️ Configuration Options:
• Base system prompt templates
• Custom coding standards integration
• Performance and security focus areas
• Integration with existing workflows

📅 Coming Soon:
This feature will be available in the next major release. 

Would you like to be notified when it's ready?`;

    alert(message);
  };

  const openAddLanguageModal = () => {
    setShowAddLanguageModal(true);
  };

  if (loading) {
    return (
      <div className={styles.loadingContainer}>
        <div className={styles.loadingSpinner}></div>
        <span className={styles.loadingText}>Loading system prompts...</span>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      {/* Language Tabs */}
      <div className={styles.tabsContainer}>
        <div className={styles.tabsBorder}>
          <nav className={styles.tabsNav}>
            {languages.map((language) => (
              <button
                key={language.key}
                onClick={() => setActiveLanguage(language.key)}
                className={`${styles.tab} ${
                  activeLanguage === language.key
                    ? styles.tabActive
                    : styles.tabInactive
                }`}
              >
                <span>{language.icon}</span>
                {language.label}
              </button>
            ))}

            {/* Add Language Button */}
            <button
              onClick={openAddLanguageModal}
              className={styles.addLanguageButton}
              title="Add new programming language support"
            >
              <span className={styles.addIcon}>➕</span>
              <span>Add Language</span>
            </button>
          </nav>
        </div>
      </div>

      {/* Main Content - Fixed Layout */}
      {prompts[activeLanguage] && (
        <div className={styles.contentLayout}>
          {/* Left Panel - Base System Prompt and Custom Additions */}
          <div className={styles.leftPanel}>
            {/* Base System Prompt (Read-only) */}
            <div className={styles.card}>
              <div className={styles.cardHeader}>
                <h2 className={styles.cardTitle}>
                  <span>
                    {languages.find((l) => l.key === activeLanguage)?.icon}
                  </span>
                  {languages.find((l) => l.key === activeLanguage)?.label} Base
                  System Prompt
                  <span className={styles.badgeReadonly}>Read-only</span>
                </h2>
                <p className={styles.cardDescription}>
                  Default system prompt loaded from configuration. This cannot
                  be modified.
                </p>
              </div>

              <div className={styles.textareaContainer}>
                <div className={styles.textareaHeader}>
                  <button
                    onClick={() =>
                      copyToClipboard(
                        prompts[activeLanguage]?.systemPrompt || "",
                        `systemPrompt-${activeLanguage}`
                      )
                    }
                    className={styles.copyButton}
                    disabled={!prompts[activeLanguage]?.systemPrompt}
                  >
                    {copiedField === `systemPrompt-${activeLanguage}` ? (
                      <>
                        <Check className={styles.copyIcon} />
                        Copied!
                      </>
                    ) : (
                      <>
                        <Copy className={styles.copyIcon} />
                        Copy to Clipboard
                      </>
                    )}
                  </button>
                </div>
                <textarea
                  value={prompts[activeLanguage]?.systemPrompt || ""}
                  readOnly
                  className={styles.textareaReadonly}
                  placeholder="Loading base prompt..."
                />
              </div>
            </div>

            {/* Custom Additions */}
            <div className={styles.card}>
              <div className={styles.cardHeader}>
                <h2 className={styles.cardTitle}>
                  Custom Additions for{" "}
                  {languages.find((l) => l.key === activeLanguage)?.label}
                  <span className={styles.badgeEditable}>Editable</span>
                </h2>
                <p className={styles.cardDescription}>
                  Add your own requirements, coding standards, or specific focus
                  areas for{" "}
                  {languages.find((l) => l.key === activeLanguage)?.label}{" "}
                  reviews.
                </p>
              </div>

              <textarea
                value={prompts[activeLanguage]?.customAdditions || ""}
                onChange={(e: React.ChangeEvent<HTMLTextAreaElement>) =>
                  setPrompts((prev) => ({
                    ...prev,
                    [activeLanguage]: {
                      ...prev[activeLanguage],
                      customAdditions: e.target.value,
                    },
                  }))
                }
                placeholder={`Add custom requirements for ${
                  languages.find((l) => l.key === activeLanguage)?.label
                } code reviews...\n\nExamples:\n- Follow our company naming conventions\n- Focus on performance in data processing functions\n- Ensure all public APIs have documentation\n- Check for proper error handling in async operations`}
                className={styles.textareaEditable}
              />

              <div className={styles.buttonGroup}>
                <button
                  onClick={() =>
                    updateCustomAdditions(
                      activeLanguage,
                      prompts[activeLanguage]?.customAdditions || ""
                    )
                  }
                  disabled={saving === activeLanguage}
                  className={styles.buttonPrimary}
                >
                  {saving === activeLanguage ? (
                    <div className={styles.buttonSpinner}></div>
                  ) : (
                    <span>💾</span>
                  )}
                  Save Changes
                </button>

                <button
                  onClick={() => previewCombinedPrompt(activeLanguage)}
                  className={styles.buttonSecondary}
                >
                  <span>👁️</span>
                  Preview Combined
                </button>

                <button
                  onClick={() => resetCustomAdditions(activeLanguage)}
                  className={styles.buttonSecondary}
                >
                  <span>🔄</span>
                  Reset
                </button>
              </div>

              {prompts[activeLanguage]?.lastModified && (
                <p className={styles.lastModified}>
                  Last modified:{" "}
                  {new Date(
                    prompts[activeLanguage].lastModified
                  ).toLocaleString()}
                </p>
              )}
            </div>
          </div>

          {/* Right Panel - Template Library */}
          <div className={styles.rightPanel}>
            <h2 className={styles.templatesTitle}>💡 Quick Templates</h2>
            <p className={styles.templatesDescription}>
              Click to add common prompt additions
            </p>

            {templates && (
              <div className={styles.templatesContainer}>
                {Object.entries(templates).map(([category, items]) => (
                  <div key={category}>
                    <h4 className={styles.templateCategory}>
                      {category.replace(/([A-Z])/g, " $1").trim()}
                    </h4>
                    <div className={styles.templateItems}>
                      {items.map((template: string, index: number) => (
                        <button
                          key={index}
                          className={styles.templateButton}
                          onClick={() =>
                            insertTemplate(activeLanguage, template)
                          }
                        >
                          {template}
                        </button>
                      ))}
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      )}

      {/* Preview Panel - Full Width */}
      {previewMode[activeLanguage] && previewData[activeLanguage] && (
        <div className={styles.previewCard}>
          <h2 className={styles.previewTitle}>Combined Prompt Preview</h2>
          <p className={styles.templatesDescription}>
            This is how the final prompt will look when sent to the AI
          </p>

          <div className={styles.previewMeta}>
            <p>
              Preview generated at:{" "}
              {new Date(
                previewData[activeLanguage].previewGeneratedAt
              ).toLocaleString()}
            </p>
          </div>

          <div>
            <div className={styles.textareaHeader}>
              <h4>Combined System Prompt:</h4>
              <button
                onClick={() =>
                  copyToClipboard(
                    previewData[activeLanguage].combinedPrompt,
                    `preview-${activeLanguage}`
                  )
                }
                className={styles.copyButton}
              >
                {copiedField === `preview-${activeLanguage}` ? (
                  <>
                    <Check className={styles.copyIcon} />
                    Copied!
                  </>
                ) : (
                  <>
                    <Copy className={styles.copyIcon} />
                    Copy to Clipboard
                  </>
                )}
              </button>
            </div>
            <textarea
              value={previewData[activeLanguage].combinedPrompt}
              readOnly
              className={styles.previewTextarea}
            />
          </div>

          <button
            onClick={() =>
              setPreviewMode((prev) => ({
                ...prev,
                [activeLanguage]: false,
              }))
            }
            className={styles.closePreviewButton}
          >
            Close Preview
          </button>
        </div>
      )}

      {/* Add Language Modal */}
      <Modal
        isOpen={showAddLanguageModal}
        onClose={() => setShowAddLanguageModal(false)}
        title="Add New Programming Language"
        maxWidth="md"
      >
        <div className={styles.modalContent}>
          <Alert
            type="info"
            message="Configure a new programming language for AI code reviews. This will add language-specific prompts and analysis capabilities."
          />

          <div>
            <label className={styles.formLabel}>Language Name</label>
            <input
              type="text"
              value={newLanguageForm.name}
              onChange={(e) =>
                setNewLanguageForm((prev) => ({
                  ...prev,
                  name: e.target.value,
                }))
              }
              className={styles.formInput}
              placeholder="e.g., Python, Rust, Go"
            />
          </div>

          <div>
            <label className={styles.formLabel}>Language Key</label>
            <input
              type="text"
              value={newLanguageForm.key}
              onChange={(e) =>
                setNewLanguageForm((prev) => ({
                  ...prev,
                  key: e.target.value,
                }))
              }
              className={styles.formInput}
              placeholder="e.g., Python, Rust, Go"
            />
          </div>

          <div>
            <label className={styles.formLabel}>Language Icon (Emoji)</label>
            <input
              type="text"
              value={newLanguageForm.icon}
              onChange={(e) =>
                setNewLanguageForm((prev) => ({
                  ...prev,
                  icon: e.target.value,
                }))
              }
              className={styles.formInput}
              placeholder="e.g., 🐍, 🦀, 🐹"
              maxLength={2}
            />
          </div>

          <div className={styles.modalButtonGroup}>
            <button
              onClick={handleAddLanguage}
              className={styles.modalPrimaryButton}
            >
              Add Language
            </button>
            <button
              onClick={() => setShowAddLanguageModal(false)}
              className={styles.modalSecondaryButton}
            >
              Cancel
            </button>
          </div>
        </div>
      </Modal>
    </div>
  );
};

export default SystemPromptsManager;
