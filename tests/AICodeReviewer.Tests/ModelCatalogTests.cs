using AICodeReviewer.Services.Reviewing;

namespace AICodeReviewer.Tests;

public class ModelCatalogTests
{
    [Fact]
    public void The_shipped_catalog_has_the_reviewer_and_the_judge()
    {
        var catalog = ModelCatalog.LoadDefault();

        var reviewer = catalog.Get(catalog.DefaultReviewer);
        Assert.Equal("claude-haiku-5-5", reviewer.Id);
        Assert.Equal("anthropic", reviewer.Provider);
        Assert.Equal((0.10m, 0.50m), (reviewer.InputPerMillion, reviewer.OutputPerMillion));

        var judge = catalog.Get(catalog.DefaultJudge);
        Assert.Equal("claude-sonnet-5-5", judge.Id);
        Assert.Equal((2.00m, 10.00m), (judge.InputPerMillion, judge.OutputPerMillion));
    }

    [Fact]
    public void Every_model_has_a_known_provider_and_a_unique_key()
    {
        var catalog = ModelCatalog.LoadDefault();

        Assert.All(catalog.Models, m => Assert.Contains(m.Provider, ModelCatalog.Providers));
        Assert.Equal(catalog.Models.Count, catalog.Models.Select(m => m.Key).Distinct().Count());
    }

    [Fact]
    public void An_unknown_provider_is_refused()
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, """{"models": [{"key": "x", "provider": "somewhere", "id": "x"}]}""");

        var error = Assert.Throws<InvalidOperationException>(() => ModelCatalog.Load(path));
        Assert.Contains("unknown provider 'somewhere'", error.Message);
    }

    [Fact]
    public void An_unknown_model_names_the_known_ones()
    {
        var error = Assert.Throws<InvalidOperationException>(() => ModelCatalog.LoadDefault().Get("gpt-3"));

        Assert.Contains("claude-haiku-5-5", error.Message);
    }
}
