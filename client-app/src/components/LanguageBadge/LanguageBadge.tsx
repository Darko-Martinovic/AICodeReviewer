import styles from "./LanguageBadge.module.css";
import { getLanguageInfo } from "../../utils/languageColors";

interface LanguageBadgeProps {
  language: string;
  size?: "small" | "medium" | "large";
  showName?: boolean;
}

export default function LanguageBadge({
  language,
  size = "medium",
  showName = true,
}: LanguageBadgeProps) {
  const languageInfo = getLanguageInfo(language);

  if (!language || language === "Unknown") {
    return null;
  }

  const sizeClass = styles[size] || styles.medium;

  return (
    <div
      className={`${styles.languageBadge} ${sizeClass}`}
      title={`Primary language: ${languageInfo.name}`}
    >
      <div
        className={styles.colorDot}
        style={{ backgroundColor: languageInfo.color }}
      />
      {showName && (
        <span className={styles.languageName}>{languageInfo.name}</span>
      )}
    </div>
  );
}
