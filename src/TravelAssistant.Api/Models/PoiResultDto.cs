namespace TravelAssistant.Api.Models;

/// <summary>
/// Points of interest results from the POI agent.
/// </summary>
public record PoiResultDto
{
    /// <summary>
    /// Whether the POI agent successfully processed the request.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Natural language summary of the results.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// List of points of interest found.
    /// </summary>
    public List<PoiItem> Items { get; init; } = [];
}

/// <summary>
/// Individual point of interest.
/// </summary>
public record PoiItem
{
    /// <summary>
    /// Name of the attraction or place.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Category (e.g., museum, restaurant, park).
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    /// Description of the place.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Rating (0-5 scale).
    /// </summary>
    public double? Rating { get; init; }

    /// <summary>
    /// Address or location.
    /// </summary>
    public string? Address { get; init; }
}
