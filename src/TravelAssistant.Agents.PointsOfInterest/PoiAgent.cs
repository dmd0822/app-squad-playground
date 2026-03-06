using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using TravelAssistant.Abstractions;

namespace TravelAssistant.Agents.PointsOfInterest;

/// <summary>
/// Agent specialized in finding points of interest, attractions,
/// restaurants, and activities at a travel destination.
/// Uses Semantic Kernel to execute a prompt loaded from
/// <c>Prompts/search.prompt.yaml</c>.
/// </summary>
public class PoiAgent : TravelAgentBase
{
    /// <summary>
    /// Stable identifier used to register and route to this agent.
    /// </summary>
    public const string Id = "poi";

    /// <summary>
    /// Name of the prompt file (without extension) loaded at query time.
    /// </summary>
    private const string SearchPromptName = "search";

    private static readonly string[] Keywords =
        ["attraction", "restaurant", "thing to do", "activity", "landmark", "museum",
         "tour", "sight", "visit", "see", "explore", "poi", "place"];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly Kernel? _kernel;

    /// <summary>
    /// Initialises the POI agent.
    /// </summary>
    /// <param name="promptLoader">Service that loads prompt YAML files at runtime.</param>
    /// <param name="kernel">
    /// Semantic Kernel instance used to invoke the LLM.
    /// When <see langword="null"/> the agent loads prompts but cannot call the LLM
    /// (useful in unit tests that only exercise routing logic).
    /// </param>
    public PoiAgent(IPromptLoader promptLoader, Kernel? kernel = null)
        : base(promptLoader)
    {
        _kernel = kernel;
    }

    /// <inheritdoc/>
    public override string AgentId => Id;

    /// <inheritdoc/>
    public override string DisplayName => "Points of Interest";

    /// <inheritdoc/>
    /// <remarks>
    /// Returns <see langword="true"/> when the query contains at least one
    /// POI-related keyword (e.g. "attraction", "museum", "restaurant").
    /// </remarks>
    public override bool CanHandle(string query, TravelContext context)
    {
        var lowerQuery = query.ToLowerInvariant();
        return Keywords.Any(keyword => lowerQuery.Contains(keyword));
    }

    /// <summary>
    /// Processes a travel query and returns a list of recommended points of interest.
    /// </summary>
    /// <param name="query">The user's natural-language query.</param>
    /// <param name="context">
    /// Shared travel context containing destination, dates, and preferences.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// An <see cref="AgentResponse"/> whose <see cref="AgentResponse.Data"/> is a
    /// <see cref="PoiSearchResult"/> when the LLM call succeeds.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no Semantic Kernel instance was provided at construction time.
    /// </exception>
    public override async Task<AgentResponse> ProcessAsync(
        string query,
        TravelContext context,
        CancellationToken cancellationToken = default)
    {
        if (_kernel is null)
        {
            throw new InvalidOperationException(
                $"{nameof(PoiAgent)} requires a Semantic Kernel {nameof(Kernel)} instance " +
                "to process queries. Ensure a Kernel is registered in the service provider.");
        }

        // Load the prompt template from the YAML file — no strings hardcoded here.
        var promptTemplate = await PromptLoader.LoadAsync(AgentId, SearchPromptName, cancellationToken);

        // Build a chat history using the loaded system prompt.
        var chatHistory = new ChatHistory(promptTemplate.SystemPrompt);

        // Render the user message template with values from context + query.
        var userMessage = RenderUserMessage(promptTemplate.UserMessageTemplate, query, context);
        chatHistory.AddUserMessage(userMessage);

        try
        {
            var chatService = _kernel.GetRequiredService<IChatCompletionService>();

            var executionSettings = new PromptExecutionSettings
            {
                ModelId = promptTemplate.Settings.Model
            };

            var result = await chatService.GetChatMessageContentAsync(
                chatHistory,
                executionSettings,
                _kernel,
                cancellationToken);

            var responseText = result.Content ?? string.Empty;
            var poiResult = ParsePoiResult(responseText, context.Destination ?? query);

            return CreateResponse(
                BuildSummaryMessage(poiResult),
                data: poiResult,
                confidence: 0.9);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return CreateErrorResponse($"POI search failed: {ex.Message}");
        }
    }

