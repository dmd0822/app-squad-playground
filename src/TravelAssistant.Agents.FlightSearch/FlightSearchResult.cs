namespace TravelAssistant.Agents.FlightSearch;

/// <summary>
/// Typed result model returned by the <see cref="FlightAgent"/> containing
/// a list of candidate flight options for the requested itinerary.
/// </summary>
public record FlightSearchResult
{
    /// <summary>
    /// Human-readable summary of the search results.
    /// </summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>
    /// Ordered list of flight options, best match first.
    /// </summary>
    public IReadOnlyList<FlightOption> Options { get; init; } = [];
}

/// <summary>
/// Represents a single flight option returned by the flight search.
/// All pricing is indicative guidance, not a live booking quote.
/// </summary>
public record FlightOption
{
    /// <summary>
    /// Airline name (e.g., "Delta Air Lines").
    /// </summary>
    public string Airline { get; init; } = string.Empty;

    /// <summary>
    /// IATA-style flight number (e.g., "DL 402").
    /// </summary>
    public string FlightNumber { get; init; } = string.Empty;

    /// <summary>
    /// Scheduled local departure date and time at the origin airport.
    /// </summary>
    public DateTimeOffset DepartureTime { get; init; }

    /// <summary>
    /// Scheduled local arrival date and time at the destination airport.
    /// </summary>
    public DateTimeOffset ArrivalTime { get; init; }

    /// <summary>
    /// Total elapsed flight time in minutes (block time).
    /// </summary>
    public int DurationMinutes { get; init; }

    /// <summary>
    /// Number of intermediate stops (0 = nonstop).
    /// </summary>
    public int Stops { get; init; }

    /// <summary>
    /// Indicative price range per person (e.g., "$350 – $420").
    /// This is AI-generated guidance, not a live fare.
    /// </summary>
    public string EstimatedPriceRange { get; init; } = string.Empty;

    /// <summary>
    /// Cabin class for this fare (e.g., "Economy", "Business", "First").
    /// </summary>
    public string CabinClass { get; init; } = string.Empty;
}
