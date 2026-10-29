import React, { useEffect, useMemo, useState } from "react";
import { Check, FlaskConical, Undo2 } from "lucide-react";
import { labApi } from "../services/api";
import type {
  LabCase,
  LabMetrics,
  LabRun,
  LabRunSummary,
  LabScoreboardRow,
} from "../services/api";
import { Alert, EmptyState, LoadingSpinner } from "./UI";
import styles from "./LabPage.module.css";

const languageNames: Record<string, string> = {
  CSharp: "C#",
  Sql: "T-SQL",
  TypeScript: "TypeScript",
  React: "React",
  JavaScript: "JavaScript",
  VbNet: "VB.NET",
  Java: "Java",
  General: "Other languages",
};

const verdicts = [
  { key: "match", label: "Found", className: "verdictFound" },
  { key: "real", label: "Real, no label", className: "verdictReal" },
  { key: "duplicate", label: "Duplicate", className: "verdictDuplicate" },
  { key: "false_alarm", label: "False alarm", className: "verdictFalseAlarm" },
] as const;

const languageName = (language: string | null) =>
  language ? (languageNames[language] ?? language) : "";
const percent = (rate: number) => `${Math.round(rate * 100)}%`;
const dollars = (amount: number) => `$${amount.toFixed(2)}`;
const shortDate = (iso: string) =>
  new Date(iso).toLocaleString(undefined, {
    day: "numeric",
    month: "short",
    hour: "2-digit",
    minute: "2-digit",
  });

interface Pooled {
  recall: number;
  precision: number;
  f1: number;
  falseAlarmsPerClean: number;
}

/** Adds up the counts behind several rows, so the rates are over every label and finding, not an average of rates */
const pool = (rows: LabScoreboardRow[]): Pooled => {
  const sum = (pick: (m: LabMetrics) => number) =>
    rows.reduce((total, row) => total + pick(row.metrics), 0);
  const labels = sum((m) => m.labels);
  const findings = sum((m) => m.findings);
  const clean = sum((m) => m.clean_cases);
  const recall = labels === 0 ? 1 : sum((m) => m.found) / labels;
  const precision =
    findings === 0 ? 1 : 1 - sum((m) => m.false_alarms) / findings;
  return {
    recall,
    precision,
    f1:
      recall + precision === 0
        ? 0
        : (2 * recall * precision) / (recall + precision),
    falseAlarmsPerClean:
      clean === 0 ? 0 : sum((m) => m.false_alarms_on_clean) / clean,
  };
};

const StatTile: React.FC<{
  label: string;
  value: string;
  delta: number;
  deltaText: string;
  higherIsBetter: boolean;
}> = ({ label, value, delta, deltaText, higherIsBetter }) => {
  const better = higherIsBetter ? delta > 0 : delta < 0;
  const unchanged = Math.abs(delta) < 0.005;
  let deltaClass = styles.deltaWorse;
  if (unchanged) {
    deltaClass = styles.deltaFlat;
  } else if (better) {
    deltaClass = styles.deltaBetter;
  }
  let arrow = "▼";
  if (unchanged) {
    arrow = "■";
  } else if (delta > 0) {
    arrow = "▲";
  }
  return (
    <div className={styles.tile}>
      <div className={styles.tileLabel}>{label}</div>
      <div className={styles.tileValue}>{value}</div>
      <div className={deltaClass}>
        <span aria-hidden="true">{arrow}</span> {deltaText}
      </div>
    </div>
  );
};

/** A 0-1 rate as a bar on a track of the same blue; F1 reads as a score (0.94), the rates as percentages */
const Meter: React.FC<{ label: string; rate: number; asScore?: boolean }> = ({
  label,
  rate,
  asScore = false,
}) => {
  const value = asScore ? rate.toFixed(2) : percent(rate);
  return (
    <div className={styles.meter} title={`${label} ${value}`}>
      <span className={styles.meterLabel}>{label}</span>
      <span className={styles.meterTrack}>
        <span
          className={styles.meterFill}
          style={{ width: `${Math.max(2, rate * 100)}%` }}
        />
      </span>
      <span className={styles.meterValue}>{value}</span>
    </div>
  );
};

const DecisionBadge: React.FC<{ row: LabScoreboardRow }> = ({ row }) => {
  if (!row.decision) {
    return <span className={styles.baselineBadge}>Baseline</span>;
  }
  const kept = row.decision.result === "kept";
  const Icon = kept ? Check : Undo2;
  return (
    <span
      className={kept ? styles.keptBadge : styles.rolledBackBadge}
      title={row.decision.reason}
    >
      <Icon className={styles.badgeIcon} aria-hidden="true" />
      {kept ? "Kept" : "Rolled back"}
    </span>
  );
};

