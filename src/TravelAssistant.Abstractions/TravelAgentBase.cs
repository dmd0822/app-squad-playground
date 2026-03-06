namespace TravelAssistant.Abstractions;

/// <summary>
/// Base class for travel agents providing common functionality.
/// </summary>
public abstract class TravelAgentBase : ITravelAgent
{
    protected readonly IPromptLoader PromptLoader;

    protected TravelAgentBase(IPromptLoader promptLoader)
    {
        PromptLoader = promptLoader;
    }

    public abstract string AgentId { get; }
    public abstract string DisplayName { get; }

    public abstract Task<AgentResponse> ProcessAsync(
        string query,
        TravelContext context,
        CancellationToken cancellationToken = default);

    public abstract bool CanHandle(string query, TravelContext context);

    /// <summary>
    /// Helper method to create a successful response.
    /// </summary>
    protected AgentResponse CreateResponse(string message, object? data = null, double confidence = 1.0)
    {
        return new AgentResponse
        {
            AgentId = AgentId,
            Success = true,
            Message = message,
            Data = data,
            Confidence = confidence
        };
    }

    /// <summary>
    /// Helper method to create an error response.
    /// </summary>
    protected AgentResponse CreateErrorResponse(string errorMessage)
    {
        return new AgentResponse
        {
            AgentId = AgentId,
            Success = false,
            Message = "I encountered an error while processing your request.",
            ErrorMessage = errorMessage,
            Confidence = 0.0
        };
    }
}
