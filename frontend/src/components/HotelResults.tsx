import type { HotelOption } from '../types/travel';

interface HotelResultsProps {
  hotels: HotelOption[];
}

function StarRating({ count }: { count: number }) {
  return <span title={`${count} stars`}>{'⭐'.repeat(Math.min(count, 5))}</span>;
}

export function HotelResults({ hotels }: HotelResultsProps) {
  return (
    <div className="result-panel">
      <h3>Hotels</h3>
      {hotels.length === 0 ? (
        <p className="empty-msg">No hotel options found.</p>
      ) : (
        <ul className="result-list">
          {hotels.map((hotel, index) => (
            <li key={index} className="result-card">
              <div style={{ display: 'flex', alignItems: 'baseline', gap: '8px', flexWrap: 'wrap' }}>
                <span style={{ fontWeight: 700, fontSize: '15px' }}>{hotel.name}</span>
                <StarRating count={hotel.starRating} />
              </div>
              <p style={{ margin: '4px 0 2px', fontSize: '14px' }}>
                💰 {hotel.pricePerNightRange} / night
              </p>
              {hotel.locationDescription && (
                <p style={{ margin: '2px 0', fontSize: '13px', color: '#555' }}>📍 {hotel.locationDescription}</p>
              )}
              {hotel.amenities.length > 0 && (
                <p style={{ margin: '4px 0 2px', fontSize: '13px', color: '#555' }}>
                  ✔ {hotel.amenities.slice(0, 3).join(' · ')}
                </p>
              )}
              <p style={{ margin: '2px 0', fontSize: '12px', color: '#777' }}>
                🔖 {hotel.cancellationPolicy}
              </p>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

export default HotelResults;
