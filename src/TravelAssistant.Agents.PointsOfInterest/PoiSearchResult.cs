namespace TravelAssistant.Agents.PointsOfInterest;

/// <summary>
/// Represents a single point of interest recommended for a travel destination.
/// </summary>
public record PoiItem
{
    /// <summary>
    /// The name of the point of interest.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// A short description of the place and why it is worth visiting.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Category of the POI — e.g. museum, food, outdoor, entertainment,
    /// shopping, cultural, historical, nature.
    /// </summary>
    public required string Category { get; init; }

    /// <summary>
    /// Estimated time a visitor should budget, in minutes.
    /// </summary>
    public int EstimatedVisitMinutes { get; init; }

    /// <summary>
    /// Street address, neighbourhood, or district. Null if unknown.
    /// </summary>
    public string? Address { get; init; }
}

/// <summary>
/// Structured result returned by the <see cref="PoiAgent"/>.
/// Contains the destination summary and a ranked list of recommended points of interest.
/// </summary>
public record PoiSearchResult
{
    /// <summary>
    /// The destination that was searched.
    /// </summary>
    public required string Destination { get; init; }

    /// <summary>
    /// One or two sentences summarising what makes this destination great for visitors.
    /// </summary>
    public string? Summary { get; init; }

    /// <summary>
    /// Ordered list of recommended points of interest (5–10 items).
    /// </summary>
    public IReadOnlyList<PoiItem> PointsOfInterest { get; init; } = [];
}
