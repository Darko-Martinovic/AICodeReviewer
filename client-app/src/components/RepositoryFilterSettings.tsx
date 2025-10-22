import React, { useState, useEffect } from "react";
import { Modal, Alert } from "./UI";
import { repositoryFilterApi } from "../services/api";
import type {
  RepositoryFilterSettings as FilterSettings,
  RepositoryFilterPattern,
  FilterMode,
  TestPatternRequest,
} from "../services/api";
import styles from "./RepositoryFilterSettings.module.css";

interface RepositoryFilterSettingsProps {
  onFiltersChanged?: () => void;
}

const RepositoryFilterSettings: React.FC<RepositoryFilterSettingsProps> = ({
  onFiltersChanged,
}) => {
  const [settings, setSettings] = useState<FilterSettings>({
    includePatterns: [],
    excludePatterns: [],
    enableFiltering: false,
    defaultMode: "ShowAll",
  });
  const [loading, setLoading] = useState<boolean>(false);
  const [saving, setSaving] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  // Pattern Editor Modal
  const [showPatternModal, setShowPatternModal] = useState<boolean>(false);
  const [editingPattern, setEditingPattern] = useState<{
    pattern: RepositoryFilterPattern;
    index?: number;
    type: "include" | "exclude";
  } | null>(null);

  // Pattern Test Modal
  const [showTestModal, setShowTestModal] = useState<boolean>(false);
  const [testForm, setTestForm] = useState({
    repositoryName: "",
    owner: "",
    pattern: null as RepositoryFilterPattern | null,
  });
  const [testResult, setTestResult] = useState<{
    matches: boolean;
    message: string;
  } | null>(null);

  const supportedProviders = [
    { key: "", label: "All Providers" },
    { key: "github", label: "GitHub" },
    { key: "gitlab", label: "GitLab" },
    { key: "bitbucket", label: "Bitbucket" },
    { key: "azure-devops", label: "Azure DevOps" },
  ];

  useEffect(() => {
    loadSettings();
  }, []);

  const loadSettings = async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await repositoryFilterApi.getSettings();
      setSettings(response.data);
    } catch (error) {
      console.error("Failed to load repository filter settings:", error);
      setError("Failed to load filter settings");
    } finally {
      setLoading(false);
    }
  };

  const saveSettings = async () => {
    try {
      setSaving(true);
      setError(null);
      setSuccess(null);

      await repositoryFilterApi.updateSettings(settings);
      setSuccess("Filter settings saved successfully!");

      // Notify parent component that filters have changed
      if (onFiltersChanged) {
        onFiltersChanged();
      }

      // Clear success message after 3 seconds
      setTimeout(() => setSuccess(null), 3000);
    } catch (error) {
      console.error("Failed to save repository filter settings:", error);
      setError("Failed to save filter settings");
    } finally {
      setSaving(false);
    }
  };

  const resetSettings = async () => {
    if (
      !confirm("Are you sure you want to reset all filter settings to default?")
    ) {
      return;
    }

    try {
      setSaving(true);
      setError(null);
      setSuccess(null);

      await repositoryFilterApi.resetSettings();
      await loadSettings(); // Reload from server
      setSuccess("Filter settings reset to default!");

      if (onFiltersChanged) {
        onFiltersChanged();
      }

      setTimeout(() => setSuccess(null), 3000);
    } catch (error) {
      console.error("Failed to reset repository filter settings:", error);
      setError("Failed to reset filter settings");
    } finally {
      setSaving(false);
    }
  };

  const openPatternModal = (
    type: "include" | "exclude",
    pattern?: RepositoryFilterPattern,
    index?: number
  ) => {
    setEditingPattern({
      pattern: pattern || {
        pattern: "",
        provider: "",
        owner: "",
        caseSensitive: false,
        description: "",
      },
      index,
      type,
    });
    setShowPatternModal(true);
  };

  const savePattern = () => {
    if (!editingPattern) return;

    const { pattern, index, type } = editingPattern;

    if (!pattern.pattern.trim()) {
      setError("Pattern cannot be empty");
      return;
    }

    setSettings((prev) => {
      const targetArray =
        type === "include" ? "includePatterns" : "excludePatterns";
      const newArray = [...prev[targetArray]];

      if (index !== undefined) {
        // Edit existing pattern
        newArray[index] = pattern;
      } else {
        // Add new pattern
        newArray.push(pattern);
      }

      return {
        ...prev,
        [targetArray]: newArray,
      };
    });

    setShowPatternModal(false);
    setEditingPattern(null);
    setError(null);
  };

  const removePattern = (type: "include" | "exclude", index: number) => {
    setSettings((prev) => {
      const targetArray =
        type === "include" ? "includePatterns" : "excludePatterns";
      const newArray = [...prev[targetArray]];
      newArray.splice(index, 1);

      return {
        ...prev,
        [targetArray]: newArray,
      };
    });
  };

  const testPattern = async () => {
    if (!testForm.pattern || !testForm.repositoryName.trim()) {
      setError("Please provide both pattern and repository name for testing");
      return;
    }

    try {
      const request: TestPatternRequest = {
        repositoryName: testForm.repositoryName,
        owner: testForm.owner || undefined,
        pattern: testForm.pattern,
      };

      const response = await repositoryFilterApi.testPattern(request);
      setTestResult({
        matches: response.data.matches,
        message: response.data.message,
      });
    } catch (error) {
      console.error("Failed to test pattern:", error);
      setError("Failed to test pattern");
    }
  };

  const openTestModal = (pattern: RepositoryFilterPattern) => {
    setTestForm({
      repositoryName: "",
      owner: "",
      pattern,
    });
    setTestResult(null);
    setShowTestModal(true);
  };

  if (loading) {
    return (
      <div className={styles.loadingContainer}>
        <div className={styles.loadingSpinner}></div>
        <span className={styles.loadingText}>
          Loading repository filter settings...
        </span>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <h1 className={styles.title}>Repository Filter Settings</h1>
        <p className={styles.description}>
          Control which repositories are displayed in the application. Use
          wildcard patterns with asterisk (*) to filter repositories by name,
          owner, or provider.
        </p>
      </div>

      {/* Status Messages */}
      {error && (
        <Alert type="error" message={error} onClose={() => setError(null)} />
      )}
      {success && (
        <Alert
          type="success"
          message={success}
          onClose={() => setSuccess(null)}
        />
      )}

      {/* Main Settings */}
      <div className={styles.settingsCard}>
        <div className={styles.settingGroup}>
          <label className={styles.toggleLabel}>
            <input
              type="checkbox"
              checked={settings.enableFiltering}
              onChange={(e) =>
                setSettings((prev) => ({
                  ...prev,
                  enableFiltering: e.target.checked,
                }))
              }
              className={styles.toggleInput}
            />
            <span className={styles.toggleSlider}></span>
            Enable Repository Filtering
          </label>
          <p className={styles.settingDescription}>
            When enabled, only repositories matching your filter patterns will
            be displayed.
          </p>
        </div>

        <div className={styles.settingGroup}>
          <label className={styles.selectLabel}>Default Filter Mode</label>
          <select
            value={settings.defaultMode}
            onChange={(e) =>
              setSettings((prev) => ({
                ...prev,
                defaultMode: e.target.value as FilterMode,
              }))
            }
            className={styles.selectInput}
          >
            <option value="ShowAll">Show All Repositories</option>
            <option value="IncludeOnly">Show Only Matching Repositories</option>
            <option value="ExcludeMatching">Hide Matching Repositories</option>
          </select>
          <p className={styles.settingDescription}>
            How to handle repositories when no specific patterns are configured.
          </p>
        </div>
      </div>

      {/* Include Patterns */}
      <div className={styles.patternsCard}>
        <div className={styles.patternsHeader}>
          <h2 className={styles.patternsTitle}>
            <span className={styles.includeIcon}>✅</span>
            Include Patterns
          </h2>
          <button
            onClick={() => openPatternModal("include")}
            className={styles.addPatternButton}
          >
            <span>➕</span>
            Add Include Pattern
          </button>
        </div>
        <p className={styles.patternsDescription}>
          Repositories matching these patterns will be included in the display.
        </p>

        {settings.includePatterns.length === 0 ? (
          <div className={styles.emptyState}>
            <span className={styles.emptyIcon}>📂</span>
            <p>No include patterns configured</p>
            <p className={styles.emptySubtext}>
              Add patterns to include specific repositories
            </p>
          </div>
        ) : (
          <div className={styles.patternsList}>
            {settings.includePatterns.map((pattern, index) => (
              <PatternCard
                key={index}
                pattern={pattern}
                type="include"
                onEdit={() => openPatternModal("include", pattern, index)}
                onRemove={() => removePattern("include", index)}
                onTest={() => openTestModal(pattern)}
              />
            ))}
          </div>
        )}
      </div>

      {/* Exclude Patterns */}
      <div className={styles.patternsCard}>
        <div className={styles.patternsHeader}>
          <h2 className={styles.patternsTitle}>
            <span className={styles.excludeIcon}>❌</span>
            Exclude Patterns
          </h2>
          <button
            onClick={() => openPatternModal("exclude")}
            className={styles.addPatternButton}
          >
            <span>➕</span>
            Add Exclude Pattern
          </button>
        </div>
        <p className={styles.patternsDescription}>
          Repositories matching these patterns will be excluded from the
          display.
        </p>

        {settings.excludePatterns.length === 0 ? (
          <div className={styles.emptyState}>
            <span className={styles.emptyIcon}>🚫</span>
            <p>No exclude patterns configured</p>
            <p className={styles.emptySubtext}>
              Add patterns to exclude specific repositories
            </p>
          </div>
        ) : (
          <div className={styles.patternsList}>
            {settings.excludePatterns.map((pattern, index) => (
              <PatternCard
                key={index}
                pattern={pattern}
                type="exclude"
                onEdit={() => openPatternModal("exclude", pattern, index)}
                onRemove={() => removePattern("exclude", index)}
                onTest={() => openTestModal(pattern)}
              />
            ))}
          </div>
        )}
      </div>

      {/* Action Buttons */}
      <div className={styles.actionButtons}>
        <button
          onClick={saveSettings}
          disabled={saving}
          className={styles.saveButton}
        >
          {saving ? (
            <>
              <div className={styles.buttonSpinner}></div>
              Saving...
            </>
          ) : (
            <>
              <span>💾</span>
              Save Settings
            </>
          )}
        </button>

        <button
          onClick={resetSettings}
          disabled={saving}
          className={styles.resetButton}
        >
          <span>🔄</span>
          Reset to Default
        </button>
      </div>

      {/* Pattern Editor Modal */}
      <Modal
        isOpen={showPatternModal}
        onClose={() => {
          setShowPatternModal(false);
          setEditingPattern(null);
          setError(null);
        }}
        title={`${editingPattern?.index !== undefined ? "Edit" : "Add"} ${
          editingPattern?.type === "include" ? "Include" : "Exclude"
        } Pattern`}
        maxWidth="lg"
      >
        {editingPattern && (
          <PatternEditor
            pattern={editingPattern.pattern}
            onChange={(updatedPattern) =>
              setEditingPattern((prev) =>
                prev ? { ...prev, pattern: updatedPattern } : null
              )
            }
            onSave={savePattern}
            onCancel={() => {
              setShowPatternModal(false);
              setEditingPattern(null);
              setError(null);
            }}
            supportedProviders={supportedProviders}
          />
        )}
      </Modal>

      {/* Pattern Test Modal */}
      <Modal
        isOpen={showTestModal}
        onClose={() => {
          setShowTestModal(false);
          setTestForm({ repositoryName: "", owner: "", pattern: null });
          setTestResult(null);
        }}
        title="Test Pattern"
        maxWidth="md"
      >
        <div className={styles.testModalContent}>
          <div className={styles.testForm}>
            <div>
              <label className={styles.formLabel}>Repository Name</label>
              <input
                type="text"
                value={testForm.repositoryName}
                onChange={(e) =>
                  setTestForm((prev) => ({
                    ...prev,
                    repositoryName: e.target.value,
                  }))
                }
                placeholder="e.g., AICodeReviewer"
                className={styles.formInput}
              />
            </div>

            <div>
              <label className={styles.formLabel}>Owner (Optional)</label>
              <input
                type="text"
                value={testForm.owner}
                onChange={(e) =>
                  setTestForm((prev) => ({ ...prev, owner: e.target.value }))
                }
                placeholder="e.g., Darko-Martinovic"
                className={styles.formInput}
              />
            </div>

            <button onClick={testPattern} className={styles.testButton}>
              <span>🧪</span>
              Test Pattern
            </button>
          </div>

          {testResult && (
            <div
              className={`${styles.testResult} ${
                testResult.matches ? styles.testMatch : styles.testNoMatch
              }`}
            >
              <div className={styles.testResultIcon}>
                {testResult.matches ? "✅" : "❌"}
              </div>
              <div className={styles.testResultMessage}>
                {testResult.message}
              </div>
            </div>
          )}
        </div>
      </Modal>
    </div>
  );
};