const LanguageCard: React.FC<{
  language: string;
  rows: LabScoreboardRow[];
}> = ({ language, rows }) => {
  const active = rows.find((r) => r.active);
  return (
    <div className={styles.card}>
      <div className={styles.cardHeader}>
        <h4 className={styles.cardTitle}>{languageName(language)}</h4>
        {active && (
          <span className={styles.activePill}>v{active.version} active</span>
        )}
      </div>
      {rows.map((row) => (
        <div
          key={`${row.version}-${row.model}`}
          className={row.active ? styles.versionActive : styles.version}
        >
          <div className={styles.versionHeader}>
            <span className={styles.versionName}>v{row.version}</span>
            <DecisionBadge row={row} />
            <span className={styles.versionFalseAlarms}>
              {row.metrics.false_alarms_per_clean_case.toFixed(2)} false alarms
              per clean file
            </span>
          </div>
          <Meter label="Recall" rate={row.metrics.recall} />
          <Meter label="Precision" rate={row.metrics.precision} />
          <Meter label="F1" rate={row.metrics.f1} asScore />
          <div className={styles.versionNotes} title={row.notes}>
            {row.runs} {row.runs === 1 ? "run" : "runs"}, {row.metrics.cases}{" "}
            reviews · {row.notes}
          </div>
        </div>
      ))}
    </div>
  );
};

const VerdictBar: React.FC<{ item: LabCase }> = ({ item }) => {
  const total = item.findings.length;
  if (total === 0) {
    return <span className={styles.muted}>no findings</span>;
  }
  const counts = verdicts.map((v) => ({
    ...v,
    count: item.findings.filter((f) => f.verdict === v.key).length,
  }));
  return (
    <span
      className={styles.verdictBar}
      title={counts.map((c) => `${c.label}: ${c.count}`).join(", ")}
    >
      {counts
        .filter((c) => c.count > 0)
        .map((c) => (
          <span
            key={c.key}
            className={styles[c.className]}
            style={{ flexGrow: c.count }}
          />
        ))}
    </span>
  );
};

const FoundBadge: React.FC<{ item: LabCase }> = ({ item }) => {
  if (item.error) {
    return <span className={styles.missBadge}>! error</span>;
  }
  if (item.labels.length === 0) {
    return <span className={styles.muted}>clean file</span>;
  }
  const all = item.found.length === item.labels.length;
  return (
    <span className={all ? styles.allFoundBadge : styles.missBadge}>
      <span aria-hidden="true">{all ? "✓" : "!"}</span> {item.found.length}/
      {item.labels.length}
    </span>
  );
};

