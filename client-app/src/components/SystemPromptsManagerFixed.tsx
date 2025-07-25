import React, { useState, useEffect } from "react";
import { Modal, Alert } from "./UI";

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
    { key: "VBNet", label: "VB.NET", icon: "🔵" },
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
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-500"></div>
        <span className="ml-2">Loading system prompts...</span>
      </div>
    );
  }

  return (
    <div className="h-full flex flex-col">
      {/* Language Tabs */}
      <div className="mb-6">
        <div className="border-b border-gray-200">
          <nav className="-mb-px flex space-x-8 items-center">
            {languages.map((language) => (
              <button
                key={language.key}
                onClick={() => setActiveLanguage(language.key)}
                className={`${
                  activeLanguage === language.key
                    ? "border-blue-500 text-blue-600"
                    : "border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300"
                } whitespace-nowrap py-2 px-1 border-b-2 font-medium text-sm flex items-center gap-2`}
              >
                <span>{language.icon}</span>
                {language.label}
              </button>
            ))}

            {/* Add Language Button */}
            <button
              onClick={openAddLanguageModal}
              className="ml-4 px-4 py-2 text-sm font-medium text-white bg-gradient-to-r from-blue-500 to-purple-600 hover:from-blue-600 hover:to-purple-700 rounded-md shadow-sm hover:shadow-md transition-all duration-200 flex items-center gap-2"
              title="Add new programming language support"
            >
              <span className="text-lg">➕</span>
              <span>Add Language</span>
            </button>
          </nav>
        </div>
      </div>

      {/* Main Content - Fixed Layout */}
      {prompts[activeLanguage] && (
        <div className="flex-1 flex flex-col lg:flex-row gap-6">
          {/* Left Panel - Base System Prompt and Custom Additions */}
          <div className="flex-1 space-y-6">
            {/* Base System Prompt (Read-only) */}
            <div className="bg-white rounded-lg shadow-md p-6">
              <div className="mb-4">
                <h2 className="text-xl font-semibold flex items-center gap-2">
                  <span>
                    {languages.find((l) => l.key === activeLanguage)?.icon}
                  </span>
                  {languages.find((l) => l.key === activeLanguage)?.label} Base
                  System Prompt
                  <span className="bg-gray-100 text-gray-700 px-2 py-1 rounded text-sm">
                    Read-only
                  </span>
                </h2>
                <p className="text-gray-600 text-sm mt-1">
                  Default system prompt loaded from configuration. This cannot
                  be modified.
                </p>
              </div>
              <textarea
                value={prompts[activeLanguage]?.systemPrompt || ""}
                readOnly
                className="w-full h-64 p-3 border rounded-md bg-gray-50 font-mono text-sm resize-none"
                placeholder="Loading base prompt..."
              />
            </div>

            {/* Custom Additions */}
            <div className="bg-white rounded-lg shadow-md p-6">
              <div className="mb-4">
                <h2 className="text-xl font-semibold flex items-center gap-2">
                  Custom Additions for{" "}
                  {languages.find((l) => l.key === activeLanguage)?.label}
                  <span className="bg-green-100 text-green-700 px-2 py-1 rounded text-sm">
                    Editable
                  </span>
                </h2>
                <p className="text-gray-600 text-sm mt-1">
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
                className="w-full h-48 p-3 border rounded-md font-mono text-sm resize-none"
              />

              <div className="flex gap-2 mt-4">
                <button
                  onClick={() =>
                    updateCustomAdditions(
                      activeLanguage,
                      prompts[activeLanguage]?.customAdditions || ""
                    )
                  }
                  disabled={saving === activeLanguage}
                  className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50 flex items-center gap-2"
                >
                  {saving === activeLanguage ? (
                    <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
                  ) : (
                    <span>💾</span>
                  )}
                  Save Changes
                </button>

                <button
                  onClick={() => previewCombinedPrompt(activeLanguage)}
                  className="px-4 py-2 border border-gray-300 text-gray-700 rounded hover:bg-gray-50 flex items-center gap-2"
                >
                  <span>👁️</span>
                  Preview Combined
                </button>

                <button
                  onClick={() => resetCustomAdditions(activeLanguage)}
                  className="px-4 py-2 border border-gray-300 text-gray-700 rounded hover:bg-gray-50 flex items-center gap-2"
                >
                  <span>🔄</span>
                  Reset
                </button>
              </div>

              {prompts[activeLanguage]?.lastModified && (
                <p className="text-sm text-gray-500 mt-2">
                  Last modified:{" "}
                  {new Date(
                    prompts[activeLanguage].lastModified
                  ).toLocaleString()}
                </p>
              )}
            </div>
          </div>

          {/* Right Panel - Template Library */}
          <div className="w-full lg:w-80 bg-white rounded-lg shadow-md p-6">
            <h2 className="text-xl font-semibold mb-4 flex items-center gap-2">
              💡 Quick Templates
            </h2>
            <p className="text-gray-600 text-sm mb-4">
              Click to add common prompt additions
            </p>

            {templates && (
              <div className="space-y-4">
                {Object.entries(templates).map(([category, items]) => (
                  <div key={category}>
                    <h4 className="font-medium mb-2 capitalize text-gray-800">
                      {category.replace(/([A-Z])/g, " $1").trim()}
                    </h4>
                    <div className="space-y-2">
                      {items.map((template: string, index: number) => (
                        <button
                          key={index}
                          className="w-full text-left p-2 border rounded text-xs hover:bg-blue-50 hover:border-blue-300 transition-colors"
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
        <div className="bg-white rounded-lg shadow-md p-6 mt-6">
          <h2 className="text-xl font-semibold mb-4">
            Combined Prompt Preview
          </h2>
          <p className="text-gray-600 text-sm mb-4">
            This is how the final prompt will look when sent to the AI
          </p>

          <div className="bg-blue-50 border border-blue-200 rounded p-3 mb-4">
            <p className="text-blue-800 text-sm">
              Preview generated at:{" "}
              {new Date(
                previewData[activeLanguage].previewGeneratedAt
              ).toLocaleString()}
            </p>
          </div>

          <div className="border-t pt-4">
            <h4 className="font-medium mb-2">Combined System Prompt:</h4>
            <textarea
              value={previewData[activeLanguage].combinedPrompt}
              readOnly
              className="w-full h-80 p-3 border rounded-md bg-gray-50 font-mono text-sm resize-none"
            />
          </div>

          <button
            onClick={() =>
              setPreviewMode((prev) => ({
                ...prev,
                [activeLanguage]: false,
              }))
            }
            className="mt-4 px-4 py-2 border border-gray-300 text-gray-700 rounded hover:bg-gray-50"
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
        <div className="space-y-4">
          <Alert
            type="info"
            message="Configure a new programming language for AI code reviews. This will add language-specific prompts and analysis capabilities."
          />

          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
              Language Name
            </label>
            <input
              type="text"
              value={newLanguageForm.name}
              onChange={(e) =>
                setNewLanguageForm((prev) => ({
                  ...prev,
                  name: e.target.value,
                }))
              }
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 dark:bg-gray-700 dark:border-gray-600 dark:text-white"
              placeholder="e.g., Python, Rust, Go"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
              Language Key
            </label>
            <input
              type="text"
              value={newLanguageForm.key}
              onChange={(e) =>
                setNewLanguageForm((prev) => ({
                  ...prev,
                  key: e.target.value,
                }))
              }
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 dark:bg-gray-700 dark:border-gray-600 dark:text-white"
              placeholder="e.g., Python, Rust, Go"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
              Language Icon (Emoji)
            </label>
            <input
              type="text"
              value={newLanguageForm.icon}
              onChange={(e) =>
                setNewLanguageForm((prev) => ({
                  ...prev,
                  icon: e.target.value,
                }))
              }
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 dark:bg-gray-700 dark:border-gray-600 dark:text-white"
              placeholder="e.g., 🐍, 🦀, 🐹"
              maxLength={2}
            />
          </div>

          <div className="flex gap-3 pt-4">
            <button
              onClick={handleAddLanguage}
              className="flex-1 px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 transition-colors"
            >
              Add Language
            </button>
            <button
              onClick={() => setShowAddLanguageModal(false)}
              className="flex-1 px-4 py-2 border border-gray-300 text-gray-700 rounded-md hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 dark:border-gray-600 dark:text-gray-300 dark:hover:bg-gray-700 transition-colors"
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
