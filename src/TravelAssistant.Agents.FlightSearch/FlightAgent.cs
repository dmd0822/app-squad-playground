using Azure.AI.Projects;
using Azure.AI.Projects.OpenAI;
using OpenAI.Responses;
using System.Text.Json;
using TravelAssistant.Abstractions;

namespace TravelAssistant.Agents.FlightSearch;

/// <summary>
/// Agent specialized in searching for flights, comparing prices,
/// and providing flight-related travel information.
/// Uses Azure AI Foundry's agent service to load and execute the flight search prompt.
/// </summary>
public class FlightAgent : TravelAgentBase
{
    /// <summary>Unique identifier for this agent.</summary>
    public const string AgentName = "flight";

    private const string PromptName = "search";
    private const string DefaultModelDeployment = "gpt-4o";

    private static readonly string[] Keywords =
        ["flight", "fly", "airline", "airport", "plane", "departure", "arrival",
         "layover", "nonstop", "round trip", "one way", "ticket", "booking"];

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly AIProjectClient? _projectClient;
    private readonly string _modelDeploymentName;
    private string? _registeredAgentName;
    private readonly SemaphoreSlim _registrationLock = new(1, 1);

    /// <summary>
    /// Initializes the agent with only a prompt loader. Suitable for testing and
    /// environments where Azure AI Foundry integration is not yet configured.
    /// </summary>
    /// <param name="promptLoader">Service used to load YAML prompt templates.</param>
    public FlightAgent(IPromptLoader promptLoader)
        : this(promptLoader, null, DefaultModelDeployment)
    {
    }

    /// <summary>
    /// Initializes the agent with full Azure AI Foundry integration.
    /// </summary>
    /// <param name="promptLoader">Service used to load YAML prompt templates.</param>
    /// <param name="projectClient">
    /// Authenticated Azure AI Foundry project client. When provided, the agent
    /// registers itself and executes queries through the Foundry Responses API.
    /// </param>
    /// <param name="modelDeploymentName">
    /// Name of the model deployment in the Foundry project (e.g., "gpt-4o").
    /// </param>
    public FlightAgent(
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
    public override string DisplayName => "Flight Search";

    /// <inheritdoc/>
    public override bool CanHandle(string query, TravelContext context)
    {
        var lowerQuery = query.ToLowerInvariant();
        return Keywords.Any(keyword => lowerQuery.Contains(keyword));
    }

    /// <summary>
    /// Processes a flight search query. Loads the system prompt from YAML,
    /// substitutes context variables, and invokes the Azure AI Foundry agent.
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
    /// An <see cref="AgentResponse"/> whose <c>Data</c> property is a
    /// <see cref="FlightSearchResult"/> containing the raw AI response and
    /// structured search metadata.
    /// </returns>
    public override async Task<AgentResponse> ProcessAsync(
        string query,
        TravelContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var template = await PromptLoader.LoadAsync(AgentId, PromptName, cancellationToken);

            var origin = GetMetaString(context, "Origin", "Not specified");
            var cabinClass = GetMetaString(context, "CabinClass", "Economy");

            var userMessage = BuildUserMessage(
                template.UserMessageTemplate ?? query,
                origin: origin,
                destination: context.Destination ?? "Not specified",
                departureDate: context.DepartureDate?.ToString("yyyy-MM-dd") ?? "Not specified",
                returnDate: context.ReturnDate?.ToString("yyyy-MM-dd") ?? "Not specified",
                passengers: context.TravelerCount.ToString(),
                cabinClass: cabinClass,
                query: query);

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
                var searchResult = ParseFlightSearchResult(message);

                return CreateResponse(
                    searchResult.Summary.Length > 0 ? searchResult.Summary : message,
                    data: searchResult,
                    confidence: 0.9);
            }

            // Graceful fallback when no Azure AI Foundry client is configured.
            var fallbackData = new FlightSearchResult
            {
                Summary = "Flight search prompt loaded. Configure Azure AI Foundry for live results.",
                Options = []
            };

            return CreateResponse(
                "Flight search prompt loaded. Configure Azure AI Foundry for live results.",
                data: fallbackData,
                confidence: 0.5);
        }
        catch (Exception ex)
        {
            return CreateErrorResponse($"Flight search failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Lazily registers (or re-uses) the flight agent in Azure AI Foundry.
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
