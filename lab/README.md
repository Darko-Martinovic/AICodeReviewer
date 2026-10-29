# The lab

The lab measures how good a reviewer is. It reviews every file of a labelled dataset with one model, has a second model (the judge) grade each review against the answer key, and adds up the scores.

- `datasets/v1`: the files and their labels; see [its README](datasets/v1/README.md)
- `AICodeReviewer.Lab`: the console tool
- `results/<dataset>`: one JSON file per run, kept in the repository so runs can be compared

## Running it

The tool uses the app's reviewer, prompts and `Configuration/models.json`, and reads keys the same way the app does: `appsettings.json` in the repository root or environment variables such as `ANTHROPIC_API_KEY`.

```bash
dotnet run --project lab/AICodeReviewer.Lab -- run --estimate            # what a full run would cost
dotnet run --project lab/AICodeReviewer.Lab -- run                       # Claude Haiku 5.5 reviews, Claude Sonnet 5.5 judges
dotnet run --project lab/AICodeReviewer.Lab -- run --cases cs- --max-cost 0.50
dotnet run --project lab/AICodeReviewer.Lab -- run --model claude-sonnet-5-5 --repeat 3
```

| Option | Default | Meaning |
|---|---|---|
| `--dataset` | `v1` | the dataset folder under `lab/datasets` |
| `--model` | `defaultReviewer` in models.json | the reviewing model, a key from models.json |
| `--judge` | `defaultJudge` in models.json | the judging model; keep it the same across runs you compare |
| `--cases` | all | only cases whose id starts with this |
| `--repeat` | 1 | review every case this many times, to see how much the scores move between runs |
| `--max-cost` | 2.00 | dollars; the run refuses to start when the estimate is over it, and stops before a case would go over it |
| `--estimate` | | print the estimate and stop |

The estimate is deliberately on the high side: the first live run on the 11 C# cases was estimated at $0.29 and cost $0.11, almost all of it the judge.

## How a review is scored

The judge sees the numbered file, the labels and the findings, and gives every finding one verdict:

| Verdict | Meaning |
|---|---|
| `match` | the finding describes a labelled defect. It only counts when the finding's lines are within 3 lines of the label's; otherwise it becomes `real` and is counted as "outside the window" |
| `duplicate` | a second finding for a label that is already found |
| `real` | a genuine defect with no label: a good finding, and a candidate for a new label |
| `false_alarm` | wrong, not applicable, or only a style or preference remark |

| Measure | Definition |
|---|---|
| Recall | labels found / labels |
| Precision | findings that are not false alarms / findings |
| Strict precision | matches and duplicates / findings |
| F1 | the harmonic mean of recall and precision |
| False alarms per clean file | false alarms on the clean files / clean files |

Read the results per group as well as overall: the two dense files hold almost half the labels, and the clean files only measure false alarms.
