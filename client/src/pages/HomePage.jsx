import React from 'react';
import { Link, useNavigate } from 'react-router-dom';
import ImageCard from '../components/ImageCard';
import FeatureCard from '../components/FeatureCard';
import { useAuth } from '../context/AuthContext';

export default function HomePage() {
  const { isAuthenticated, user } = useAuth();
  const navigate = useNavigate();

  const getDashboardPath = () => {
    if (!user) return '/login';
    switch (user.role) {
      case 'HotelOwner': return '/hotel-owner';
      case 'Traveler': return '/traveler';
      case 'TransportProvider': return '/transport-provider';
      case 'Supplier': return '/supplier';
      case 'SuperAdmin': return '/admin';
      case 'Admin': return '/admin';
      default: return '/';
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
            <a href="#destinations" className="font-heading text-sm font-semibold tracking-wide text-text-secondary hover:text-primary transition-colors">Destinations</a>
            <a href="#how-it-works" className="font-heading text-sm font-semibold tracking-wide text-text-secondary hover:text-primary transition-colors">How It Works</a>
          </nav>

          <div className="flex items-center gap-4">
            {isAuthenticated ? (
              <>
                <Link to={getDashboardPath()} className="inline-flex items-center justify-center px-6 py-2 bg-primary text-white font-heading text-xs font-bold uppercase tracking-widest rounded-pill hover:bg-primary-dark transition-all shadow-sm">
                  Dashboard
                </Link>
                <div className="hidden md:flex items-center gap-3 text-right ml-2">
                  <div className="flex flex-col justify-center">
                    <span className="font-heading font-bold text-sm text-text leading-tight">{user?.fullName || 'User'}</span>
                    <span className="font-body text-xs text-text-secondary">{user?.role || ''}</span>
                  </div>
                  <div className="w-10 h-10 rounded-full bg-primary text-white flex items-center justify-center font-heading font-bold text-lg">
                    {user?.fullName ? user.fullName.split(' ').map(n => n[0]).join('').substring(0, 2).toUpperCase() : 'U'}
                  </div>
                </div>
              </>
            ) : (
              <div className="flex flex-col items-center justify-center">
                <Link to="/login" className="inline-flex items-center justify-center px-6 py-2 bg-primary text-white font-heading text-xs font-bold uppercase tracking-widest rounded-pill hover:bg-primary-dark transition-all shadow-sm">
                  Log in
                </Link>
                <Link to="/register" className="text-[11px] text-text-secondary hover:text-text underline mt-1 tracking-wide uppercase font-bold">
                  Register
                </Link>
              </div>
            )}
          </div>
        </div>
      </header>

      <main className="w-full">
        {/* Hero Section */}
        <section className="relative w-full min-h-[92vh] flex items-center justify-center overflow-hidden bg-hero text-white pt-20">
          <div className="absolute inset-0 z-0">
            {/* Hero image from public folder */}
            <img
              src="/Hero.jpg"
              alt="Hero Landscape"
              className="w-full h-full object-cover object-center transform scale-100"
            />
            <div className="absolute inset-0 bg-gradient-to-t from-hero/80 via-hero/30 to-transparent"></div>
          </div>

          <div className="relative z-10 max-w-4xl mx-auto px-6 py-20 text-center flex flex-col items-center">
            <div className="inline-flex items-center gap-2 px-3 py-1 bg-white/10 backdrop-blur-md rounded-pill text-accent font-heading text-xs font-bold uppercase tracking-widest mb-6">
              <span className="w-2 h-2 rounded-full bg-primary"></span>
              Plan Your Next Adventure
            </div>
            <h1 className="font-heading text-4xl sm:text-6xl lg:text-7xl font-bold tracking-tight uppercase leading-[1.1] mb-6 text-white">
              Discover Sri Lanka.<br />Your Way.
            </h1>
            <p className="font-body text-base sm:text-xl text-white/85 max-w-2xl font-light leading-relaxed mb-10">
              Build your custom itinerary with curated routes, trusted local partners, and intelligent recommendations for the perfect island journey.
            </p>
            <Link to={isAuthenticated ? getDashboardPath() : "/register"} className="inline-flex items-center justify-center px-8 py-3.5 bg-primary text-white font-heading text-xs font-bold uppercase tracking-widest rounded-pill hover:bg-primary-dark transition-all duration-200 shadow-lg group">
              <span>{isAuthenticated ? 'Go to Dashboard' : 'Start Planning'}</span>
              <span className="material-symbols-outlined text-[18px] ml-2 group-hover:translate-x-1 transition-transform">arrow_forward</span>
            </Link>
          </div>
        </section>

        {/* Popular Destinations */}
        <section className="w-full py-24 px-6 lg:px-12 max-w-7xl mx-auto" id="destinations">
          <div className="mb-14 text-center max-w-2xl mx-auto">
            <span className="font-heading text-xs font-bold uppercase tracking-widest text-primary block mb-2">Curated Highlights</span>
            <h2 className="font-heading text-3xl sm:text-4xl font-bold tracking-tight text-text uppercase">Explore Destinations</h2>
            <p className="font-body text-base text-text-secondary mt-3">Essential places to begin your journey across the island.</p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8">
            <ImageCard
              title="Sigiriya"
              category="CULTURAL TRIANGLE"
              description="5th-century ancient rock citadel & water gardens."
              imageUrl="/sigiriya.png"
              onClick={() => navigate('/destinations/sigiriya')}
            />
            <ImageCard
              title="Ella - Nine Arches Bridge"
              category="CENTRAL HIGHLANDS"
              description="Discover the iconic Nine Arches Bridge hidden in the misty emerald highlands of Ella."
              imageUrl="/ella.png"
              onClick={() => navigate('/destinations/nine-arches-bridge')}
            />
            <ImageCard
              title="Galle Fort"
              category="SOUTHERN COAST"
              description="Wander through the historic Galle Fort and its iconic lighthouse by the southern coastal swells."
              imageUrl="/galle_fort.png"
              onClick={() => navigate('/destinations/galle-fort')}
            />
            <ImageCard
              title="Mirissa"
              category="SOUTHERN COAST"
              description="Relax on sun-drenched tropical beaches and enjoy vibrant southern coast sunsets."
              imageUrl="/4.jpg"
              onClick={() => navigate('/destinations/mirissa')}
            />
            <ImageCard
              title="Nuwara Eliya"
              category="CENTRAL HIGHLANDS"
              description="Experience the cool green highlands and sprawling tea plantations of Little England."
              imageUrl="/5.jpg"
              onClick={() => navigate('/destinations/nuwara-eliya')}
            />
            <ImageCard
              title="Matara - Paravi Duwa Temple"
              category="SOUTHERN COAST"
              description="Visit the serene island temple accessed by a beautiful pedestrian bridge over the ocean."
              imageUrl="/6.jpg"
              onClick={() => navigate('/destinations/paravi-duwa-temple')}
            />
          </div>
        </section>

        {/* Premium Accommodation Section */}
        <section className="w-full py-24 px-6 lg:px-12 max-w-7xl mx-auto">
          <div className="flex flex-col lg:flex-row items-stretch gap-12 lg:gap-16 lg:h-[550px]">
            {/* Left Side - Image */}
            <div className="w-full lg:w-1/2 rounded-[32px] overflow-hidden shadow-soft flex">
              <img
                src="/luxury_hotel.png"
                alt="Luxury Sri Lankan Hotel"
                className="w-full h-full object-cover object-center transition-transform duration-700 hover:scale-105"
              />
            </div>
            
            {/* Right Side - Content */}
            <div className="w-full lg:w-1/2 flex flex-col justify-center">
              <div className="inline-flex items-center gap-2 text-primary font-heading text-xs font-bold uppercase tracking-widest mb-4">
                <span className="w-2 h-2 rounded-full bg-primary"></span>
                Your Home Away From Home
              </div>
              
              <h2 className="font-heading text-4xl sm:text-5xl font-bold tracking-tight text-text leading-[1.1] mb-6">
                Stay Somewhere<br />Extraordinary.
              </h2>
              
              <p className="font-body text-lg text-text-secondary leading-relaxed mb-4 max-w-lg">
                Discover beautiful stays across Sri Lanka, from coastal hideaways to peaceful mountain retreats.
              </p>
              
              <p className="font-body text-lg text-text-secondary leading-relaxed mb-8 max-w-lg">
                Find your perfect place to unwind and make every journey unforgettable.
              </p>
              
              <p className="font-heading text-sm font-semibold tracking-wide text-text-secondary uppercase mb-8">
                Thoughtful stays. Memorable journeys.
              </p>
              
              <div>
                <Link to={isAuthenticated ? getDashboardPath() : "/login"} className="inline-flex items-center justify-center px-8 py-3.5 bg-primary text-white font-heading text-xs font-bold uppercase tracking-widest rounded-pill hover:bg-primary-dark transition-all shadow-md group">
                  <span>Book a Hotel</span>
                  <span className="material-symbols-outlined text-[18px] ml-2 group-hover:translate-x-1 transition-transform">arrow_forward</span>
                </Link>
              </div>
            </div>
          </div>
        </section>

        {/* Features Row */}
        <section className="w-full py-24 bg-white border-y border-border-blue/60" id="how-it-works">
          <div className="max-w-7xl mx-auto px-6 lg:px-12">
            <div className="mb-14 text-center max-w-2xl mx-auto">
              <span className="font-heading text-xs font-bold uppercase tracking-widest text-primary block mb-2">Features</span>
              <h2 className="font-heading text-3xl sm:text-4xl font-bold tracking-tight text-text uppercase">How It Works</h2>
              <p className="font-body text-base text-text-secondary mt-3">Plan your trip seamlessly using our intelligent tools.</p>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
              <FeatureCard
                icon="map"
                title="Smart Itineraries"
                description="Customized daily plans based on your interests."
              />
              <FeatureCard
                icon="hotel"
                title="Trusted Stays"
                description="Verified hotel owners offering premium comfort."
              />
              <FeatureCard
                icon="directions_car"
                title="Easy Transport"
                description="Book reliable local transport providers directly."
              />
              <FeatureCard
                icon="hiking"
                title="Curated Activities"
                description="Unique experiences guided by local suppliers."
              />
            </div>
          </div>
        </section>

        {/* Split Section */}
        <section className="w-full py-24 px-6 lg:px-12 max-w-7xl mx-auto">
          <div className="bg-surface-light text-text border border-border-blue rounded-[32px] overflow-hidden shadow-soft grid grid-cols-1 lg:grid-cols-12 items-center">
            <div className="lg:col-span-7 h-72 sm:h-96 lg:h-[500px] relative overflow-hidden">
              <img
                src="/Nine arch Bridge.jpg"
                alt="Nine Arch Bridge"
                className="w-full h-full object-cover object-center transform hover:scale-105 transition-transform duration-700"
              />
            </div>

            <div className="lg:col-span-5 p-8 sm:p-12 lg:p-14 flex flex-col items-start">
              <div className="inline-flex items-center gap-2 text-primary font-heading text-xs font-bold uppercase tracking-widest mb-4">
                <span className="w-2 h-2 rounded-full bg-primary"></span>
                AI-Powered Planning
              </div>
              <h3 className="font-heading text-3xl sm:text-4xl font-bold uppercase tracking-tight mb-4 text-text leading-tight">
                Effortless Journey Mapping
              </h3>
              <p className="font-body text-base text-text-secondary leading-relaxed mb-8">
                Tell us what you love—beaches, heritage, nature, or adventure—and let our intelligent engine craft a personalized route across the island just for you.
              </p>
              <Link to={isAuthenticated ? getDashboardPath() : "/register"} className="inline-flex items-center gap-2 px-7 py-3 bg-primary text-white font-heading text-xs font-bold uppercase tracking-widest rounded-pill hover:bg-primary-dark transition-all shadow-sm">
                <span>{isAuthenticated ? 'Go to Dashboard' : 'Start Planning'}</span>
                <span className="material-symbols-outlined text-[16px]">arrow_forward</span>
              </Link>
            </div>
          </div>
        </section>

        {/* CTA Section */}
        <section className="w-full py-20 px-6 lg:px-12">
          <div className="max-w-5xl mx-auto bg-surface-blue text-text border border-border-blue rounded-[32px] p-10 sm:p-16 text-center shadow-soft relative overflow-hidden">
            <div className="max-w-2xl mx-auto relative z-10">
              <h2 className="font-heading text-3xl sm:text-4xl font-bold uppercase tracking-tight mb-4 text-text">
                Ready to Begin Your Journey?
              </h2>
              <p className="font-body text-base sm:text-lg text-text-secondary leading-relaxed mb-8">
                Craft your custom Sri Lankan itinerary with curated routes, private transfers, and local insight.
              </p>
              <Link to={isAuthenticated ? getDashboardPath() : "/register"} className="inline-flex items-center justify-center px-8 py-3.5 bg-primary text-white font-heading text-xs font-bold uppercase tracking-widest rounded-pill hover:bg-primary-dark transition-all shadow-md">
                {isAuthenticated ? 'Go to Dashboard' : 'Start Planning'}
              </Link>
            </div>
          </div>
        </section>
      </main>

      {/* Footer */}
      <footer className="w-full bg-surface-neutral text-text pt-16 pb-12 border-t border-border-neutral/70">
        <div className="max-w-7xl mx-auto px-6 lg:px-12">
          <div className="grid grid-cols-1 md:grid-cols-12 gap-10 pb-12 border-b border-border-neutral items-start">

            <div className="md:col-span-6 flex flex-col">
              <Link to="/" className="mb-4 inline-flex items-center gap-3">
                <img src="/logo.png" alt="Easy Planner Logo" className="h-8 w-auto object-contain" />
                <span className="font-heading text-xl font-bold tracking-tight text-text">Easy Planner</span>
              </Link>
              <p className="font-body text-sm text-text-secondary leading-relaxed max-w-sm">
                The premier travel planning platform for discovering the living heritage, landscapes, and horizons of Sri Lanka.
              </p>
            </div>

            <div className="md:col-span-6 flex flex-wrap gap-x-8 gap-y-3 font-heading text-xs font-semibold uppercase tracking-wider text-text md:justify-end">
              <a href="#destinations" className="hover:text-primary transition-colors">Destinations</a>
              <a href="#how-it-works" className="hover:text-primary transition-colors">How It Works</a>
              {isAuthenticated ? (
                <Link to={getDashboardPath()} className="hover:text-primary transition-colors">Dashboard</Link>
              ) : (
                <>
                  <Link to="/login" className="hover:text-primary transition-colors">Log In</Link>
                  <Link to="/register" className="hover:text-primary transition-colors">Register</Link>
                </>
              )}
            </div>

          </div>

          <div className="pt-8 flex flex-col sm:flex-row items-center justify-between gap-4 font-body text-xs text-text-secondary">
            <p>© 2024 Easy Planner. All rights reserved.</p>
            <div className="flex items-center gap-6">
              <a href="#" className="hover:text-primary transition-colors">Privacy</a>
              <a href="#" className="hover:text-primary transition-colors">Terms</a>
            </div>
          </div>
        </div>
      </footer>
    </div>
  );
}
