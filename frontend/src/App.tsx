import { useState } from 'react';
import { QueryClient, QueryClientProvider, useMutation } from '@tanstack/react-query';
import './App.css';
import { SearchForm } from './components/SearchForm';
import { PoiResults } from './components/PoiResults';
import { FlightResults } from './components/FlightResults';
import { HotelResults } from './components/HotelResults';
import { travelApi } from './services/travelApi';
import type { TravelSearchRequest, TravelSearchResponse } from './types/travel';

const queryClient = new QueryClient();

function TravelApp() {
  const [searchResults, setSearchResults] = useState<TravelSearchResponse | null>(null);

  const searchMutation = useMutation({
    mutationFn: (request: TravelSearchRequest) => travelApi.search(request),
    onSuccess: (data) => {
      setSearchResults(data);
    },
  });

  const handleSearch = (request: TravelSearchRequest) => {
    searchMutation.mutate(request);
  };

  return (
    <div className="app">
      <header>
        <h1>Travel Assistant</h1>
        <p>Find flights, hotels, and points of interest for your next trip</p>
      </header>

      <main>
        <section className="search-section">
          <SearchForm onSearch={handleSearch} isLoading={searchMutation.isPending} />
        </section>

        {searchMutation.isError && (
          <section className="error-section">
            <p>Error: {(searchMutation.error as Error).message}</p>
          </section>
        )}

        {searchResults && (
          <section className="results-section">
            <h2>Results for {searchResults.destination}</h2>
            
            {searchResults.errors.length > 0 && (
              <div className="errors">
                {searchResults.errors.map((error, index) => (
                  <p key={index} className="error">{error}</p>
                ))}
              </div>
            )}

            <div className="results-grid">
              <PoiResults items={searchResults.pointsOfInterest} />
              <FlightResults flights={searchResults.flights} />
              <HotelResults hotels={searchResults.hotels} />
            </div>
          </section>
        )}
      </main>

      <footer>
        <p>Travel Assistant Squad</p>
      </footer>
    </div>
  );
}

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <TravelApp />
    </QueryClientProvider>
  );
}

export default App;
