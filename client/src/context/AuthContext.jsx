import React, { createContext, useContext, useState, useEffect, useCallback } from 'react';
import apiClient, { loginUser, setAuthToken } from '../services/api';

const AuthContext = createContext(null);

const SESSION_USER_KEY = 'tm_user';
const SESSION_TOKEN_KEY = 'tm_token';

const isTokenExpired = (token) => {
  if (!token) return true;
  try {
    const base64Url = token.split('.')[1];
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    const jsonPayload = decodeURIComponent(atob(base64).split('').map(function(c) {
        return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
    }).join(''));
    const { exp } = JSON.parse(jsonPayload);
    if (!exp) return false;
    return Date.now() >= exp * 1000;
  } catch (error) {
    return true;
  }
};

export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(null);
  const [token, setToken] = useState(null);
  const [loading, setLoading] = useState(true);

  const logout = useCallback(() => {
    setUser(null);
    setToken(null);
    setAuthToken(null);
    sessionStorage.removeItem(SESSION_USER_KEY);
    sessionStorage.removeItem(SESSION_TOKEN_KEY);
  }, []);

  useEffect(() => {
    const initAuth = () => {
      try {
        const storedToken = sessionStorage.getItem(SESSION_TOKEN_KEY);
        const storedUser = sessionStorage.getItem(SESSION_USER_KEY);

        if (storedToken && storedUser) {
          if (!isTokenExpired(storedToken)) {
            const parsedUser = JSON.parse(storedUser);
            setToken(storedToken);
            setUser(parsedUser);
            setAuthToken(storedToken);
          } else {
            sessionStorage.removeItem(SESSION_USER_KEY);
            sessionStorage.removeItem(SESSION_TOKEN_KEY);
          }
        }
      } catch (error) {
        sessionStorage.removeItem(SESSION_USER_KEY);
        sessionStorage.removeItem(SESSION_TOKEN_KEY);
      } finally {
        setLoading(false);
      }
    };

    initAuth();
  }, []);

  // Axios interceptor to catch 401s and automatically log out
  useEffect(() => {
    const interceptor = apiClient.interceptors.response.use(
      (response) => response,
      (error) => {
        if (error.response && error.response.status === 401) {
          logout();
        }
        return Promise.reject(error);
      }
    );

    return () => {
      apiClient.interceptors.response.eject(interceptor);
    };
  }, [logout]);

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
      sessionStorage.setItem(SESSION_TOKEN_KEY, authToken);
      sessionStorage.setItem(SESSION_USER_KEY, JSON.stringify(userData));

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

  const updateUser = (userData) => {
    setUser(prev => ({
      ...prev,
      ...userData
    }));
  };

  const value = {
    user,
    token,
    loading,
    login,
    logout,
    updateUser,
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
