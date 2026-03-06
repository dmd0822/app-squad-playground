namespace TravelAssistant.Agents.HotelSearch;

/// <summary>
/// Aggregated result from a hotel search query.
/// </summary>
public record HotelSearchResult
{
    /// <summary>
    /// The destination searched for (city, region, or address).
    /// </summary>
    public required string Destination { get; init; }

    /// <summary>
    /// Requested check-in date, or <c>null</c> if flexible.
    /// </summary>
    public DateOnly? CheckIn { get; init; }

    /// <summary>
    /// Requested check-out date, or <c>null</c> if flexible.
    /// </summary>
    public DateOnly? CheckOut { get; init; }

    /// <summary>
    /// Number of guests.
    /// </summary>
    public int Guests { get; init; } = 1;

    /// <summary>
    /// Preferred star rating, or <c>null</c> if any rating is acceptable.
    /// </summary>
    public int? PreferredStarRating { get; init; }

    /// <summary>
    /// Maximum budget per night in USD, or <c>null</c> if flexible.
    /// </summary>
    public decimal? BudgetPerNight { get; init; }

    /// <summary>
    /// Hotel options returned by the agent.
    /// </summary>
    public IReadOnlyList<HotelOption> Hotels { get; init; } = [];

    /// <summary>
    /// The raw natural-language response from the AI agent.
    /// </summary>
    public string RawResponse { get; init; } = string.Empty;
}

/// <summary>
/// A single hotel recommendation from the search agent.
/// </summary>
public record HotelOption
{
    /// <summary>
    /// Hotel name as known in travel sources.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Star rating (1–5). May be fractional (e.g., 4.5).
    /// </summary>
    public double StarRating { get; init; }

    /// <summary>
    /// Human-readable nightly price range, e.g., "$180–$240 / night".
    /// </summary>
    public required string PricePerNightRange { get; init; }

    /// <summary>
    /// Key amenities offered (e.g., "Free Wi-Fi", "Pool", "Gym").
    /// </summary>
    public IReadOnlyList<string> Amenities { get; init; } = [];

    /// <summary>
    /// Description of the hotel's location relative to landmarks or transit.
    /// </summary>
    public required string LocationDescription { get; init; }

    /// <summary>
    /// Cancellation policy summary (e.g., "Free cancellation up to 48 hours before check-in").
    /// </summary>
    public required string CancellationPolicy { get; init; }
}
