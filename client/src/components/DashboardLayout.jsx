import React, { useState, useEffect, useRef } from 'react';
import { NavLink, useNavigate, useLocation, Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export default function DashboardLayout({ navItems, roleBadge, profileRoute = '/profile', children }) {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);
  const [isProfileDropdownOpen, setIsProfileDropdownOpen] = useState(false);
  const [isSupportOpen, setIsSupportOpen] = useState(false);
  
  const profileDropdownRef = useRef(null);

  // Close menus on navigate
  useEffect(() => {
    setIsMobileMenuOpen(false);
    setIsProfileDropdownOpen(false);
  }, [location.pathname]);

  // Click outside profile dropdown to close
  useEffect(() => {
    const handleClickOutside = (event) => {
      if (profileDropdownRef.current && !profileDropdownRef.current.contains(event.target)) {
        setIsProfileDropdownOpen(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const getInitials = (name) => {
    if (!name) return '?';
    return name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2);
  };

  // profileRoute is passed as a prop by each dashboard — no role-switching here.

  return (
    <div className="min-h-screen bg-canvas flex flex-col relative">
      {/* Top Navbar */}
      <header className="sticky top-0 z-30 h-[72px] bg-white border-b border-border-neutral flex items-center justify-between px-4 lg:px-[var(--space-margin)] shrink-0">
        
        {/* Left: Logo & Mobile Toggle */}
        <div className="flex items-center gap-4">
          <Link to="/" className="flex items-center gap-3">
            <div className="w-10 h-10 bg-primary rounded-xl flex items-center justify-center text-white">
              <span className="material-symbols-outlined font-bold">flight_takeoff</span>
            </div>
            <span className="hidden sm:block font-heading text-xl font-bold text-text">EasyPlanner</span>
          </Link>
          
          {roleBadge && (
            <div className="hidden md:inline-flex px-3 py-1 bg-surface-blue text-primary font-heading font-bold text-label-badge rounded-pill ml-2 whitespace-nowrap">
              {roleBadge}
            </div>
          )}
        </div>

        {/* Center: Desktop Navigation */}
        <nav className="hidden md:flex flex-1 mx-8">
          <ul className="flex items-center gap-2 flex-wrap">
            {navItems.map((item) => (
              <li key={item.path}>
                <NavLink
                  to={item.path}
                  className={({ isActive }) => `
                    flex items-center gap-2 px-4 py-2 rounded-md font-heading text-label-button transition-colors
                    ${isActive 
                      ? 'bg-primary text-white' 
                      : 'text-text-secondary hover:bg-surface-neutral hover:text-text'
                    }
                  `}
                >
                  <span className="material-symbols-outlined text-xl">{item.icon}</span>
                  {item.label}
                </NavLink>
              </li>
            ))}
          </ul>
        </nav>

        {/* Right: Profile & Mobile Toggle */}
        <div className="flex items-center gap-4">
          
          <button 
            onClick={() => setIsMobileMenuOpen(!isMobileMenuOpen)}
            className="md:hidden text-text-secondary hover:text-text"
          >
            <span className="material-symbols-outlined">menu</span>
          </button>

          <div className="relative" ref={profileDropdownRef}>
            <button 
              onClick={() => setIsProfileDropdownOpen(!isProfileDropdownOpen)}
              className="flex items-center gap-3 pl-4 border-l border-border-neutral focus:outline-none"
            >
              <div className="hidden sm:flex flex-col items-end">
                <span className="font-heading font-semibold text-text text-sm">
                  {user?.fullName || 'User'}
                </span>
                <span className="text-xs text-text-secondary font-body">
                  {user?.role || 'User'}
                </span>
              </div>
              <div className="w-10 h-10 rounded-full bg-primary flex items-center justify-center text-white font-heading font-bold hover:opacity-90 transition-opacity">
                {getInitials(user?.fullName)}
              </div>
            </button>

            {/* Profile Dropdown */}
            {isProfileDropdownOpen && (
              <div className="absolute right-0 mt-2 w-48 bg-white rounded-lg shadow-soft border border-border-neutral overflow-hidden z-50">
                <ul className="py-1">
                  <li>
                    <Link 
                      to={profileRoute}
                      className="flex items-center gap-2 px-4 py-2 text-body-sm text-text hover:bg-surface-neutral transition-colors"
                    >
                      <span className="material-symbols-outlined text-sm">person</span>
                      Profile
                    </Link>
                  </li>
                  <li>
                    <button 
                      onClick={handleLogout}
                      className="w-full flex items-center gap-2 px-4 py-2 text-body-sm text-status-danger hover:bg-red-50 transition-colors text-left"
                    >
                      <span className="material-symbols-outlined text-sm">logout</span>
                      Sign Out
                    </button>
                  </li>
                </ul>
              </div>
            )}
          </div>
        </div>
      </header>

      {/* Mobile Menu */}
      {isMobileMenuOpen && (
        <div className="md:hidden bg-white border-b border-border-neutral p-4 z-20">
          <ul className="flex flex-col gap-2">
            {navItems.map((item) => (
              <li key={item.path}>
                <NavLink
                  to={item.path}
                  className={({ isActive }) => `
                    flex items-center gap-3 px-4 py-3 rounded-md font-heading text-label-button transition-colors
                    ${isActive 
                      ? 'bg-primary text-white' 
                      : 'text-text-secondary hover:bg-surface-neutral hover:text-text'
                    }
                  `}
                >
                  <span className="material-symbols-outlined text-xl">{item.icon}</span>
                  {item.label}
                </NavLink>
              </li>
            ))}
          </ul>
        </div>
      )}

      {/* Main Content */}
      <main className="flex-1 overflow-x-hidden overflow-y-auto bg-canvas p-4 lg:p-[var(--space-margin)] relative">
        <div className="max-w-[1400px] mx-auto w-full">
          {children}
        </div>
      </main>

      {/* Floating Support Widget */}
      <div className="fixed bottom-6 right-6 z-40 flex flex-col items-end gap-2">
        {isSupportOpen && (
          <div className="bg-white rounded-lg shadow-soft border border-border-neutral p-4 w-64 animate-in fade-in slide-in-from-bottom-4 text-body-sm text-text-secondary">
            <h4 className="font-heading font-bold text-text text-sm mb-2">Need Help?</h4>
            <p className="mb-2">Our support team is here for you.</p>
            <p className="font-semibold text-text">support@easyplanner.com</p>
            <p className="font-semibold text-text">+1 (555) 123-4567</p>
          </div>
        )}
        <button 
          onClick={() => setIsSupportOpen(!isSupportOpen)}
          className={`w-12 h-12 rounded-full flex items-center justify-center text-white shadow-soft transition-colors ${isSupportOpen ? 'bg-text-secondary' : 'bg-primary hover:bg-primary-dark'}`}
          title="Help & Support"
        >
          <span className="material-symbols-outlined">
            {isSupportOpen ? 'close' : 'support_agent'}
          </span>
        </button>
      </div>
    </div>
  );
}
