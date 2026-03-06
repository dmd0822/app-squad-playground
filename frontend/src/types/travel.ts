// TypeScript interfaces mirroring the ASP.NET Core DTOs

export interface TravelSearchRequest {
  destination: string;
  origin?: string;
  checkIn?: string;  // ISO date string
  checkOut?: string; // ISO date string
  guests: number;
  interests?: string;
}

export interface PoiItem {
  name: string;
  description: string;
  category: string;
  estimatedVisitMinutes?: number;
  address?: string;
}

export interface FlightOption {
  airline: string;
  flightNumber: string;
  departureTime: string;
  arrivalTime: string;
  durationMinutes?: number;
  stops: number;
  estimatedPriceRange: string;
  cabinClass: string;
}

export interface HotelOption {
  name: string;
  starRating: number;
  pricePerNightRange: string;
  amenities: string[];
  locationDescription: string;
  cancellationPolicy: string;
}

export interface TravelSearchResponse {
  destination: string;
  searchedAt: string;
  pointsOfInterest: PoiItem[];
  flights: FlightOption[];
  hotels: HotelOption[];
  errors: string[];
}

export interface HealthResponse {
  status: string;
  team: string;
  timestamp: string;
}
