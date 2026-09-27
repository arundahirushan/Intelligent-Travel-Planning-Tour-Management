import React, { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { getAcceptedHotels } from '../services/travelerApi';
import LoadingSpinner from '../components/LoadingSpinner';
import ErrorBanner from '../components/ErrorBanner';

export default function HotelsPage() {
  const [hotels, setHotels] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const navigate = useNavigate();

  useEffect(() => {
    fetchHotels();
  }, []);

  const fetchHotels = async () => {
    try {
      setLoading(true);
      setError(null);
      const res = await getAcceptedHotels();
      setHotels(res.items || []);
    } catch (err) {
      setError(err.message || 'Failed to load hotels');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-canvas">
      {/* Navbar */}
      <header className="fixed top-0 left-0 right-0 z-50 bg-canvas/90 backdrop-blur-md border-b border-border-blue/70 transition-all">
        <div className="max-w-7xl mx-auto h-20 px-6 lg:px-12 flex items-center justify-between">
          <Link to="/" className="flex items-center gap-3 group">
            <img src="/logo.png" alt="Easy Planner Logo" className="h-25 w-auto object-contain" />
            <span className="font-heading text-xl font-bold tracking-tight text-text hidden sm:inline-block">Easy Planner</span>
          </Link>

          <nav className="hidden md:flex items-center gap-9">
            <Link to="/#destinations" className="font-heading text-sm font-semibold tracking-wide text-text-secondary hover:text-primary transition-colors">Destinations</Link>
            <Link to="/#accommodation" className="font-heading text-sm font-semibold tracking-wide text-primary transition-colors">Accommodation</Link>
            <Link to="/#vehicles" className="font-heading text-sm font-semibold tracking-wide text-text-secondary hover:text-primary transition-colors">Vehicles</Link>
            <Link to="/#how-it-works" className="font-heading text-sm font-semibold tracking-wide text-text-secondary hover:text-primary transition-colors">How It Works</Link>
          </nav>

          <div className="flex items-center gap-4">
            <Link to="/" className="inline-flex items-center justify-center px-6 py-2 bg-white border border-border-blue/50 text-text font-heading text-xs font-bold uppercase tracking-widest rounded-pill hover:bg-surface-blue transition-all shadow-sm">
              Back to Home
            </Link>
          </div>
        </div>
      </header>

      <main className="w-full pt-32 pb-24 px-6 lg:px-12 max-w-7xl mx-auto">
        <div className="mb-14 text-center max-w-2xl mx-auto">
          <span className="font-heading text-xs font-bold uppercase tracking-widest text-primary block mb-2">Curated Stays</span>
          <h1 className="font-heading text-4xl sm:text-5xl font-bold tracking-tight text-text uppercase leading-tight mb-4">
            Stay Somewhere<br />Extraordinary.
          </h1>
          <p className="font-body text-base text-text-secondary">Browse all verified partner hotels available for your journey.</p>
        </div>

        {error && <div className="mb-8"><ErrorBanner message={error} /></div>}

        {loading ? (
          <div className="py-20 flex justify-center"><LoadingSpinner /></div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8">
            {hotels.length > 0 ? (
              hotels.map(hotel => (
                <div key={hotel.id} className="group bg-white rounded-3xl overflow-hidden shadow-soft border border-border-blue/50 flex flex-col hover:shadow-lg transition-all duration-300">
                  <div className="h-48 bg-surface-blue relative overflow-hidden">
                    {hotel.imageUrl ? (
                      <img src={hotel.imageUrl} alt={hotel.name} className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-700" />
                    ) : (
                      <div className="w-full h-full flex items-center justify-center">
                        <span className="material-symbols-outlined text-4xl text-blue-200">apartment</span>
                      </div>
                    )}
                    <div className="absolute top-4 right-4 bg-white/90 backdrop-blur-sm px-3 py-1 rounded-full border border-white/20 shadow-sm flex items-center gap-1">
                      <span className="material-symbols-outlined text-[14px] text-yellow-500">star</span>
                      <span className="font-body text-xs font-bold text-text">{hotel.starRating || 5} Star</span>
                    </div>
                  </div>
                  <div className="p-6 flex flex-col flex-grow">
                    <h3 className="font-heading text-xl font-bold text-text mb-2 line-clamp-1">{hotel.name}</h3>
                    <div className="flex items-center gap-1.5 text-text-secondary mb-4">
                      <span className="material-symbols-outlined text-[16px]">location_on</span>
                      <span className="font-body text-sm line-clamp-1">{hotel.destinationName || 'Sri Lanka'}</span>
                    </div>
                    <p className="font-body text-sm text-text-secondary line-clamp-2 mb-6 flex-grow">{hotel.description || 'Experience a wonderful stay with excellent amenities and service.'}</p>
                    
                    <button
                      onClick={() => navigate('/login')}
                      className="w-full inline-flex items-center justify-center px-4 py-2.5 bg-surface-blue text-primary font-heading text-xs font-bold uppercase tracking-widest rounded-pill hover:bg-primary hover:text-white transition-all group/btn"
                    >
                      <span>Book Now</span>
                      <span className="material-symbols-outlined text-[16px] ml-2 group-hover/btn:translate-x-1 transition-transform">arrow_forward</span>
                    </button>
                  </div>
                </div>
              ))
            ) : (
              <div className="col-span-full py-12 text-center text-text-secondary font-body bg-white rounded-2xl border border-border-blue/50">
                No hotels are currently available. Check back soon!
              </div>
            )}
          </div>
        )}
      </main>
    </div>
  );
}
