import type { FlightOption } from '../types/travel';

interface FlightResultsProps {
  flights: FlightOption[];
}

function formatTime(iso: string): string {
  try {
    return new Date(iso).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  } catch {
    return iso;
  }
}

export function FlightResults({ flights }: FlightResultsProps) {
  return (
    <div className="result-panel">
      <h3>Flights</h3>
      {flights.length === 0 ? (
        <p className="empty-msg">No flight options found.</p>
      ) : (
        <ul className="result-list">
          {flights.map((flight, index) => (
            <li key={index} className="result-card">
              <div style={{ display: 'flex', alignItems: 'baseline', gap: '8px', flexWrap: 'wrap' }}>
                <span style={{ fontWeight: 700, fontSize: '15px' }}>{flight.airline}</span>
                <span style={{ fontSize: '13px', color: '#666' }}>{flight.flightNumber}</span>
                <span className="badge">{flight.cabinClass}</span>
              </div>
              <p style={{ margin: '6px 0 2px', fontSize: '14px' }}>
                🛫 {formatTime(flight.departureTime)} → 🛬 {formatTime(flight.arrivalTime)}
                {flight.durationMinutes != null && (
                  <span style={{ color: '#666', marginLeft: '8px' }}>({Math.floor(flight.durationMinutes / 60)}h {flight.durationMinutes % 60}m)</span>
                )}
              </p>
              <p style={{ margin: '2px 0', fontSize: '13px', color: '#555' }}>
                {flight.stops === 0 ? '✅ Direct' : `🔁 ${flight.stops} stop${flight.stops > 1 ? 's' : ''}`}
                {' · '}
                <strong>{flight.estimatedPriceRange}</strong>
              </p>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

export default FlightResults;
