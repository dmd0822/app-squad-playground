using Microsoft.AspNetCore.Mvc;
using TravelAssistant.Abstractions;
using TravelAssistant.Agents.FlightSearch;
using TravelAssistant.Agents.HotelSearch;
using TravelAssistant.Api.Models;
using TravelAssistant.Host.Orchestration;
using AgentPoiItem = TravelAssistant.Agents.PointsOfInterest.PoiItem;
using AgentPoiSearchResult = TravelAssistant.Agents.PointsOfInterest.PoiSearchResult;

namespace TravelAssistant.Api.Controllers;

/// <summary>
/// API controller for travel search operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TravelController : ControllerBase
{
    private readonly TravelOrchestrator _orchestrator;
    private readonly ILogger<TravelController> _logger;

    public TravelController(
        TravelOrchestrator orchestrator,
        ILogger<TravelController> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    /// <summary>
    /// Searches for travel options by querying all agents (POI, Flight, Hotel).
    /// </summary>
    /// <param name="request">The search parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Unified search response with results from all agents.</returns>
    [HttpPost("search")]
    [ProducesResponseType<TravelSearchResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TravelSearchResponse>> Search(
        [FromBody] TravelSearchRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Destination))
            return BadRequest("Destination is required.");

        _logger.LogInformation("Travel search request for destination: {Destination}", request.Destination);

        var context = new TravelContext
        {
            Destination = request.Destination,
            DepartureDate = request.CheckIn,
            ReturnDate = request.CheckOut,
            TravelerCount = request.Guests
        };

        if (!string.IsNullOrWhiteSpace(request.Origin))
            context.Metadata["Origin"] = request.Origin;

        if (!string.IsNullOrWhiteSpace(request.Interests))
            context.Metadata["interests"] = request.Interests;

        var query = $"Find travel options for {request.Destination}";
        var agentResponses = await _orchestrator.ProcessQueryAsync(query, context, cancellationToken);

        var errors = new List<string>();
        PoiResultDto? poiResult = null;
        FlightResultDto? flightResult = null;
        HotelResultDto? hotelResult = null;

        foreach (var response in agentResponses)
        {
            if (!response.Success && response.ErrorMessage is not null)
                errors.Add($"[{response.AgentId}] {response.ErrorMessage}");

            switch (response.AgentId)
            {
                case "poi":
                    poiResult = MapPoiResult(response);
                    break;
                case "flight":
                    flightResult = MapFlightResult(response);
                    break;
                case "hotel":
                    hotelResult = MapHotelResult(response);
                    break;
                default:
                    _logger.LogWarning("Unrecognised agent response: {AgentId}", response.AgentId);
                    break;
            }
        }

        return Ok(new TravelSearchResponse
        {
            Destination = request.Destination,
            PointsOfInterest = poiResult,
            Flights = flightResult,
            Hotels = hotelResult,
            Errors = errors
        });
    }

    // ── Private mapping helpers ───────────────────────────────────────────────

    /// <summary>Maps an <see cref="AgentResponse"/> from the POI agent to <see cref="PoiResultDto"/>.</summary>
    private static PoiResultDto MapPoiResult(AgentResponse response)
    {
        var items = new List<PoiItem>();

        if (response.Data is AgentPoiSearchResult poiData)
        {
            items = poiData.PointsOfInterest
                .Select(static (AgentPoiItem p) => new PoiItem
                {
                    Name = p.Name,
                    Category = p.Category,
                    Description = p.Description,
                    Address = p.Address
                })
                .ToList();
        }

        return new PoiResultDto
        {
            Success = response.Success,
            Message = response.Message,
            Items = items
        };
    }

    /// <summary>Maps an <see cref="AgentResponse"/> from the Flight agent to <see cref="FlightResultDto"/>.</summary>
    private static FlightResultDto MapFlightResult(AgentResponse response)
    {
        var items = new List<FlightItem>();

        if (response.Data is FlightSearchResult flightData)
        {
            items = flightData.Options
                .Select(static f => new FlightItem
                {
                    Airline = f.Airline,
                    FlightNumber = f.FlightNumber,
                    DepartureAirport = string.Empty,
                    ArrivalAirport = string.Empty,
                    DepartureTime = f.DepartureTime.DateTime,
                    ArrivalTime = f.ArrivalTime.DateTime,
                    Stops = f.Stops
                })
                .ToList();
        }

        return new FlightResultDto
        {
            Success = response.Success,
            Message = response.Message,
            Items = items
        };
    }

    /// <summary>Maps an <see cref="AgentResponse"/> from the Hotel agent to <see cref="HotelResultDto"/>.</summary>
    private static HotelResultDto MapHotelResult(AgentResponse response)
    {
        var items = new List<HotelItem>();

        if (response.Data is HotelSearchResult hotelData)
        {
            items = hotelData.Hotels
                .Select(static h => new HotelItem
                {
                    Name = h.Name,
                    StarRating = (int)Math.Round(h.StarRating),
                    Address = h.LocationDescription,
                    Amenities = h.Amenities.ToList()
                })
                .ToList();
        }

        return new HotelResultDto
        {
            Success = response.Success,
            Message = response.Message,
            Items = items
        };
    }

    /// <summary>
    /// Health check endpoint.
    /// </summary>
    /// <returns>Health status with team name.</returns>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        return Ok(new
        {
            Status = "Healthy",
            Team = "Travel Assistant Squad",
            Timestamp = DateTime.UtcNow
        });
    }
}
