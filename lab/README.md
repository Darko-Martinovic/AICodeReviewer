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

## Prompt versions

Each language's system prompt lives in `Configuration/Prompts/<Language>/v<N>.md`, and `Configuration/Prompts/prompts.json` says which version is active, what each version changed and what the lab decided about it. The app reviews with the active versions. A prompt saved in `appsettings.json` (for example by Train AI) still overrides the versioned one in the app, but the lab ignores such overrides so it always measures the versions.

The loop for improving a prompt:

```bash
dotnet run --project lab/AICodeReviewer.Lab -- prompt new CSharp --notes "what this version changes"
# edit Configuration/Prompts/CSharp/v<N>.md
dotnet run --project lab/AICodeReviewer.Lab -- run --prompt CSharp=<N> --repeat 2
dotnet run --project lab/AICodeReviewer.Lab -- prompt decide CSharp <N>
dotnet run --project lab/AICodeReviewer.Lab -- scoreboard
```

`run --prompt` reviews only that language's cases with the given version. `prompt decide` pools every saved run of the candidate and of the active version with the same reviewer and judge, scores both on the cases they share, and keeps the candidate only when:

- F1 rises by at least 0.02, and
- precision falls by no more than 0.02, and
- recall falls by no more than 0.02, so a quieter prompt cannot win by missing defects.

`--margin` changes the 0.02. The decision, its reason and the scores are written to `prompts.json`; a kept version becomes active.

Two cautions. The prompts are tuned and measured on the same 28 files, so a version can fit v1 rather than code in general; a second dataset kept out of tuning is the fix. And with only a few files per language, one label is 4 to 7 percent of recall, so use `--repeat` before deciding.
