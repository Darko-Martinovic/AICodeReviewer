import React, { useEffect, useState } from "react";
import { FlaskConical } from "lucide-react";
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

const percent = (rate: number) => `${Math.round(rate * 100)}%`;
const dollars = (amount: number) => `$${amount.toFixed(2)}`;
const when = (iso: string) => new Date(iso).toLocaleString();

const verdictLabels: Record<string, string> = {
  match: "Found",
  duplicate: "Duplicate",
  real: "Real, no label",
  false_alarm: "False alarm",
};

const MetricCells: React.FC<{ metrics: LabMetrics }> = ({ metrics }) => (
  <>
    <td className={styles.number}>
      {metrics.found}/{metrics.labels}
    </td>
    <td className={styles.number}>{percent(metrics.recall)}</td>
    <td className={styles.number}>{percent(metrics.precision)}</td>
    <td className={styles.number}>{metrics.f1.toFixed(2)}</td>
    <td className={styles.number}>
      {metrics.false_alarms_per_clean_case.toFixed(2)}
    </td>
  </>
);

const MetricHeaders: React.FC = () => (
  <>
    <th className={styles.number}>Found</th>
    <th className={styles.number}>Recall</th>
    <th className={styles.number}>Precision</th>
    <th className={styles.number}>F1</th>
    <th className={styles.number} title="False alarms per clean file">
      FA / clean
    </th>
  </>
);

const Scoreboard: React.FC<{ rows: LabScoreboardRow[] }> = ({ rows }) => (
  <table className={styles.table}>
    <thead>
      <tr>
        <th>Prompt</th>
        <th>Reviewer</th>
        <th className={styles.number}>Runs</th>
        <MetricHeaders />
        <th>Decision</th>
      </tr>
    </thead>
    <tbody>
      {rows.map((row) => (
        <tr key={`${row.language}-${row.version}-${row.model}`}>
          <td title={row.notes}>
            {row.language} v{row.version}
            {row.active && <span className={styles.activeBadge}>active</span>}
          </td>
          <td>{row.model}</td>
          <td className={styles.number}>{row.runs}</td>
          <MetricCells metrics={row.metrics} />
          <td>
            {row.decision ? (
              <span
                className={
                  row.decision.result === "kept"
                    ? styles.keptBadge
                    : styles.rolledBackBadge
                }
                title={row.decision.reason}
              >
                {row.decision.result === "kept" ? "Kept" : "Rolled back"}
              </span>
            ) : (
              <span className={styles.muted}>{row.notes}</span>
            )}
          </td>
        </tr>
      ))}
    </tbody>
  </table>
);

