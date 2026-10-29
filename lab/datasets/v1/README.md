# Dataset v1

The labelled set the lab scores reviewers against: 28 files in C#, TypeScript and T-SQL, 20 with known defects and 8 without, and 71 labels in all.

The code here is deliberately wrong. None of it is built or run; it only exists to be reviewed.

## Layout

Each case is a folder in `cases/`:

| File | What it holds |
|---|---|
| `case.json` | id, language, file name, group, a one-line description, and kind (`buggy` or `clean`) |
| `after/<file>` | the file the reviewer is given |
| `before/<file>` | for the realistic cases, the same file before the change that introduced the defects |
| `labels.json` | the answer key: one entry per defect, with its lines in the `after` file, category, severity, title and description |

A reviewer only ever sees the `after` file, through `DatasetCase.ForReview()`, which returns the file name and content and nothing else. Label titles and descriptions are for the judge and for people reading results.

## Groups

| Group | Cases | Labels | What they are |
|---|---|---|---|
| `realistic` | 18 | 39 | Small, plausible files (15 to 55 lines) with one to three defects each, written as a clean version and a change that breaks it, the way a defect arrives in a pull request |
| `seeded-dense` | 2 | 32 | The two files from the `OPS-1` branch, 145 and 201 lines with many defects each. Their `// BUG:` comments became the labels and were then removed, and names that gave the defects away were changed |
| `clean` | 8 | 0 | Files with no known defect, to count false alarms |

Results should be read per group as well as overall, since the two dense files hold almost half the labels.

## Labels

- Only defects that are clear from the code are labelled: security holes, wrong results, crashes, resource and concurrency problems, and clear performance problems. Style and naming are not labelled.
- A reviewer may find real problems that have no label, especially in the dense files. The judge in phase 4 counts those as "real but unlabelled" rather than as false alarms, and they are candidates for new labels.
- Line numbers are 1-based and refer to the `after` file. A label covers `line` through `end_line`.

## Rules for changing the dataset

- Never add comments or names that point at a defect (`BUG`, `vulnerable`, `leak` and the like). `DatasetTests` fails when it finds one.
- Keep `labels.json` in step with the file: `DatasetTests` checks that every label points at lines that exist.
- Changing a file or its labels changes every score measured on it, so make a new dataset version (`v2`) rather than editing `v1` once results have been recorded against it.

List the cases, or print every label with the lines it points at (add a case id for one case):

```bash
dotnet run --project lab/AICodeReviewer.Lab -- list
dotnet run --project lab/AICodeReviewer.Lab -- labels
dotnet run --project lab/AICodeReviewer.Lab -- labels v1 cs-04-document-upload
```

## Changes

- 2026-10-29: `sql-02-transfer-funds#2` (the transaction left open on failure) now covers the whole transaction, lines 12-40, instead of only `BEGIN TRANSACTION` on line 12. The defect is a missing `SET XACT_ABORT ON`, which has no line of its own, and reviewers rightly pointed at the statements that can fail. Every saved review of that case was graded again with `rejudge --cases sql-02`, so all results match the current labels.