// Pattern Card Component
interface PatternCardProps {
  pattern: RepositoryFilterPattern;
  type: "include" | "exclude";
  onEdit: () => void;
  onRemove: () => void;
  onTest: () => void;
}

const PatternCard: React.FC<PatternCardProps> = ({
  pattern,
  type,
  onEdit,
  onRemove,
  onTest,
}) => {
  return (
    <div
      className={`${styles.patternCard} ${
        type === "include" ? styles.includeCard : styles.excludeCard
      }`}
    >
      <div className={styles.patternContent}>
        <div className={styles.patternMain}>
          <code className={styles.patternCode}>{pattern.pattern}</code>
          {pattern.description && (
            <p className={styles.patternDescription}>{pattern.description}</p>
          )}
        </div>

        <div className={styles.patternMeta}>
          {pattern.provider && (
            <span className={styles.patternTag}>
              Provider: {pattern.provider}
            </span>
          )}
          {pattern.owner && (
            <span className={styles.patternTag}>Owner: {pattern.owner}</span>
          )}
          {pattern.caseSensitive && (
            <span className={styles.patternTag}>Case Sensitive</span>
          )}
        </div>
      </div>

      <div className={styles.patternActions}>
        <button
          onClick={onTest}
          className={styles.testActionButton}
          title="Test Pattern"
        >
          🧪
        </button>
        <button
          onClick={onEdit}
          className={styles.editActionButton}
          title="Edit Pattern"
        >
          ✏️
        </button>
        <button
          onClick={onRemove}
          className={styles.removeActionButton}
          title="Remove Pattern"
        >
          🗑️
        </button>
      </div>
    </div>
  );
};

