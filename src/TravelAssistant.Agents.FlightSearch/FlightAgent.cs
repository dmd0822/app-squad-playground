using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;
using TravelAssistant.Abstractions;

namespace TravelAssistant.Agents.FlightSearch;

/// <summary>
/// Agent specialized in searching for flights, comparing prices,
/// and providing flight-related travel information.
/// Prompts are loaded from <c>Prompts/search.prompt.yaml</c> via <see cref="IPromptLoader"/>.
/// </summary>
public class FlightAgent : TravelAgentBase
{
    /// <summary>
    /// Unique identifier for the Flight Search agent.
    /// Use this constant when registering or referencing the agent — no magic strings.
    /// </summary>
    public const string AgentIdValue = "flight";

    private const string SearchPromptName = "search";

    private static readonly string[] Keywords =
        ["flight", "fly", "airline", "airport", "plane", "departure", "arrival",
         "layover", "nonstop", "round trip", "one way", "ticket", "booking"];

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly Kernel? _kernel;

    /// <summary>
    /// Initializes the Flight Search agent without a Semantic Kernel instance.
    /// <see cref="ProcessAsync"/> will return an error response if called without a kernel.
    /// Use this constructor only in unit-testing <see cref="CanHandle"/>.
    /// </summary>
    /// <param name="promptLoader">Service used to load prompt templates from YAML files.</param>
    public FlightAgent(IPromptLoader promptLoader) : base(promptLoader)
    {
    }

    /// <summary>
    /// Initializes the Flight Search agent with a Semantic Kernel instance for LLM execution.
    /// </summary>
    /// <param name="promptLoader">Service used to load prompt templates from YAML files.</param>
    /// <param name="kernel">The configured Semantic Kernel with a chat-completion backend.</param>
    public FlightAgent(IPromptLoader promptLoader, Kernel kernel) : base(promptLoader)
    {
        _kernel = kernel;
    }

    /// <inheritdoc/>
    public override string AgentId => AgentIdValue;

    /// <inheritdoc/>
    public override string DisplayName => "Flight Search";

    /// <inheritdoc/>
    public override bool CanHandle(string query, TravelContext context)
    {
        var lowerQuery = query.ToLowerInvariant();
        return Keywords.Any(keyword => lowerQuery.Contains(keyword));
    }

