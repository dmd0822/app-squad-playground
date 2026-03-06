namespace TravelAssistant.Api.Models;

/// <summary>
/// Request DTO for the travel search endpoint.
/// </summary>
public record TravelSearchRequest
{
    /// <summary>
    /// Travel destination (city, country, or region).
    /// </summary>
    public required string Destination { get; init; }

    /// <summary>
    /// Origin location for flights.
    /// </summary>
    public string? Origin { get; init; }

    /// <summary>
    /// Check-in date for hotels.
    /// </summary>
    public DateOnly? CheckIn { get; init; }

    /// <summary>
    /// Check-out date for hotels.
    /// </summary>
    public DateOnly? CheckOut { get; init; }

    /// <summary>
    /// Number of guests/travelers.
    /// </summary>
    public int Guests { get; init; } = 1;

    /// <summary>
    /// User's interests for POI search (e.g., "museums, restaurants, nightlife").
    /// </summary>
    public string? Interests { get; init; }
}
