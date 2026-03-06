namespace TravelAssistant.Abstractions;

/// <summary>
/// Standard response structure from any travel agent.
/// </summary>
public record AgentResponse
{
    /// <summary>
    /// The agent that produced this response.
    /// </summary>
    public required string AgentId { get; init; }

    /// <summary>
    /// Whether the agent successfully processed the query.
    /// </summary>
    public bool Success { get; init; } = true;

    /// <summary>
    /// The natural language response to show the user.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Structured data returned by the agent (e.g., list of hotels, flights).
    /// </summary>
    public object? Data { get; init; }

    /// <summary>
    /// Confidence score (0.0 to 1.0) indicating how well the agent handled the query.
    /// </summary>
    public double Confidence { get; init; } = 1.0;

    /// <summary>
    /// Error message if Success is false.
    /// </summary>
    public string? ErrorMessage { get; init; }
}
