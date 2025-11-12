import React from "react";
import styles from "./AzureOpenAISettingsModal.module.css";

interface AzureOpenAIConfig {
  endpoint: string;
  apiKey: string;
  deploymentName: string;
  apiVersion: string;
  temperature: number;
  maxTokens: number;
  contentLimit: number;
}

interface AzureOpenAISettingsModalProps {
  show: boolean;
  config: AzureOpenAIConfig | null;
  isSaving: boolean;
  onClose: () => void;
  onSave: () => void;
  onConfigChange: (config: AzureOpenAIConfig) => void;
}

export const AzureOpenAISettingsModal: React.FC<
  AzureOpenAISettingsModalProps
> = ({ show, config, isSaving, onClose, onSave, onConfigChange }) => {
  if (!show || !config) return null;

  return (
    <div className={styles.modal} onClick={onClose}>
      <div className={styles.modalContent} onClick={(e) => e.stopPropagation()}>
        <div className={styles.modalHeader}>
          <h2>Azure OpenAI Settings</h2>
          <button
            onClick={onClose}
            className={styles.closeButton}
            title="Close settings"
          >
            ✕
          </button>
        </div>
        <div className={styles.modalBody}>
          <div className={styles.settingsSection}>
            <h3>Connection Settings</h3>
            <p className={styles.settingsNote}>
              ⚠️ Changes to these settings require app restart
            </p>

            <div className={styles.settingField}>
              <label>Endpoint</label>
              <input
                type="text"
                value={config.endpoint}
                readOnly
                className={styles.input}
              />
            </div>

            <div className={styles.settingField}>
              <label>API Key</label>
              <input
                type="text"
                value={config.apiKey}
                readOnly
                className={styles.input}
              />
            </div>

            <div className={styles.settingField}>
              <label>Deployment Name</label>
              <input
                type="text"
                value={config.deploymentName}
                readOnly
                className={styles.input}
              />
            </div>

            <div className={styles.settingField}>
              <label>API Version</label>
              <input
                type="text"
                value={config.apiVersion}
                readOnly
                className={styles.input}
              />
            </div>
          </div>

          <div className={styles.settingsSection}>
            <h3>Runtime Settings</h3>
            <p className={styles.settingsNote}>
              ✅ Changes take effect immediately
            </p>

            <div className={styles.settingField}>
              <label>
                Temperature: {config.temperature.toFixed(2)}
                <span className={styles.hint}>(0-2, lower = more focused)</span>
              </label>
              <input
                type="range"
                min="0"
                max="2"
                step="0.1"
                value={config.temperature}
                onChange={(e) =>
                  onConfigChange({
                    ...config,
                    temperature: parseFloat(e.target.value),
                  })
                }
                className={styles.slider}
              />
            </div>

            <div className={styles.settingField}>
              <label>
                Max Tokens
                <span className={styles.hint}>(1-128000)</span>
              </label>
              <input
                type="number"
                min="1"
                max="128000"
                value={config.maxTokens}
                onChange={(e) =>
                  onConfigChange({
                    ...config,
                    maxTokens: parseInt(e.target.value) || 4000,
                  })
                }
                className={styles.input}
              />
            </div>

            <div className={styles.settingField}>
              <label>
                Content Limit
                <span className={styles.hint}>(minimum 1000)</span>
              </label>
              <input
                type="number"
                min="1000"
                value={config.contentLimit}
                onChange={(e) =>
                  onConfigChange({
                    ...config,
                    contentLimit: parseInt(e.target.value) || 15000,
                  })
                }
                className={styles.input}
              />
            </div>
          </div>
        </div>
        <div className={styles.modalFooter}>
          <button onClick={onClose} className={styles.cancelButton}>
            Cancel
          </button>
          <button
            onClick={onSave}
            disabled={isSaving}
            className={styles.saveButton}
          >
            {isSaving ? "Saving..." : "Save Runtime Settings"}
          </button>
        </div>
      </div>
    </div>
  );
};

