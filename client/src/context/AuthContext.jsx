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
      const responseData = await loginUser(email, password);
      
      // Handle ApiResponse wrapper if present
      const payload = responseData.data ? responseData.data : responseData;
      
      const authToken = payload.token || payload.Token;
      const userObj = payload.user || payload.User || payload;
      
      const userData = {
        id: userObj.id || userObj.Id || userObj.userId || userObj.UserId,
        fullName: userObj.fullName || userObj.FullName,
        email: userObj.email || userObj.Email,
        role: userObj.role || userObj.Role,
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
