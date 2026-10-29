using System.Text.Json;

namespace AICodeReviewer.Services.Reviewing
{
    /// <summary>
    /// A request for an answer in JSON that follows a schema
    /// </summary>
    public record ModelRequest(string System, string User, string SchemaName, JsonElement Schema, int MaxTokens);

    /// <summary>
    /// The model's JSON answer and the tokens it took
    /// </summary>
    public record ModelAnswer(string Json, int InputTokens, int OutputTokens, string? StopReason);

    /// <summary>
    /// One provider's way of asking a model for structured JSON
    /// </summary>
    public interface IModelClient
    {
        Task<ModelAnswer> CompleteJsonAsync(ModelRequest request, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Builds the client for a model listed in Configuration/models.json
    /// </summary>
    public interface IModelClientFactory
    {
        IModelClient Create(Models.Review.ModelInfo model);
    }

    /// <summary>
    /// The model could not give a usable answer
    /// </summary>
    public class ModelAnswerException(string message, Exception? inner = null) : Exception(message, inner);
}