const CaseRow: React.FC<{ item: LabCase }> = ({ item }) => {
  const [open, setOpen] = useState(false);
  return (
    <>
      <tr className={styles.clickable} onClick={() => setOpen(!open)}>
        <td>
          <span className={styles.chevron}>{open ? "▾" : "▸"}</span>
          {item.id}
          {item.repeat > 1 && (
            <span className={styles.muted}> · run {item.repeat}</span>
          )}
        </td>
        <td>
          <span className={styles.chip}>
            {languageName(item.language)} v{item.prompt_version || 1}
          </span>
        </td>
        <td>
          <FoundBadge item={item} />
        </td>
        <td className={styles.verdictCell}>
          <VerdictBar item={item} />
          <span className={styles.findingCount}>{item.findings.length}</span>
        </td>
        <td className={styles.missed}>
          {item.error ??
            item.missed
              .map((id) => id.split("#")[1])
              .map((n) => `#${n}`)
              .join(" ")}
        </td>
      </tr>
      {open && (
        <tr>
          <td colSpan={5} className={styles.findingsCell}>
            {item.findings.length === 0 ? (
              <span className={styles.muted}>
                The reviewer reported nothing.
              </span>
            ) : (
              <ul className={styles.findings}>
                {item.findings.map((f, index) => {
                  const verdict = verdicts.find((v) => v.key === f.verdict);
                  return (
                    <li key={index} className={styles.finding}>
                      <span
                        className={
                          styles[
                            `${verdict?.className ?? "verdictDuplicate"}Badge`
                          ]
                        }
                      >
                        {verdict?.label ?? f.verdict}
                      </span>
                      <span className={styles.lines}>
                        L{f.line}
                        {f.end_line !== f.line && `-${f.end_line}`}
                      </span>
                      <strong className={styles.findingTitle}>{f.title}</strong>
                      <span className={styles.muted}>
                        {f.severity} · {f.category}
                        {f.label_ids.length > 0 &&
                          ` · ${f.label_ids.map((id) => `#${id.split("#")[1]}`).join(" ")}`}
                      </span>
                      <div className={styles.reason}>{f.reason}</div>
                    </li>
                  );
                })}
              </ul>
            )}
          </td>
        </tr>
      )}
    </>
  );
};

const RunDetail: React.FC<{ run: LabRun }> = ({ run }) => (
  <div className={styles.runDetail}>
    <div className={styles.groupTiles}>
      {Object.entries(run.groups).map(([group, m]) => (
        <div key={group} className={styles.groupTile}>
          <div className={styles.tileLabel}>
            {group} · {m.cases} reviews
          </div>
          <div className={styles.groupFigures}>
            {m.labels > 0 && (
              <span>
                <strong>{percent(m.recall)}</strong> recall
              </span>
            )}
            <span>
              <strong>{percent(m.precision)}</strong> precision
            </span>
            <span>
              <strong>{m.false_alarms}</strong> false alarms
            </span>
          </div>
        </div>
      ))}
    </div>

    <div className={styles.legend}>
      {verdicts.map((v) => (
        <span key={v.key} className={styles.legendItem}>
          <span className={styles[`${v.className}Swatch`]} />
          {v.label}
        </span>
      ))}
    </div>

    <table className={styles.table}>
      <thead>
        <tr>
          <th>Case</th>
          <th>Prompt</th>
          <th>Labels found</th>
          <th>Findings</th>
          <th>Missed</th>
        </tr>
      </thead>
      <tbody>
        {run.cases.map((item) => (
          <CaseRow key={`${item.id}-${item.repeat}`} item={item} />
        ))}
      </tbody>
    </table>
  </div>
);

export const LabPage: React.FC = () => {
  const [rows, setRows] = useState<LabScoreboardRow[] | null>(null);
  const [updatedAt, setUpdatedAt] = useState<string | null>(null);
  const [runs, setRuns] = useState<LabRunSummary[]>([]);
  const [selected, setSelected] = useState<LabRun | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    Promise.all([labApi.getScoreboard(), labApi.getRuns()])
      .then(([scoreboard, savedRuns]) => {
        setRows(scoreboard.data.scoreboard?.rows ?? []);
        setUpdatedAt(scoreboard.data.scoreboard?.updated_at ?? null);
        setRuns(savedRuns.data);
      })
      .catch(() => setError("The lab's results could not be loaded."))
      .finally(() => setLoading(false));
  }, []);

  const languages = useMemo(() => {
    const byLanguage = new Map<string, LabScoreboardRow[]>();
    for (const row of rows ?? []) {
      byLanguage.set(row.language, [
        ...(byLanguage.get(row.language) ?? []),
        row,
      ]);
    }
    return [...byLanguage.entries()];
  }, [rows]);

  const headline = useMemo(() => {
    const all = rows ?? [];
    const active = all.filter((r) => r.active);
    const languagesWithResults = new Set(active.map((r) => r.language));
    const builtIn = all.filter(
      (r) => r.version === 1 && languagesWithResults.has(r.language),
    );
    return active.length > 0 && builtIn.length > 0
      ? { now: pool(active), before: pool(builtIn) }
      : null;
  }, [rows]);

  const openRun = (file: string) => {
    if (selected?.file === file) {
      setSelected(null);
      return;
    }
    labApi
      .getRun(file)
      .then((response) => setSelected({ ...response.data, file }))
      .catch(() => setError(`The run ${file} could not be loaded.`));
  };

  if (loading) {
    return <LoadingSpinner />;
  }

  const judge = runs[0]?.judge_model;
  const points = (after: number, before: number) =>
    `${Math.abs(Math.round((after - before) * 100))} pts vs built-in prompts`;

  return (
    <div className={styles.page}>
      <div className={styles.header}>
        <div>
          <h2 className={styles.heading}>
            <FlaskConical className={styles.headingIcon} /> Review lab
          </h2>
          <p className={styles.subheading}>
            How well each prompt version finds the seeded defects in dataset v1,
            graded by a judge model. The lab tool writes these results; see
            lab/README.md.
          </p>
        </div>
        <div className={styles.metaChips}>
          <span className={styles.chip}>Dataset v1</span>
          {judge && <span className={styles.chip}>Judge {judge}</span>}
          {updatedAt && (
            <span className={styles.chip}>Updated {shortDate(updatedAt)}</span>
          )}
        </div>
      </div>

      {error && <Alert type="error" message={error} />}

      {rows !== null && rows.length === 0 && runs.length === 0 ? (
        <EmptyState
          icon={<FlaskConical className="w-12 h-12 text-gray-400" />}
          title="No lab results yet"
          description="Run the lab tool to review the dataset and fill this page."
        />
      ) : (
        <>
          {headline && (
            <section>
              <h3 className={styles.sectionTitle}>Active prompts</h3>
              <div className={styles.tiles}>
                <StatTile
                  label="Recall"
                  value={percent(headline.now.recall)}
                  delta={headline.now.recall - headline.before.recall}
                  deltaText={points(
                    headline.now.recall,
                    headline.before.recall,
                  )}
                  higherIsBetter
                />
                <StatTile
                  label="Precision"
                  value={percent(headline.now.precision)}
                  delta={headline.now.precision - headline.before.precision}
                  deltaText={points(
                    headline.now.precision,
                    headline.before.precision,
                  )}
                  higherIsBetter
                />
                <StatTile
                  label="F1"
                  value={headline.now.f1.toFixed(2)}
                  delta={headline.now.f1 - headline.before.f1}
                  deltaText={`${Math.abs(headline.now.f1 - headline.before.f1).toFixed(2)} vs built-in prompts`}
                  higherIsBetter
                />
                <StatTile
                  label="False alarms per clean file"
                  value={headline.now.falseAlarmsPerClean.toFixed(2)}
                  delta={
                    headline.now.falseAlarmsPerClean -
                    headline.before.falseAlarmsPerClean
                  }
                  deltaText={`from ${headline.before.falseAlarmsPerClean.toFixed(2)} with built-in prompts`}
                  higherIsBetter={false}
                />
              </div>
            </section>
          )}

          <section>
            <h3 className={styles.sectionTitle}>Prompt versions</h3>
            <div className={styles.cards}>
              {languages.map(([language, languageRows]) => (
                <LanguageCard
                  key={language}
                  language={language}
                  rows={languageRows}
                />
              ))}
            </div>
          </section>

          <section>
            <h3 className={styles.sectionTitle}>Runs</h3>
            <div className={styles.tableWrap}>
              <table className={styles.table}>
                <thead>
                  <tr>
                    <th>Started</th>
                    <th>Prompts</th>
                    <th className={styles.number}>Reviews</th>
                    <th className={styles.number}>Found</th>
                    <th className={styles.number}>Recall</th>
                    <th className={styles.number}>Precision</th>
                    <th className={styles.number}>F1</th>
                    <th className={styles.number}>Cost</th>
                  </tr>
                </thead>
                <tbody>
                  {runs.map((run) => (
                    <React.Fragment key={run.file}>
                      <tr
                        className={`${styles.clickable} ${
                          selected?.file === run.file ? styles.selected : ""
                        }`}
                        onClick={() => openRun(run.file)}
                      >
                        <td>
                          <span className={styles.chevron}>
                            {selected?.file === run.file ? "▾" : "▸"}
                          </span>
                          {shortDate(run.started_at)}
                          <div className={styles.muted}>{run.model}</div>
                        </td>
                        <td>
                          {Object.entries(run.prompts ?? {}).map(
                            ([language, version]) => (
                              <span key={language} className={styles.chip}>
                                {languageName(language)} v{version}
                              </span>
                            ),
                          )}
                        </td>
                        <td className={styles.number}>{run.summary.cases}</td>
                        <td className={styles.number}>
                          {run.summary.found}/{run.summary.labels}
                        </td>
                        <td className={styles.number}>
                          {percent(run.summary.recall)}
                        </td>
                        <td className={styles.number}>
                          {percent(run.summary.precision)}
                        </td>
                        <td className={styles.number}>
                          {run.summary.f1.toFixed(2)}
                        </td>
                        <td className={styles.number}>
                          {dollars(
                            run.summary.reviewer_cost + run.summary.judge_cost,
                          )}
                        </td>
                      </tr>
                      {selected?.file === run.file && (
                        <tr>
                          <td colSpan={8} className={styles.detailCell}>
                            <RunDetail run={selected} />
                          </td>
                        </tr>
                      )}
                    </React.Fragment>
                  ))}
                </tbody>
              </table>
            </div>
          </section>
        </>
      )}
    </div>
  );
};

export default LabPage;
