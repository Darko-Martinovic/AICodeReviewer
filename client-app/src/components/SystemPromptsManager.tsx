import React, { useState, useEffect } from "react";

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
  const [previewMode, setPreviewMode] = useState<{ [key: string]: boolean }>({});
  const [previewData, setPreviewData] = useState<{ [key: string]: PreviewData }>({});

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
      const response = await fetch("https://localhost:7001/api/systemprompts/templates");
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
      await fetch(`https://localhost:7001/api/systemprompts/${language}/custom`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ customAdditions }),
      });

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
      const response = await fetch(`https://localhost:7001/api/systemprompts/preview/${language}`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          customAdditions: prompts[language]?.customAdditions || "",
        }),
      });
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
        customAdditions: (prev[language]?.customAdditions || "") + "\n" + template,
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
          <nav className="-mb-px flex space-x-8">
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
                  Default system prompt loaded from configuration. This cannot be
                  modified.
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
    </div>
  );
};

export default SystemPromptsManager;

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
  const [previewMode, setPreviewMode] = useState<{ [key: string]: boolean }>({});
  const [previewData, setPreviewData] = useState<{ [key: string]: PreviewData }>({});

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
      const response = await fetch("https://localhost:7001/api/systemprompts/templates");
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
      await fetch(`https://localhost:7001/api/systemprompts/${language}/custom`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ customAdditions }),
      });

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
      const response = await fetch(`https://localhost:7001/api/systemprompts/preview/${language}`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          customAdditions: prompts[language]?.customAdditions || "",
        }),
      });
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
        customAdditions: (prev[language]?.customAdditions || "") + "\n" + template,
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
          <nav className="-mb-px flex space-x-8">
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
          </nav>
        </div>
      </div>

      {/* Main Content */}
      {prompts[activeLanguage] && (
        <div className="flex-1 flex flex-col lg:flex-row gap-6">
          {/* Left Panel - Base System Prompt and Custom Additions */}
          <div className="flex-1 flex flex-col gap-6">
            {/* Base System Prompt (Read-only) */}
            <div className="bg-white rounded-lg shadow-md p-6 flex-1">
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
                  Default system prompt loaded from configuration. This cannot be
                  modified.
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
            <div className="bg-white rounded-lg shadow-md p-6 flex-1">
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
                    <span>�</span>
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
                  <span>�</span>
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
    </div>
  );
};

export default SystemPromptsManager;

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

  const insertTemplate = (language: string, templateText: string) => {
    const currentAdditions = prompts[language]?.customAdditions || "";
    const newAdditions = currentAdditions
      ? `${currentAdditions}\n\n${templateText}`
      : templateText;

    setPrompts((prev) => ({
      ...prev,
      [language]: {
        ...prev[language],
        customAdditions: newAdditions,
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

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
        <span className="ml-2">Loading system prompts...</span>
      </div>
    );
  }

  return (
    <div className="container mx-auto p-6">
      <div className="mb-6">
        <h1 className="text-3xl font-bold mb-2">System Prompts Manager</h1>
        <p className="text-gray-600">
          Customize AI review prompts for different programming languages. Add
          project-specific requirements, coding standards, or additional focus
          areas.
        </p>
      </div>

      {/* Language Tabs */}
      <div className="mb-6">
        <div className="flex flex-wrap gap-2 border-b">
          {languages.map((lang) => (
            <button
              key={lang.key}
              onClick={() => setActiveLanguage(lang.key)}
              className={`px-4 py-2 font-medium border-b-2 transition-colors ${
                activeLanguage === lang.key
                  ? "border-blue-500 text-blue-600"
                  : "border-transparent text-gray-500 hover:text-gray-700"
              }`}
            >
              <span className="mr-2">{lang.icon}</span>
              {lang.label}
            </button>
          ))}
        </div>
      </div>

      {/* Content for active language */}
      {prompts[activeLanguage] && (
        <div className="flex flex-col lg:flex-row gap-6 h-full">
          {/* Left Panel - Base System Prompt and Custom Additions */}
          <div className="flex-1 flex flex-col gap-6">
            {/* Base System Prompt (Read-only) */}
            <div className="bg-white rounded-lg shadow-md p-6 flex-1">
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
                  Default system prompt loaded from configuration. This cannot be
                  modified.
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
            <div className="bg-white rounded-lg shadow-md p-6 flex-1">
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

          {/* Preview Panel */}
          {previewMode[activeLanguage] && previewData[activeLanguage] && (
            <div className="col-span-full bg-white rounded-lg shadow-md p-6 mt-6">
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
        </div>
      )}
    </div>
  );
};

export default SystemPromptsManager;
