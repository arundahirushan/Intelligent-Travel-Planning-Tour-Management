import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import Input from '../components/Input';
import Button from '../components/Button';
import ErrorBanner from '../components/ErrorBanner';
import { registerUser } from '../services/api';

const RegistrationForm = ({ role, onSuccess }) => {
  const [formData, setFormData] = useState({
    fullName: '',
    email: '',
    password: ''
  });
  const [errors, setErrors] = useState({});
  const [apiError, setApiError] = useState('');
  const [isLoading, setIsLoading] = useState(false);

  const validate = () => {
    const newErrors = {};
    if (!formData.fullName.trim()) newErrors.fullName = 'Full Name is required.';
    
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!formData.email.trim()) {
      newErrors.email = 'Email is required.';
    } else if (!emailRegex.test(formData.email)) {
      newErrors.email = 'Invalid email format.';
    }

    if (!formData.password) {
      newErrors.password = 'Password is required.';
    } else if (formData.password.length < 8) {
      newErrors.password = 'Password must be at least 8 characters.';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleChange = (e) => {
    setFormData({ ...formData, [e.target.name]: e.target.value });
    // Clear error when user types
    if (errors[e.target.name]) {
      setErrors({ ...errors, [e.target.name]: '' });
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setApiError('');
    
    if (!validate()) return;

    setIsLoading(true);
    try {
      await registerUser({
        FullName: formData.fullName,
        Email: formData.email,
        Password: formData.password,
        Role: role
      });
      onSuccess(role);
    } catch (error) {
      let message = 'Registration failed. Please try again.';
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
      setApiError(message);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="w-full">
      <ErrorBanner message={apiError} className="mb-6" />
      
      <Input
        label="Full Name"
        name="fullName"
        type="text"
        placeholder="e.g. Jane Doe"
        value={formData.fullName}
        onChange={handleChange}
        error={errors.fullName}
      />
      
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
        placeholder="Min. 8 characters"
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
        {isLoading ? 'Registering...' : `Register as ${role}`}
      </Button>
    </form>
  );
};

export default function RegisterPage() {
  const [activeRole, setActiveRole] = useState('Traveler');
  const [showBusinessRoles, setShowBusinessRoles] = useState(false);
  const [successMessage, setSuccessMessage] = useState('');

  const handleSuccess = (role) => {
    if (role === 'Traveler') {
      setSuccessMessage('Registration successful — you can log in now.');
    } else {
      setSuccessMessage('Registration successful — your account is awaiting admin approval.');
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
          Create an account
        </h2>
      </div>

      <div className="mt-8 sm:mx-auto sm:w-full sm:max-w-md">
        <div className="bg-white py-10 px-6 shadow-soft sm:rounded-xl sm:px-10 border border-border-neutral">
          
          {successMessage ? (
            <div className="text-center">
              <div className="mx-auto flex items-center justify-center h-12 w-12 rounded-full bg-green-100 mb-4">
                <span className="material-symbols-outlined text-green-600">check</span>
              </div>
              <h3 className="font-heading text-lg font-medium text-text mb-2">Success</h3>
              <p className="font-body text-sm text-text-secondary mb-6">{successMessage}</p>
              <Link to="/login">
                <Button variant="primary" className="w-full">Go to Login</Button>
              </Link>
            </div>
          ) : (
            <>
              {showBusinessRoles && (
                <div className="flex justify-between border-b border-border-neutral mb-8">
                  {['HotelOwner', 'TransportProvider', 'Supplier'].map(role => (
                    <button
                      key={role}
                      onClick={() => setActiveRole(role)}
                      className={`pb-3 font-heading text-xs font-bold uppercase tracking-wider transition-colors ${
                        activeRole === role 
                          ? 'text-primary border-b-2 border-primary' 
                          : 'text-text-secondary hover:text-text'
                      }`}
                    >
                      {role.replace(/([A-Z])/g, ' $1').trim()}
                    </button>
                  ))}
                </div>
              )}
              
              <RegistrationForm role={activeRole} onSuccess={handleSuccess} />

              <div className="mt-8 text-center border-t border-border-neutral pt-6">
                {!showBusinessRoles ? (
                  <button 
                    onClick={() => {
                      setShowBusinessRoles(true);
                      setActiveRole('HotelOwner');
                    }}
                    className="font-body text-sm text-primary hover:text-primary-dark font-medium transition-colors"
                  >
                    Not a traveler? Register as a business partner.
                  </button>
                ) : (
                  <button 
                    onClick={() => {
                      setShowBusinessRoles(false);
                      setActiveRole('Traveler');
                    }}
                    className="font-body text-sm text-primary hover:text-primary-dark font-medium transition-colors"
                  >
                    Register as a Traveler instead.
                  </button>
                )}
              </div>
              
              <div className="mt-6 text-center">
                <p className="font-body text-sm text-text-secondary">
                  Already have an account?{' '}
                  <Link to="/login" className="font-medium text-primary hover:text-primary-dark transition-colors">
                    Log in
                  </Link>
                </p>
              </div>
            </>
          )}

        </div>
      </div>
    </div>
  );
}
