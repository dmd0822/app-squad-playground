// TODO: implement
// Axios API service layer for the Travel Assistant API

import axios from 'axios';
import type { TravelSearchRequest, TravelSearchResponse, HealthResponse } from '../types/travel';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000';

const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

export const travelApi = {
  // TODO: implement - call POST /api/travel/search
  search: async (request: TravelSearchRequest): Promise<TravelSearchResponse> => {
    const response = await apiClient.post<TravelSearchResponse>('/api/travel/search', request);
    return response.data;
  },

  // TODO: implement - call GET /api/travel/health
  health: async (): Promise<HealthResponse> => {
    const response = await apiClient.get<HealthResponse>('/api/travel/health');
    return response.data;
  },
};

export default travelApi;
