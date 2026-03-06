namespace TravelAssistant.Api.Models;

/// <summary>
/// Hotel search results from the Hotel agent.
/// </summary>
public record HotelResultDto
{
    /// <summary>
    /// Whether the Hotel agent successfully processed the request.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Natural language summary of the results.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// List of hotels found.
    /// </summary>
    public List<HotelItem> Items { get; init; } = [];
}

/// <summary>
/// Individual hotel option.
/// </summary>
public record HotelItem
{
    /// <summary>
    /// Hotel name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Star rating (1-5).
    /// </summary>
    public int? StarRating { get; init; }

    /// <summary>
    /// Guest review score (0-10).
    /// </summary>
    public double? ReviewScore { get; init; }

    /// <summary>
    /// Price per night in USD.
    /// </summary>
    public decimal? PricePerNight { get; init; }

    /// <summary>
    /// Hotel address.
    /// </summary>
    public string? Address { get; init; }

    /// <summary>
    /// List of amenities.
    /// </summary>
    public List<string> Amenities { get; init; } = [];
}
