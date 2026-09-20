import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import Input from '../components/Input';
import Button from '../components/Button';
import ErrorBanner from '../components/ErrorBanner';
import { useAuth } from '../context/AuthContext';

export default function LoginPage() {
  const [formData, setFormData] = useState({ email: '', password: '' });
  const [errors, setErrors] = useState({});
  const [apiError, setApiError] = useState('');
  const [adminMessage, setAdminMessage] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  
  const { login, logout } = useAuth();
  const navigate = useNavigate();

  const validate = () => {
    const newErrors = {};
    if (!formData.email.trim()) newErrors.email = 'Email is required.';
    if (!formData.password) newErrors.password = 'Password is required.';
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleChange = (e) => {
    setFormData({ ...formData, [e.target.name]: e.target.value });
    if (errors[e.target.name]) {
      setErrors({ ...errors, [e.target.name]: '' });
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setApiError('');
    setAdminMessage('');
    
    if (!validate()) return;

    setIsLoading(true);
    const result = await login(formData.email, formData.password);
    setIsLoading(false);

    if (result.success) {
      const role = result.role;
      if (role === 'Admin' || role === 'SuperAdmin') {
        logout(); // Clear the session
        setAdminMessage('Admin accounts use the Tour Management mobile app — please log in there instead.');
      } else if (role === 'Traveler') {
        navigate('/traveler');
      } else if (role === 'HotelOwner') {
        navigate('/hotel-owner');
      } else if (role === 'TransportProvider') {
        navigate('/transport-provider');
      } else if (role === 'Supplier') {
        navigate('/supplier');
      } else {
        // Fallback for unknown roles
        navigate('/');
      }
    } else {
      setApiError(result.error || 'Invalid credentials.');
    }
  };

  return (
    <div className="min-h-screen bg-canvas flex flex-col justify-center py-12 sm:px-6 lg:px-8">
      <div className="sm:mx-auto sm:w-full sm:max-w-md">
        <div className="text-center mb-8">
          <Link to="/" className="inline-flex flex-col items-center gap-2">
             <img src="/logo.png" alt="Easy Planner Logo" className="h-12 w-auto object-contain mx-auto" />
             <span className="font-heading text-xl font-bold tracking-tight text-text">Easy Planner</span>
          </Link>
        </div>
        <h2 className="mt-2 text-center font-heading text-3xl font-extrabold uppercase text-text">
          Log in to your account
        </h2>
      </div>

      <div className="mt-8 sm:mx-auto sm:w-full sm:max-w-md">
        <div className="bg-white py-10 px-6 shadow-soft sm:rounded-xl sm:px-10 border border-border-neutral">
          
          <ErrorBanner message={apiError} className="mb-6" />
          
          {adminMessage ? (
            <div className="text-center bg-blue-50 border border-blue-200 text-blue-800 p-6 rounded-lg mb-6">
              <span className="material-symbols-outlined text-4xl text-blue-500 mb-2">phone_iphone</span>
              <p className="font-body text-sm font-medium">{adminMessage}</p>
              <Button onClick={() => setAdminMessage('')} variant="secondary" className="w-full mt-4">
                Try a different account
              </Button>
            </div>
          ) : (
            <form onSubmit={handleSubmit} className="w-full">
              <Input
                label="Email Address"
                name="email"
                type="email"
                placeholder="you@example.com"
                value={formData.email}
                onChange={handleChange}
                error={errors.email}
              />
              
              <Input
                label="Password"
                name="password"
                type="password"
                placeholder="••••••••"
                value={formData.password}
                onChange={handleChange}
                error={errors.password}
              />
              
              <Button 
                type="submit" 
                variant="primary" 
                className="w-full mt-2" 
                disabled={isLoading}
              >
                {isLoading ? 'Logging in...' : 'Log In'}
              </Button>
            </form>
          )}

          <div className="mt-8 text-center border-t border-border-neutral pt-6">
            <p className="font-body text-sm text-text-secondary">
              Don't have an account?{' '}
              <Link to="/register" className="font-medium text-primary hover:text-primary-dark transition-colors">
                Register here
              </Link>
            </p>
          </div>

        </div>
      </div>
    </div>
  );
}