// Pattern Editor Component
interface PatternEditorProps {
  pattern: RepositoryFilterPattern;
  onChange: (pattern: RepositoryFilterPattern) => void;
  onSave: () => void;
  onCancel: () => void;
  supportedProviders: { key: string; label: string }[];
}

const PatternEditor: React.FC<PatternEditorProps> = ({
  pattern,
  onChange,
  onSave,
  onCancel,
  supportedProviders,
}) => {
  return (
    <div className={styles.patternEditor}>
      <div className={styles.editorForm}>
        <div>
          <label className={styles.formLabel}>
            Pattern <span className={styles.required}>*</span>
          </label>
          <input
            type="text"
            value={pattern.pattern}
            onChange={(e) => onChange({ ...pattern, pattern: e.target.value })}
            placeholder="e.g., ai*, *backend*, project-name"
            className={styles.formInput}
            autoFocus
          />
          <p className={styles.fieldHelp}>
            Use asterisk (*) as wildcard. Examples: "ai*" (starts with ai),
            "*backend*" (contains backend)
          </p>
        </div>

        <div>
          <label className={styles.formLabel}>Provider</label>
          <select
            value={pattern.provider || ""}
            onChange={(e) =>
              onChange({ ...pattern, provider: e.target.value || undefined })
            }
            className={styles.formSelect}
          >
            {supportedProviders.map((provider) => (
              <option key={provider.key} value={provider.key}>
                {provider.label}
              </option>
            ))}
          </select>
        </div>

        <div>
          <label className={styles.formLabel}>Owner/Organization</label>
          <input
            type="text"
            value={pattern.owner || ""}
            onChange={(e) =>
              onChange({ ...pattern, owner: e.target.value || undefined })
            }
            placeholder="e.g., microsoft, google"
            className={styles.formInput}
          />
          <p className={styles.fieldHelp}>
            Optional: Filter by specific repository owner or organization
          </p>
        </div>

        <div>
          <label className={styles.formLabel}>Description</label>
          <input
            type="text"
            value={pattern.description || ""}
            onChange={(e) =>
              onChange({ ...pattern, description: e.target.value || undefined })
            }
            placeholder="Optional description for this pattern"
            className={styles.formInput}
          />
        </div>

        <div className={styles.checkboxGroup}>
          <label className={styles.checkboxLabel}>
            <input
              type="checkbox"
              checked={pattern.caseSensitive}
              onChange={(e) =>
                onChange({ ...pattern, caseSensitive: e.target.checked })
              }
              className={styles.checkboxInput}
            />
            Case Sensitive
          </label>
        </div>
      </div>

      <div className={styles.editorActions}>
        <button onClick={onSave} className={styles.savePatternButton}>
          <span>💾</span>
          Save Pattern
        </button>
        <button onClick={onCancel} className={styles.cancelButton}>
          Cancel
        </button>
      </div>
    </div>
  );
};

export default RepositoryFilterSettings;
