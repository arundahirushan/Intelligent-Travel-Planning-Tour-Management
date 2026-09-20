import React, { createContext, useContext, useState, useEffect } from 'react';
import { loginUser, setAuthToken } from '../services/api';

const AuthContext = createContext(null);

export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(null);
  const [token, setToken] = useState(null);
  const [loading, setLoading] = useState(true);

  // In a real app we might try to refresh the token on load, 
  // but as requested we are keeping it purely in memory.
  useEffect(() => {
    setLoading(false);
  }, []);

  const login = async (email, password) => {
    try {
      const data = await loginUser(email, password);
      
      // Expected backend response: { token, user: { id, fullName, email, role } }
      // This might differ based on the actual LoginResponseDto, but we store what we get.
      const authToken = data.token || data.Token;
      const userData = {
        id: data.id || data.Id || data.userId || data.UserId,
        fullName: data.fullName || data.FullName,
        email: data.email || data.Email,
        role: data.role || data.Role,
      };

      setToken(authToken);
      setUser(userData);
      setAuthToken(authToken);

      return { success: true, role: userData.role };
    } catch (error) {
      // Return clear error message
      let message = 'An unexpected error occurred during login.';
      if (error.response && error.response.data) {
        if (typeof error.response.data === 'string') {
          message = error.response.data;
        } else if (error.response.data.message) {
          message = error.response.data.message;
        } else if (error.response.data.title) {
          message = error.response.data.title;
        }
      } else if (error.message) {
        message = error.message;
      }
      return { success: false, error: message };
    }
  };

  const logout = () => {
    setUser(null);
    setToken(null);
    setAuthToken(null);
  };

  const value = {
    user,
    token,
    loading,
    login,
    logout,
    isAuthenticated: !!token,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
