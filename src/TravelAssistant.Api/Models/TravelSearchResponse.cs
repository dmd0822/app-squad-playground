namespace TravelAssistant.Api.Models;

/// <summary>
/// Unified response from all travel agents.
/// </summary>
public record TravelSearchResponse
{
    /// <summary>
    /// The destination that was searched.
    /// </summary>
    public required string Destination { get; init; }

    /// <summary>
    /// Points of interest results.
    /// </summary>
    public PoiResultDto? PointsOfInterest { get; init; }

    /// <summary>
    /// Flight search results.
    /// </summary>
    public FlightResultDto? Flights { get; init; }

    /// <summary>
    /// Hotel search results.
    /// </summary>
    public HotelResultDto? Hotels { get; init; }

    /// <summary>
    /// Any errors that occurred during the search.
    /// </summary>
    public List<string> Errors { get; init; } = [];
}