    /// <summary>
    /// Searches for flights matching the query and context.
    /// Loads the prompt from <c>Prompts/search.prompt.yaml</c>, substitutes template
    /// variables from <paramref name="context"/>, invokes the LLM, and returns a
    /// <see cref="AgentResponse"/> whose <c>Data</c> is a <see cref="FlightSearchResult"/>.
    /// </summary>
    /// <param name="query">The natural-language flight search request from the user.</param>
    /// <param name="context">
    /// Shared travel context. Relevant fields:
    /// <list type="bullet">
    ///   <item><description><c>Destination</c> — destination city or airport</description></item>
    ///   <item><description><c>DepartureDate</c> — outbound travel date</description></item>
    ///   <item><description><c>ReturnDate</c> — return travel date (null for one-way)</description></item>
    ///   <item><description><c>TravelerCount</c> — number of passengers</description></item>
    ///   <item><description><c>Metadata["Origin"]</c> — origin city or airport</description></item>
    ///   <item><description><c>Metadata["CabinClass"]</c> — preferred cabin (default: Economy)</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// An <see cref="AgentResponse"/> with a natural-language <c>Message</c>
    /// and <c>Data</c> typed as <see cref="FlightSearchResult"/>.
    /// </returns>
    public override async Task<AgentResponse> ProcessAsync(
        string query,
        TravelContext context,
        CancellationToken cancellationToken = default)
    {
        if (_kernel is null)
        {
            return CreateErrorResponse(
                "FlightAgent requires a Semantic Kernel instance. " +
                "Register it via the two-parameter constructor or DI.");
        }

        try
        {
            var promptTemplate = await PromptLoader.LoadAsync(AgentId, SearchPromptName, cancellationToken);

            var origin      = GetMetaString(context, "Origin",      "Not specified");
            var cabinClass  = GetMetaString(context, "CabinClass",  "Economy");

            var userMessage = BuildUserMessage(
                promptTemplate.UserMessageTemplate ?? query,
                origin:        origin,
                destination:   context.Destination   ?? "Not specified",
                departureDate: context.DepartureDate?.ToString("yyyy-MM-dd") ?? "Not specified",
                returnDate:    context.ReturnDate?.ToString("yyyy-MM-dd")    ?? "Not specified",
                passengers:    context.TravelerCount.ToString(),
                cabinClass:    cabinClass,
                query:         query);

            var chatHistory = new ChatHistory(promptTemplate.SystemPrompt);
            chatHistory.AddUserMessage(userMessage);

            var executionSettings = new PromptExecutionSettings
            {
                ModelId = promptTemplate.Settings.Model,
                ExtensionData = new Dictionary<string, object>
                {
                    ["temperature"] = promptTemplate.Settings.Temperature,
                    ["max_tokens"]  = promptTemplate.Settings.MaxTokens
                }
            };

            var chatService = _kernel.GetRequiredService<IChatCompletionService>();
            var result = await chatService.GetChatMessageContentAsync(
                chatHistory,
                executionSettings,
                _kernel,
                cancellationToken);

            var responseText = result.Content ?? string.Empty;
            var searchResult = ParseFlightSearchResult(responseText);

            return CreateResponse(
                message:    searchResult.Summary.Length > 0 ? searchResult.Summary : responseText,
                data:       searchResult,
                confidence: 0.9);
        }
        catch (Exception ex)
        {
            return CreateErrorResponse(ex.Message);
        }
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static string GetMetaString(TravelContext context, string key, string fallback)
        => context.Metadata.TryGetValue(key, out var value) && value is not null
            ? value.ToString()!
            : fallback;

    /// <summary>
    /// Substitutes all template placeholders with resolved values.
    /// Handles both the flight-specific variables and the legacy variables
    /// carried over from the POI template format.
    /// </summary>
    private static string BuildUserMessage(
        string template,
        string origin,
        string destination,
        string departureDate,
        string returnDate,
        string passengers,
        string cabinClass,
        string query)
    {
        return template
            .Replace("{{origin}}",         origin)
            .Replace("{{destination}}",    destination)
            .Replace("{{departure_date}}", departureDate)
            .Replace("{{return_date}}",    returnDate)
            .Replace("{{passengers}}",     passengers)
            .Replace("{{cabin_class}}",    cabinClass)
            // Legacy / shared variables
            .Replace("{{traveler_count}}", passengers)
            .Replace("{{budget}}",         "Not specified")
            .Replace("{{preferences}}",    cabinClass)
            .Replace("{{query}}",          query);
    }

    /// <summary>
    /// Attempts to parse a <see cref="FlightSearchResult"/> from the LLM response.
    /// Falls back gracefully when the model returns prose instead of JSON.
    /// </summary>
    private static FlightSearchResult ParseFlightSearchResult(string responseText)
    {
        // The prompt requests a JSON block wrapped in ```json ... ```.
        // Try to extract and parse it; fall back to a prose-only result.
        var jsonStart = responseText.IndexOf("```json", StringComparison.OrdinalIgnoreCase);
        var jsonEnd   = jsonStart >= 0
            ? responseText.IndexOf("```", jsonStart + 7, StringComparison.Ordinal)
            : -1;

        if (jsonStart >= 0 && jsonEnd > jsonStart)
        {
            var jsonBlock = responseText[(jsonStart + 7)..jsonEnd].Trim();
            try
            {
                var parsed = JsonSerializer.Deserialize<FlightSearchResult>(jsonBlock, JsonOptions);
                if (parsed is not null)
                    return parsed;
            }
            catch (JsonException)
            {
                // Fall through to prose fallback
            }
        }

        // Prose fallback: wrap the raw response text as the summary
        return new FlightSearchResult { Summary = responseText, Options = [] };
    }
}
