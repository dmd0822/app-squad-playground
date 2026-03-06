using System.Text.RegularExpressions;
using Azure.AI.Projects;
using Azure.AI.Projects.OpenAI;
using OpenAI.Responses;
using TravelAssistant.Abstractions;

namespace TravelAssistant.Agents.HotelSearch;

/// <summary>
/// Agent specialized in searching for hotels, accommodations, and lodging options.
/// Uses Azure AI Foundry's agent service to load and execute the hotel search prompt.
/// </summary>
public class HotelAgent : TravelAgentBase
{
    /// <summary>Unique identifier for this agent.</summary>
    public const string AgentName = "hotel";

    private const string PromptName = "search";
    private const string DefaultModelDeployment = "gpt-4o";

    private static readonly string[] Keywords =
        ["hotel", "stay", "accommodation", "lodging", "room", "resort", "motel",
         "airbnb", "hostel", "inn", "bed and breakfast", "bnb", "check in", "check out"];

    private readonly AIProjectClient? _projectClient;
    private readonly string _modelDeploymentName;
    private string? _registeredAgentName;
    private readonly SemaphoreSlim _registrationLock = new(1, 1);

    /// <summary>
    /// Initialises the agent with only a prompt loader. Suitable for testing and
    /// environments where Azure AI Foundry integration is not yet configured.
    /// </summary>
    /// <param name="promptLoader">Service used to load YAML prompt templates.</param>
    public HotelAgent(IPromptLoader promptLoader)
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
    public HotelAgent(
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
    public override string DisplayName => "Hotel Search";

    /// <inheritdoc/>
    public override bool CanHandle(string query, TravelContext context)
    {
        var lower = query.ToLowerInvariant();
        return Keywords.Any(keyword => lower.Contains(keyword));
    }

    /// <summary>
    /// Processes a hotel search query. Loads the system prompt from YAML,
    /// substitutes context variables, and invokes the Azure AI Foundry agent.
    /// </summary>
    /// <param name="query">Natural-language hotel search request.</param>
    /// <param name="context">Shared travel context (destination, dates, guests, budget).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// An <see cref="AgentResponse"/> whose <c>Data</c> property is a
    /// <see cref="HotelSearchResult"/> containing the raw AI response and
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

                var data = new HotelSearchResult
                {
                    Destination = context.Destination ?? ExtractDestination(query) ?? query,
                    CheckIn = context.DepartureDate,
                    CheckOut = context.ReturnDate,
                    Guests = context.TravelerCount,
                    PreferredStarRating = GetStarRating(context),
                    BudgetPerNight = context.MaxBudget,
                    Hotels = [],
                    RawResponse = message
                };

                return CreateResponse(message, data: data, confidence: 0.9);
            }

            // Graceful fallback when no Azure AI Foundry client is configured.
            var fallbackData = new HotelSearchResult
            {
                Destination = context.Destination ?? ExtractDestination(query) ?? query,
                CheckIn = context.DepartureDate,
                CheckOut = context.ReturnDate,
                Guests = context.TravelerCount,
                PreferredStarRating = GetStarRating(context),
                BudgetPerNight = context.MaxBudget,
                Hotels = []
            };

            return CreateResponse(
                "Hotel search prompt loaded. Configure an Azure AI Foundry project client for live recommendations.",
                data: fallbackData,
                confidence: 0.5);
        }
        catch (Exception ex)
        {
            return CreateErrorResponse($"Hotel search failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Lazily registers (or re-uses) the hotel agent in Azure AI Foundry.
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

        var destination = context.Destination
            ?? ExtractDestination(query)
            ?? "not specified";

        var checkIn = context.DepartureDate?.ToString("yyyy-MM-dd") ?? "flexible";
        var checkOut = context.ReturnDate?.ToString("yyyy-MM-dd") ?? "flexible";
        var guests = context.TravelerCount.ToString();
        var starRating = GetStarRatingString(context);
        var budget = context.MaxBudget.HasValue
            ? $"${context.MaxBudget:F0} per night"
            : "flexible";

        return template.UserMessageTemplate
            .Replace("{{destination}}", destination)
            .Replace("{{check_in}}", checkIn)
            .Replace("{{check_out}}", checkOut)
            .Replace("{{guests}}", guests)
            .Replace("{{star_rating}}", starRating)
            .Replace("{{budget_per_night}}", budget)
            .Replace("{{query}}", query);
    }

    /// <summary>
    /// Simple heuristic to extract a destination from a free-text query,
    /// e.g., "hotels in Paris" → "Paris".
    /// </summary>
    private static string? ExtractDestination(string query)
    {
        var match = Regex.Match(
            query,
            @"\bin\s+([A-Z][a-zA-Z\s,]+?)(?:\s+from|\s+for|\s+on|\s*\?|\s*$)",
            RegexOptions.IgnoreCase);

        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    /// <summary>Returns the numeric star rating from context metadata, or <c>null</c>.</summary>
    private static int? GetStarRating(TravelContext context)
    {
        if (context.Metadata.TryGetValue("star_rating", out var sr))
        {
            return sr switch
            {
                int i => i,
                string s when int.TryParse(s, out var parsed) => parsed,
                _ => null
            };
        }

        return null;
    }

    /// <summary>Returns a human-readable star-rating string for prompt substitution.</summary>
    private static string GetStarRatingString(TravelContext context)
    {
        var rating = GetStarRating(context);
        return rating.HasValue ? $"{rating}-star" : "any star rating";
    }
}
