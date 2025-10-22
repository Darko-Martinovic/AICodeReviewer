/**
 * Programming language colors based on GitHub's color scheme
 * https://github.com/github/linguist/blob/master/lib/linguist/languages.yml
 */

export interface LanguageInfo {
  color: string;
  name: string;
}

export const languageColors: Record<string, LanguageInfo> = {
  // Popular languages
  JavaScript: { color: "#f1e05a", name: "JavaScript" },
  TypeScript: { color: "#3178c6", name: "TypeScript" },
  Python: { color: "#3572A5", name: "Python" },
  Java: { color: "#b07219", name: "Java" },
  "C#": { color: "#239120", name: "C#" },
  csharp: { color: "#239120", name: "C#" },
  "C++": { color: "#f34b7d", name: "C++" },
  C: { color: "#555555", name: "C" },
  PHP: { color: "#4F5D95", name: "PHP" },
  Ruby: { color: "#701516", name: "Ruby" },
  Go: { color: "#00ADD8", name: "Go" },
  Rust: { color: "#dea584", name: "Rust" },
  Swift: { color: "#ffac45", name: "Swift" },
  Kotlin: { color: "#A97BFF", name: "Kotlin" },
  Dart: { color: "#00B4AB", name: "Dart" },
  Scala: { color: "#c22d40", name: "Scala" },

  // Web technologies
  HTML: { color: "#e34c26", name: "HTML" },
  CSS: { color: "#563d7c", name: "CSS" },
  SCSS: { color: "#c6538c", name: "SCSS" },
  Less: { color: "#1d365d", name: "Less" },
  Vue: { color: "#41b883", name: "Vue" },
  React: { color: "#61dafb", name: "React" },
  Angular: { color: "#dd1b16", name: "Angular" },

  // Database & Query languages
  SQL: { color: "#e38c00", name: "SQL" },
  TSQL: { color: "#e38c00", name: "T-SQL" },
  PLSQL: { color: "#dad55e", name: "PL/SQL" },
  "PL/SQL": { color: "#dad55e", name: "PL/SQL" },
  SQLPL: { color: "#dad55e", name: "SQL PL" },
  PLpgSQL: { color: "#336791", name: "PL/pgSQL" },
  "PL/pgSQL": { color: "#336791", name: "PL/pgSQL" },
  MySQL: { color: "#4479a1", name: "MySQL" },
  PostgreSQL: { color: "#336791", name: "PostgreSQL" },

  // .NET languages
  "Visual Basic .NET": { color: "#945db7", name: "VB.NET" },
  "VB.NET": { color: "#945db7", name: "VB.NET" },
  "F#": { color: "#b845fc", name: "F#" },

  // Functional languages
  Haskell: { color: "#5e5086", name: "Haskell" },
  Clojure: { color: "#db5855", name: "Clojure" },
  Erlang: { color: "#B83998", name: "Erlang" },
  Elixir: { color: "#6e4a7e", name: "Elixir" },

  // Systems languages
  Assembly: { color: "#6E4C13", name: "Assembly" },
  Shell: { color: "#89e051", name: "Shell" },
  PowerShell: { color: "#012456", name: "PowerShell" },
  Bash: { color: "#89e051", name: "Bash" },

  // Mobile
  "Objective-C": { color: "#438eff", name: "Objective-C" },
  "Objective-C++": { color: "#6866fb", name: "Objective-C++" },

  // Data & Config
  JSON: { color: "#292929", name: "JSON" },
  YAML: { color: "#cb171e", name: "YAML" },
  XML: { color: "#0060ac", name: "XML" },
  TOML: { color: "#9c4221", name: "TOML" },

  // Documentation
  Markdown: { color: "#083fa1", name: "Markdown" },
  reStructuredText: { color: "#141414", name: "reStructuredText" },

  // Other popular languages
  R: { color: "#198ce7", name: "R" },
  MATLAB: { color: "#e16737", name: "MATLAB" },
  Perl: { color: "#0298c3", name: "Perl" },
  Lua: { color: "#000080", name: "Lua" },
  Groovy: { color: "#e69f56", name: "Groovy" },
  CoffeeScript: { color: "#244776", name: "CoffeeScript" },

  // Default fallback
  Unknown: { color: "#ededed", name: "Unknown" },
  "": { color: "#ededed", name: "Unknown" },
};

/**
 * Get language info for a given language string
 */
export const getLanguageInfo = (language: string): LanguageInfo => {
  if (!language) {
    return languageColors["Unknown"];
  }

  // Try exact match first
  if (languageColors[language]) {
    return languageColors[language];
  }

  // Try case-insensitive match
  const lowerLang = language.toLowerCase();
  const matchedKey = Object.keys(languageColors).find(
    (key) => key.toLowerCase() === lowerLang
  );

  if (matchedKey) {
    return languageColors[matchedKey];
  }

  // Return unknown if no match found
  return languageColors["Unknown"];
};

/**
 * Generate a language badge component props
 */
export const getLanguageBadgeProps = (language: string) => {
  const info = getLanguageInfo(language);
  return {
    color: info.color,
    name: info.name,
    style: {
      backgroundColor: info.color,
      color: getContrastColor(info.color),
    },
  };
};

/**
 * Calculate contrast color for better text readability
 */
function getContrastColor(hexColor: string): string {
  // Remove # if present
  const color = hexColor.replace("#", "");

  // Convert to RGB
  const r = parseInt(color.substr(0, 2), 16);
  const g = parseInt(color.substr(2, 2), 16);
  const b = parseInt(color.substr(4, 2), 16);

  // Calculate luminance
  const luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255;

  // Return black or white based on luminance
  return luminance > 0.5 ? "#000000" : "#ffffff";
}
