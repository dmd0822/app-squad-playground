using TravelAssistant.Abstractions;

namespace TravelAssistant.Host.Orchestration;

/// <summary>
/// Orchestrates user queries across multiple travel agents.
/// Uses a simple fan-out pattern: queries are sent to all agents
/// that claim they can handle the query.
/// </summary>
public class TravelOrchestrator
{
    private readonly IEnumerable<ITravelAgent> _agents;
    private readonly IPromptLoader _promptLoader;

    public TravelOrchestrator(
        IEnumerable<ITravelAgent> agents,
        IPromptLoader promptLoader)
    {
        _agents = agents;
        _promptLoader = promptLoader;
    }

    /// <summary>
    /// Processes a user query by routing it to appropriate agents.
    /// </summary>
    public async Task<IReadOnlyList<AgentResponse>> ProcessQueryAsync(
        string query,
        TravelContext? context = null,
        CancellationToken cancellationToken = default)
    {
        context ??= new TravelContext();
        
        // Find agents that can handle this query
        var capableAgents = _agents
            .Where(agent => agent.CanHandle(query, context))
            .ToList();

        // If no specific agents match, send to all agents
        if (capableAgents.Count == 0)
        {
            capableAgents = _agents.ToList();
        }

        // Fan out to all capable agents in parallel
        var tasks = capableAgents.Select(agent => 
            agent.ProcessAsync(query, context, cancellationToken));

        var responses = await Task.WhenAll(tasks);

        // Add responses to conversation history
        foreach (var response in responses)
        {
            context.ConversationHistory.Add(response);
        }

        return responses;
    }

    /// <summary>
    /// Gets a list of all registered agents.
    /// </summary>
    public IReadOnlyList<ITravelAgent> GetRegisteredAgents()
    {
        return _agents.ToList();
    }
}
