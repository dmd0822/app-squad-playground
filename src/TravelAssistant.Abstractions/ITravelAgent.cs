namespace TravelAssistant.Abstractions;

/// <summary>
/// Contract for all travel assistant agents. Implement this interface
/// to add a new agent to the orchestration layer.
/// </summary>
public interface ITravelAgent
{
    /// <summary>
    /// Unique identifier for this agent type (e.g., "poi", "flight", "hotel").
    /// </summary>
    string AgentId { get; }

    /// <summary>
    /// Human-readable name for display purposes.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Processes a user query and returns a structured response.
    /// </summary>
    /// <param name="query">The travel-related query from the user.</param>
    /// <param name="context">Shared context with conversation history and user preferences.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The agent's response.</returns>
    Task<AgentResponse> ProcessAsync(
        string query,
        TravelContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true if this agent can handle the given query.
    /// Used by the orchestrator to route queries to appropriate agents.
    /// </summary>
    bool CanHandle(string query, TravelContext context);
}