const CaseRow: React.FC<{ item: LabCase }> = ({ item }) => {
  const [open, setOpen] = useState(false);
  const falseAlarms = item.findings.filter(
    (f) => f.verdict === "false_alarm"
  ).length;

  return (
    <>
      <tr className={styles.clickable} onClick={() => setOpen(!open)}>
        <td>
          {open ? "▾" : "▸"} {item.id}
          {item.repeat > 1 && (
            <span className={styles.muted}> (run {item.repeat})</span>
          )}
        </td>
        <td>
          {item.language ?? ""} v{item.prompt_version || 1}
        </td>
        <td className={styles.number}>
          {item.found.length}/{item.labels.length}
        </td>
        <td className={styles.number}>{item.findings.length}</td>
        <td className={styles.number}>{falseAlarms}</td>
        <td>
          {item.error ? (
            <span className={styles.errorText}>{item.error}</span>
          ) : (
            <span className={styles.muted}>{item.missed.join(", ")}</span>
          )}
        </td>
      </tr>
      {open && (
        <tr>
          <td colSpan={6} className={styles.findingsCell}>
            {item.findings.length === 0 ? (
              <span className={styles.muted}>No findings.</span>
            ) : (
              <ul className={styles.findings}>
                {item.findings.map((f, index) => (
                  <li key={index} className={styles.finding}>
                    <span className={styles[`verdict_${f.verdict}`]}>
                      {verdictLabels[f.verdict] ?? f.verdict}
                    </span>
                    <span className={styles.lines}>
                      lines {f.line}-{f.end_line}
                    </span>
                    <strong>{f.title}</strong>
                    <span className={styles.muted}>
                      {f.severity} / {f.category}
                      {f.label_ids.length > 0 && ` · ${f.label_ids.join(", ")}`}
                    </span>
                    <div className={styles.reason}>Judge: {f.reason}</div>
                  </li>
                ))}
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
    <h3 className={styles.subheading}>
      {when(run.started_at)}: {run.model}, judged by {run.judge_model}
    </h3>
    <table className={styles.table}>
      <thead>
        <tr>
          <th>Group</th>
          <th className={styles.number}>Cases</th>
          <MetricHeaders />
        </tr>
      </thead>
      <tbody>
        {Object.entries(run.groups).map(([group, metrics]) => (
          <tr key={group}>
            <td>{group}</td>
            <td className={styles.number}>{metrics.cases}</td>
            <MetricCells metrics={metrics} />
          </tr>
        ))}
      </tbody>
    </table>
    <table className={styles.table}>
      <thead>
        <tr>
          <th>Case</th>
          <th>Prompt</th>
          <th className={styles.number}>Found</th>
          <th className={styles.number}>Findings</th>
          <th className={styles.number}>False alarms</th>
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

const prompts = (run: LabRunSummary) =>
  Object.entries(run.prompts ?? {})
    .map(([language, version]) => `${language} v${version}`)
    .join(", ");

export const LabPage: React.FC = () => {
  const [rows, setRows] = useState<LabScoreboardRow[] | null>(null);
  const [runs, setRuns] = useState<LabRunSummary[]>([]);
  const [selected, setSelected] = useState<LabRun | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    Promise.all([labApi.getScoreboard(), labApi.getRuns()])
      .then(([scoreboard, savedRuns]) => {
        setRows(scoreboard.data.scoreboard?.rows ?? []);
        setRuns(savedRuns.data);
      })
      .catch(() => setError("The lab's results could not be loaded."))
      .finally(() => setLoading(false));
  }, []);

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

  return (
    <div className={styles.page}>
      <div className={styles.intro}>
        <h2 className={styles.heading}>Lab</h2>
        <p className={styles.muted}>
          How well each prompt version reviews dataset v1, graded against its
          labels by a judge model. The lab tool writes these results; see
          lab/README.md for how to run it.
        </p>
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
          <section className={styles.section}>
            <h3 className={styles.subheading}>Prompt scoreboard</h3>
            <Scoreboard rows={rows ?? []} />
          </section>

          <section className={styles.section}>
            <h3 className={styles.subheading}>Runs</h3>
            <table className={styles.table}>
              <thead>
                <tr>
                  <th>Started</th>
                  <th>Reviewer / judge</th>
                  <th>Prompts</th>
                  <th className={styles.number}>Cases</th>
                  <MetricHeaders />
                  <th className={styles.number}>Cost</th>
                </tr>
              </thead>
              <tbody>
                {runs.map((run) => (
                  <tr
                    key={run.file}
                    className={`${styles.clickable} ${
                      selected?.file === run.file ? styles.selected : ""
                    }`}
                    onClick={() => openRun(run.file)}
                  >
                    <td>{when(run.started_at)}</td>
                    <td>
                      {run.model} / {run.judge_model}
                    </td>
                    <td>{prompts(run)}</td>
                    <td className={styles.number}>{run.summary.cases}</td>
                    <MetricCells metrics={run.summary} />
                    <td className={styles.number}>
                      {dollars(run.summary.reviewer_cost + run.summary.judge_cost)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
            {selected && <RunDetail run={selected} />}
          </section>
        </>
      )}
    </div>
  );
};

export default LabPage;
