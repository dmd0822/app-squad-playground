namespace TravelAssistant.Abstractions;

/// <summary>
/// Shared context passed to all agents during a conversation.
/// </summary>
public class TravelContext
{
    /// <summary>
    /// Unique identifier for this conversation session.
    /// </summary>
    public string SessionId { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// User's preferred destination (if known).
    /// </summary>
    public string? Destination { get; set; }

    /// <summary>
    /// Planned departure date (if known).
    /// </summary>
    public DateOnly? DepartureDate { get; set; }

    /// <summary>
    /// Planned return date (if known).
    /// </summary>
    public DateOnly? ReturnDate { get; set; }

    /// <summary>
    /// Number of travelers.
    /// </summary>
    public int TravelerCount { get; set; } = 1;

    /// <summary>
    /// Budget constraints (if specified).
    /// </summary>
    public decimal? MaxBudget { get; set; }

    /// <summary>
    /// Previous responses in this conversation for context.
    /// </summary>
    public List<AgentResponse> ConversationHistory { get; } = [];

    /// <summary>
    /// Arbitrary key-value store for agent-specific state.
    /// </summary>
    public Dictionary<string, object> Metadata { get; } = [];
}