    // ---------------------------------------------------------------------------
    // Private helpers
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Fills <c>{{placeholder}}</c> tokens in the user message template with
    /// runtime values from <paramref name="context"/> and the raw <paramref name="query"/>.
    /// </summary>
    private static string RenderUserMessage(string? template, string query, TravelContext context)
    {
        if (string.IsNullOrWhiteSpace(template))
            return query;

        var destination = context.Destination ?? "the destination";
        var travelDates = FormatTravelDates(context);
        var interests = context.Metadata.TryGetValue("interests", out var raw)
            ? raw?.ToString() ?? "general sightseeing"
            : "general sightseeing";

        return template
            .Replace("{{destination}}", destination)
            .Replace("{{travel_dates}}", travelDates)
            .Replace("{{interests}}", interests)
            .Replace("{{query}}", query);
    }

    /// <summary>
    /// Formats departure/return dates from context into a human-readable range.
    /// </summary>
    private static string FormatTravelDates(TravelContext context)
    {
        if (context.DepartureDate is null && context.ReturnDate is null)
            return "dates not specified";

        if (context.DepartureDate is not null && context.ReturnDate is not null)
            return $"{context.DepartureDate:yyyy-MM-dd} to {context.ReturnDate:yyyy-MM-dd}";

        if (context.DepartureDate is not null)
            return $"from {context.DepartureDate:yyyy-MM-dd}";

        return $"until {context.ReturnDate:yyyy-MM-dd}";
    }

    /// <summary>
    /// Attempts to parse the LLM response as <see cref="PoiSearchResult"/> JSON.
    /// If the response is not valid JSON the raw text is stored in
    /// <see cref="PoiSearchResult.Summary"/> and an empty POI list is returned.
    /// </summary>
    private static PoiSearchResult ParsePoiResult(string responseText, string destination)
    {
        try
        {
            // Extract the outermost JSON object in case the model wraps it in prose.
            var jsonStart = responseText.IndexOf('{');
            var jsonEnd = responseText.LastIndexOf('}');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = responseText[jsonStart..(jsonEnd + 1)];
                var parsed = JsonSerializer.Deserialize<PoiResultJson>(json, JsonOptions);

                if (parsed is not null)
                {
                    return new PoiSearchResult
                    {
                        Destination = parsed.Destination ?? destination,
                        Summary = parsed.Summary,
                        PointsOfInterest = parsed.PointsOfInterest?
                            .Where(p => p.Name is not null)
                            .Select(p => new PoiItem
                            {
                                Name = p.Name!,
                                Description = p.Description ?? string.Empty,
                                Category = p.Category ?? "general",
                                EstimatedVisitMinutes = p.EstimatedVisitMinutes,
                                Address = p.Address
                            })
                            .ToList() ?? []
                    };
                }
            }
        }
        catch (JsonException)
        {
            // Fall through — return raw text as summary.
        }

        return new PoiSearchResult
        {
            Destination = destination,
            Summary = responseText,
            PointsOfInterest = []
        };
    }

    /// <summary>
    /// Builds the natural-language <see cref="AgentResponse.Message"/> from a parsed result.
    /// </summary>
    private static string BuildSummaryMessage(PoiSearchResult result)
    {
        if (result.PointsOfInterest.Count == 0)
            return result.Summary ?? $"I found some points of interest in {result.Destination}.";

        var intro = result.Summary is not null
            ? result.Summary
            : $"Here are the top points of interest in {result.Destination}.";

        return $"{intro} I found {result.PointsOfInterest.Count} recommended place(s) to visit.";
    }

    // ---------------------------------------------------------------------------
    // JSON deserialization DTOs (internal, snake_case from LLM output)
    // ---------------------------------------------------------------------------

    private sealed class PoiResultJson
    {
        [JsonPropertyName("destination")]
        public string? Destination { get; set; }

        [JsonPropertyName("summary")]
        public string? Summary { get; set; }

        [JsonPropertyName("points_of_interest")]
        public List<PoiItemJson>? PointsOfInterest { get; set; }
    }

    private sealed class PoiItemJson
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("estimated_visit_minutes")]
        public int EstimatedVisitMinutes { get; set; }

        [JsonPropertyName("address")]
        public string? Address { get; set; }
    }
}

