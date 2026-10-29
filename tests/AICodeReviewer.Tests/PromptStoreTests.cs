using AICodeReviewer.Services;
using AICodeReviewer.Services.Reviewing;

namespace AICodeReviewer.Tests;

public class PromptStoreTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), $"prompts-{Guid.NewGuid():N}");

    public PromptStoreTests()
    {
        // A copy of the shipped prompts, so tests can add versions without touching the real ones
        foreach (var file in Directory.GetFiles(PromptStore.LoadDefault().Folder, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(_folder, Path.GetRelativePath(PromptStore.LoadDefault().Folder, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }
    }

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public void Every_language_has_an_active_version_with_text()
    {
        var store = PromptStore.LoadDefault();

        foreach (var language in new[] { "CSharp", "Java", "VbNet", "Sql", "JavaScript", "TypeScript", "React", PromptStore.General })
        {
            Assert.False(string.IsNullOrWhiteSpace(store.ActiveText(language)));
            Assert.Contains(store.Versions, v => v.Language == language && v.Version == store.ActiveVersion(language));
        }
    }

    [Fact]
    public void A_language_without_prompts_of_its_own_gets_the_general_prompt()
    {
        var store = PromptStore.LoadDefault();

        Assert.Equal(store.ActiveText(PromptStore.General), store.ActiveText("Python"));
    }

    [Fact]
    public void A_new_version_copies_the_active_one_and_is_not_active_until_kept()
    {
        var store = PromptStore.Load(_folder);

        var version = store.NewVersion("VbNet", "Fewer style remarks", today: new DateOnly(2026, 10, 29));

        Assert.Equal(2, version);
        Assert.Equal(store.Text("VbNet", 1), store.Text("VbNet", 2));
        Assert.Equal(1, store.ActiveVersion("VbNet"));
        var saved = PromptStore.Load(_folder).Versions.Single(v => v.Language == "VbNet" && v.Version == 2);
        Assert.Equal((1, "2026-10-29", "Fewer style remarks"), (saved.BasedOn, saved.Created, saved.Notes));
    }

    [Fact]
    public void A_kept_decision_makes_the_version_active_and_a_rolled_back_one_does_not()
    {
        var store = PromptStore.Load(_folder);
        store.NewVersion("VbNet", "a");
        store.NewVersion("VbNet", "b");

        store.Decide("VbNet", 2, Decision("rolled_back"));
        store.Decide("VbNet", 3, Decision("kept"));

        var saved = PromptStore.Load(_folder);
        Assert.Equal(3, saved.ActiveVersion("VbNet"));
        Assert.Equal("rolled_back", saved.Versions.Single(v => v.Language == "VbNet" && v.Version == 2).Decision!.Result);
    }

    [Fact]
    public void Trying_a_version_does_not_change_the_saved_active_version()
    {
        var store = PromptStore.Load(_folder);
        store.NewVersion("VbNet", "a");

        var trial = store.WithActive(new Dictionary<string, int> { ["VbNet"] = 2 });

        Assert.Equal(2, trial.ActiveVersion("VbNet"));
        Assert.Equal(1, PromptStore.Load(_folder).ActiveVersion("VbNet"));
        Assert.Throws<InvalidOperationException>(() => store.WithActive(new Dictionary<string, int> { ["VbNet"] = 9 }));
    }

    [Fact]
    public void The_hash_ignores_line_endings()
    {
        Assert.Equal(PromptStore.Hash("a\nb\n"), PromptStore.Hash("a\r\nb"));
        Assert.NotEqual(PromptStore.Hash("a\nb"), PromptStore.Hash("a\nc"));
    }

    [Fact]
    public void A_prompt_saved_in_settings_overrides_the_versioned_one_unless_the_lab_turns_it_off()
    {
        var configuration = new FakeConfiguration();
        configuration.Settings.AzureOpenAI.LanguagePrompts.Sql.SystemPrompt = "My own SQL prompt";
        var store = PromptStore.LoadDefault();

        var app = new PromptManagementService(new LanguageDetectionService(), configuration, store);
        var lab = new PromptManagementService(new LanguageDetectionService(), configuration, store, useCustomPrompts: false);

        Assert.Equal("My own SQL prompt", app.GetSystemPrompt("Report.sql"));
        Assert.Equal(store.ActiveText("Sql"), lab.GetSystemPrompt("Report.sql"));
        Assert.Equal(store.ActiveText("CSharp"), app.GetSystemPrompt("Order.cs"));
        Assert.Equal("React", app.PromptLanguage("List.tsx"));
        Assert.Equal(PromptStore.General, app.PromptLanguage("notes.txt"));
    }

    private static PromptDecision Decision(string result) =>
        new(result, "2026-10-29", "test", 0.9, 0.9, 0.5, 0.6, 0.64, 0.72, ["base.json"], ["candidate.json"]);
}
