namespace TravelAssistant.Api.Models;

/// <summary>
/// Flight search results from the Flight agent.
/// </summary>
public record FlightResultDto
{
    /// <summary>
    /// Whether the Flight agent successfully processed the request.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Natural language summary of the results.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// List of flights found.
    /// </summary>
    public List<FlightItem> Items { get; init; } = [];
}

/// <summary>
/// Individual flight option.
/// </summary>
public record FlightItem
{
    /// <summary>
    /// Airline name.
    /// </summary>
    public required string Airline { get; init; }

    /// <summary>
    /// Flight number.
    /// </summary>
    public string? FlightNumber { get; init; }

    /// <summary>
    /// Departure airport code.
    /// </summary>
    public required string DepartureAirport { get; init; }

    /// <summary>
    /// Arrival airport code.
    /// </summary>
    public required string ArrivalAirport { get; init; }

    /// <summary>
    /// Departure time.
    /// </summary>
    public DateTime? DepartureTime { get; init; }

    /// <summary>
    /// Arrival time.
    /// </summary>
    public DateTime? ArrivalTime { get; init; }

    /// <summary>
    /// Price in USD.
    /// </summary>
    public decimal? Price { get; init; }

    /// <summary>
    /// Number of stops (0 = direct).
    /// </summary>
    public int Stops { get; init; }
}
