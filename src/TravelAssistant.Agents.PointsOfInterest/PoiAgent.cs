using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.AI.Projects;
using Azure.AI.Projects.OpenAI;
using OpenAI.Responses;
using TravelAssistant.Abstractions;

namespace TravelAssistant.Agents.PointsOfInterest;

/// <summary>
/// Agent specialized in finding points of interest, attractions,
/// restaurants, and activities at a travel destination.
/// Uses Azure AI Foundry's agent service to load and execute the POI search prompt.
/// </summary>
public class PoiAgent : TravelAgentBase
{
    /// <summary>Unique identifier for this agent.</summary>
    public const string AgentName = "poi";

    private const string PromptName = "search";
    private const string DefaultModelDeployment = "gpt-4o";

    private static readonly string[] Keywords =
        ["attraction", "restaurant", "thing to do", "activity", "landmark", "museum",
         "tour", "sight", "visit", "see", "explore", "poi", "place"];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly AIProjectClient? _projectClient;
    private readonly string _modelDeploymentName;
    private string? _registeredAgentName;
    private readonly SemaphoreSlim _registrationLock = new(1, 1);

    /// <summary>
    /// Initialises the agent with only a prompt loader. Suitable for testing and
    /// environments where Azure AI Foundry integration is not yet configured.
    /// </summary>
    /// <param name="promptLoader">Service used to load YAML prompt templates.</param>
    public PoiAgent(IPromptLoader promptLoader)
        : this(promptLoader, null, DefaultModelDeployment)
    {
    }

    /// <summary>
    /// Initialises the agent with full Azure AI Foundry integration.
    /// </summary>
    /// <param name="promptLoader">Service used to load YAML prompt templates.</param>
    /// <param name="projectClient">
    /// Authenticated Azure AI Foundry project client. When provided, the agent
    /// registers itself and executes queries through the Foundry Responses API.
    /// </param>
    /// <param name="modelDeploymentName">
    /// Name of the model deployment in the Foundry project (e.g., "gpt-4o").
    /// </param>
    public PoiAgent(
        IPromptLoader promptLoader,
        AIProjectClient? projectClient,
        string modelDeploymentName = DefaultModelDeployment)
        : base(promptLoader)
    {
        _projectClient = projectClient;
        _modelDeploymentName = modelDeploymentName;
    }

    /// <inheritdoc/>
    public override string AgentId => AgentName;

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
    /// Processes a POI search query. Loads the system prompt from YAML,
    /// substitutes context variables, and invokes the Azure AI Foundry agent.
    /// </summary>
    /// <param name="query">Natural-language POI search request.</param>
    /// <param name="context">Shared travel context (destination, dates, interests).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// An <see cref="AgentResponse"/> whose <c>Data</c> property is a
    /// <see cref="PoiSearchResult"/> containing the structured POI list.
    /// </returns>
    public override async Task<AgentResponse> ProcessAsync(
        string query,
        TravelContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var template = await PromptLoader.LoadAsync(AgentId, PromptName, cancellationToken);
            var userMessage = BuildUserMessage(query, context, template);

            if (_projectClient is not null)
            {
                await EnsureAgentRegisteredAsync(template, cancellationToken);

                var responseClient = _projectClient.OpenAI
                    .GetProjectResponsesClientForAgent(_registeredAgentName!);

#pragma warning disable OPENAI001
                ResponseResult result = await responseClient.CreateResponseAsync(
                    new CreateResponseOptions([ResponseItem.CreateUserMessageItem(userMessage)]),
                    cancellationToken);
#pragma warning restore OPENAI001

                var message = result.GetOutputText();
                var data = ParsePoiResult(message, context.Destination ?? query);

                return CreateResponse(
                    BuildSummaryMessage(data),
                    data: data,
                    confidence: 0.9);
            }

            // Graceful fallback when no Azure AI Foundry client is configured.
            var fallbackData = new PoiSearchResult
            {
                Destination = context.Destination ?? query,
                Summary = "Prompt loaded. Configure Azure AI Foundry for live results.",
                PointsOfInterest = []
            };

            return CreateResponse(
                "POI search prompt loaded. Configure an Azure AI Foundry project client for live recommendations.",
                data: fallbackData,
                confidence: 0.5);
        }
        catch (Exception ex)
        {
            return CreateErrorResponse($"POI search failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Lazily registers (or re-uses) the POI agent in Azure AI Foundry.
    /// Thread-safe — only one registration request is sent even under concurrent load.
    /// </summary>
    private async Task EnsureAgentRegisteredAsync(
        PromptTemplate template,
        CancellationToken cancellationToken)
    {
        if (_registeredAgentName is not null) return;

        await _registrationLock.WaitAsync(cancellationToken);
        try
        {
            if (_registeredAgentName is not null) return;

            var definition = new PromptAgentDefinition(_modelDeploymentName)
            {
                Instructions = template.SystemPrompt
            };

            var agentVersion = await _projectClient!.Agents
                .CreateAgentVersionAsync(
                    agentName: AgentName,
                    options: new AgentVersionCreationOptions(definition),
                    cancellationToken: cancellationToken);

            _registeredAgentName = agentVersion.Value.Name;
        }
        finally
        {
            _registrationLock.Release();
        }
    }

    /// <summary>
    /// Substitutes <see cref="PromptTemplate.UserMessageTemplate"/> placeholders
    /// with values derived from <paramref name="context"/> and <paramref name="query"/>.
    /// Falls back to the raw query when no template is available.
    /// </summary>
    private static string BuildUserMessage(
        string query,
        TravelContext context,
        PromptTemplate template)
    {
        if (template.UserMessageTemplate is null) return query;

        var destination = context.Destination ?? "the destination";
        var travelDates = FormatTravelDates(context);
        var interests = context.Metadata.TryGetValue("interests", out var raw)
            ? raw?.ToString() ?? "general sightseeing"
            : "general sightseeing";

        return template.UserMessageTemplate
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

