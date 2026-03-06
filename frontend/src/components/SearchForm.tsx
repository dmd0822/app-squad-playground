import { useState } from 'react';
import type { TravelSearchRequest } from '../types/travel';

interface SearchFormProps {
  onSearch: (request: TravelSearchRequest) => void;
  isLoading: boolean;
}

export function SearchForm({ onSearch, isLoading }: SearchFormProps) {
  const [destination, setDestination] = useState('');
  const [origin, setOrigin] = useState('');
  const [checkIn, setCheckIn] = useState('');
  const [checkOut, setCheckOut] = useState('');
  const [guests, setGuests] = useState(1);
  const [interests, setInterests] = useState('');

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!destination.trim()) return;

    const request: TravelSearchRequest = {
      destination: destination.trim(),
      guests,
      ...(origin.trim() && { origin: origin.trim() }),
      ...(checkIn && { checkIn }),
      ...(checkOut && { checkOut }),
      ...(interests.trim() && { interests: interests.trim() }),
    };
    onSearch(request);
  };

  const fieldStyle: React.CSSProperties = {
    display: 'flex',
    flexDirection: 'column',
    gap: '4px',
  };

  const inputStyle: React.CSSProperties = {
    padding: '8px 10px',
    fontSize: '14px',
    border: '1px solid #ccc',
    borderRadius: '4px',
    width: '100%',
    boxSizing: 'border-box',
  };

  const labelStyle: React.CSSProperties = {
    fontSize: '13px',
    fontWeight: 600,
    color: '#444',
  };

  return (
    <form onSubmit={handleSubmit} className="search-form" style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
      <div style={fieldStyle}>
        <label htmlFor="destination" style={labelStyle}>Where are you going? *</label>
        <input
          id="destination"
          type="text"
          value={destination}
          onChange={e => setDestination(e.target.value)}
          placeholder="e.g. Paris, Tokyo, New York"
          required
          disabled={isLoading}
          style={inputStyle}
        />
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px' }}>
        <div style={fieldStyle}>
          <label htmlFor="origin" style={labelStyle}>Flying from</label>
          <input
            id="origin"
            type="text"
            value={origin}
            onChange={e => setOrigin(e.target.value)}
            placeholder="e.g. London, LAX"
            disabled={isLoading}
            style={inputStyle}
          />
        </div>

        <div style={fieldStyle}>
          <label htmlFor="guests" style={labelStyle}>Guests</label>
          <input
            id="guests"
            type="number"
            min={1}
            value={guests}
            onChange={e => setGuests(Number(e.target.value))}
            disabled={isLoading}
            style={inputStyle}
          />
        </div>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px' }}>
        <div style={fieldStyle}>
          <label htmlFor="checkIn" style={labelStyle}>Check-in</label>
          <input
            id="checkIn"
            type="date"
            value={checkIn}
            onChange={e => setCheckIn(e.target.value)}
            disabled={isLoading}
            style={inputStyle}
          />
        </div>

        <div style={fieldStyle}>
          <label htmlFor="checkOut" style={labelStyle}>Check-out</label>
          <input
            id="checkOut"
            type="date"
            value={checkOut}
            onChange={e => setCheckOut(e.target.value)}
            disabled={isLoading}
            style={inputStyle}
          />
        </div>
      </div>

      <div style={fieldStyle}>
        <label htmlFor="interests" style={labelStyle}>Interests (museums, food, outdoor...)</label>
        <input
          id="interests"
          type="text"
          value={interests}
          onChange={e => setInterests(e.target.value)}
          placeholder="e.g. history, street food, hiking"
          disabled={isLoading}
          style={inputStyle}
        />
      </div>

      <button
        type="submit"
        disabled={isLoading || !destination.trim()}
        style={{
          padding: '10px 24px',
          fontSize: '15px',
          fontWeight: 600,
          background: isLoading ? '#aaa' : '#1a6fdb',
          color: '#fff',
          border: 'none',
          borderRadius: '4px',
          cursor: isLoading ? 'not-allowed' : 'pointer',
          alignSelf: 'flex-start',
        }}
      >
        {isLoading ? 'Searching...' : 'Search'}
      </button>
    </form>
  );
}

export default SearchForm;
