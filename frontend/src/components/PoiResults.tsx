import type { PoiItem } from '../types/travel';

interface PoiResultsProps {
  items: PoiItem[];
}

export function PoiResults({ items }: PoiResultsProps) {
  return (
    <div className="result-panel">
      <h3>Points of Interest</h3>
      {items.length === 0 ? (
        <p className="empty-msg">No points of interest found.</p>
      ) : (
        <ul className="result-list">
          {items.map((item, index) => (
            <li key={index} className="result-card">
              <div style={{ display: 'flex', alignItems: 'baseline', gap: '8px', flexWrap: 'wrap' }}>
                <span style={{ fontWeight: 700, fontSize: '15px' }}>{item.name}</span>
                <span className="badge">{item.category}</span>
              </div>
              {item.description && <p style={{ margin: '4px 0', color: '#444', fontSize: '14px' }}>{item.description}</p>}
              {item.estimatedVisitMinutes != null && (
                <p style={{ margin: '2px 0', fontSize: '13px', color: '#666' }}>
                  ⏱ ~{item.estimatedVisitMinutes} min
                </p>
              )}
              {item.address && <p style={{ margin: '2px 0', fontSize: '13px', color: '#888' }}>📍 {item.address}</p>}
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

export default PoiResults;
