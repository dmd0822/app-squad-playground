namespace TravelAssistant.Abstractions;

/// <summary>
/// Service for loading prompt files at runtime.
/// Prompts are stored as .prompt.yaml files alongside each agent.
/// </summary>
public interface IPromptLoader
{
    /// <summary>
    /// Loads a prompt template by name.
    /// </summary>
    /// <param name="agentId">The agent requesting the prompt.</param>
    /// <param name="promptName">The prompt file name (without extension).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The prompt template content.</returns>
    Task<PromptTemplate> LoadAsync(
        string agentId, 
        string promptName, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a loaded prompt template.
/// </summary>
public record PromptTemplate
{
    /// <summary>
    /// The system prompt / instructions.
    /// </summary>
    public string SystemPrompt { get; init; } = string.Empty;

    /// <summary>
    /// Optional user message template with placeholders.
    /// </summary>
    public string? UserMessageTemplate { get; init; }

    /// <summary>
    /// Prompt metadata (model, temperature, etc.).
    /// </summary>
    public PromptSettings Settings { get; init; } = new();
}

/// <summary>
/// Settings extracted from a prompt file.
/// </summary>
public record PromptSettings
{
    /// <summary>
    /// Preferred model for this prompt (e.g., "gpt-4o").
    /// </summary>
    public string? Model { get; init; }

    /// <summary>
    /// Temperature setting for generation.
    /// </summary>
    public double Temperature { get; init; } = 0.7;

    /// <summary>
    /// Maximum tokens in the response.
    /// </summary>
    public int MaxTokens { get; init; } = 2048;
}
