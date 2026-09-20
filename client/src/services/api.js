import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000/api';

const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Interceptor to add auth token if available (managed via AuthContext initially, but we can set it globally here)
export const setAuthToken = (token) => {
  if (token) {
    apiClient.defaults.headers.common['Authorization'] = `Bearer ${token}`;
  } else {
    delete apiClient.defaults.headers.common['Authorization'];
  }
};

export const registerUser = async (payload) => {
  // payload: { FullName, Email, Password, Role }
  const response = await apiClient.post('/Auth/register', payload);
  return response.data;
};

export const loginUser = async (email, password) => {
  // payload: { Email, Password }
  const response = await apiClient.post('/Auth/login', { Email: email, Password: password });
  return response.data;
};

export default apiClient;
