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
                src="/new_pool.png"
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

        {/* Vehicle Booking Section */}
        <section className="w-full py-24 bg-white relative overflow-hidden border-t border-border-blue/60">
          {/* Subtle background swoosh effect */}
          <div className="absolute bottom-0 left-0 w-3/4 h-1/2 bg-surface-blue/50 rounded-tr-[100%] -z-0 opacity-60"></div>

          <div className="max-w-7xl mx-auto px-6 lg:px-12 relative z-10">
            <div className="flex flex-col lg:flex-row items-center gap-12 lg:gap-8">

              {/* Left Side - Content */}
              <div className="w-full lg:w-[50%] lg:pr-8 py-8">
                <div className="inline-flex items-center gap-2 text-primary font-heading text-[10px] sm:text-xs font-bold uppercase tracking-widest mb-6">
                  <span className="w-2 h-2 rounded-full bg-primary"></span>
                  EXPLORE SRI LANKA YOUR WAY
                </div>

                <h2 className="font-heading text-4xl sm:text-5xl lg:text-[3.2rem] font-bold tracking-tight text-text leading-[1.15] mb-6">
                  Every Journey<br />Begins With the Right Ride<span className="text-primary">.</span>
                </h2>

                <p className="font-body text-lg text-text-secondary leading-relaxed mb-12 max-w-lg">
                  Discover comfortable transport for every adventure, from beautiful coastal escapes to unforgettable mountain journeys.
                </p>

                <div className="flex flex-col sm:flex-row flex-wrap lg:flex-nowrap items-start gap-6 sm:gap-10 mb-12 border-l-2 border-surface-blue pl-6 sm:pl-0 sm:border-l-0">
                  <div className="flex items-start gap-4">
                    <div className="w-12 h-12 rounded-full bg-surface-blue flex items-center justify-center shrink-0">
                      <span className="material-symbols-outlined text-primary text-[22px]">directions_car</span>
                    </div>
                    <div>
                      <h4 className="font-heading font-bold text-text text-[15px]">Cars</h4>
                      <p className="font-body text-xs text-text-secondary mt-0.5">For couples & families</p>
                    </div>
                  </div>

                  <div className="flex items-start gap-4">
                    <div className="w-12 h-12 rounded-full bg-surface-blue flex items-center justify-center shrink-0">
                      <span className="material-symbols-outlined text-primary text-[22px]">airport_shuttle</span>
                    </div>
                    <div>
                      <h4 className="font-heading font-bold text-text text-[15px]">Vans</h4>
                      <p className="font-body text-xs text-text-secondary mt-0.5">For groups</p>
                    </div>
                  </div>

                  <div className="flex items-start gap-4">
                    <div className="w-12 h-12 rounded-full bg-surface-blue flex items-center justify-center shrink-0">
                      <span className="material-symbols-outlined text-primary text-[22px]">map</span>
                    </div>
                    <div>
                      <h4 className="font-heading font-bold text-text text-[15px]">Your Journey</h4>
                      <p className="font-body text-xs text-text-secondary mt-0.5">Your island. Your pace.</p>
                    </div>
                  </div>
                </div>

                <div>
                  <Link to={isAuthenticated ? getDashboardPath() : "/login"} className="inline-flex items-center justify-center px-8 py-3.5 bg-primary text-white font-heading text-sm font-bold uppercase tracking-widest rounded-pill hover:bg-primary-dark transition-all shadow-md group">
                    <span>Book a Vehicle</span>
                    <span className="material-symbols-outlined text-[18px] ml-2 group-hover:translate-x-1 transition-transform">arrow_forward</span>
                  </Link>
                </div>
              </div>

              {/* Right Side - Image with curved mask */}
              <div className="w-full lg:w-[50%] relative h-[400px] lg:h-[600px]">
                <div className="w-full h-full lg:absolute lg:top-[-40px] lg:bottom-[-40px] lg:right-[-60px] lg:w-[120%] rounded-2xl lg:rounded-l-[80px] lg:rounded-r-none overflow-hidden shadow-2xl">
                  <img
                    src="/vehicle_promo.png"
                    alt="Vehicle Booking Sri Lanka"
                    className="w-full h-full object-cover object-center transition-transform duration-700 hover:scale-105"
                  />
                </div>
              </div>

            </div>
          </div>
        </section>

        {/* Features Row - How It Works */}
        <section className="w-full pt-20 pb-40 bg-white relative overflow-hidden" id="how-it-works">
          
          {/* Custom Decorative Landscape Background (Vector illustration style) */}
          <div className="absolute bottom-0 left-0 w-full h-[400px] pointer-events-none z-0 overflow-hidden bg-gradient-to-b from-transparent via-[#F0F9FF]/80 to-[#E0F2FE]">
            
            {/* Distant soft mountains */}
            <svg viewBox="0 0 1440 250" className="absolute bottom-12 w-full h-[250px]" preserveAspectRatio="none">
              <path fill="#BAE6FD" opacity="0.4" d="M0,150 C200,100 350,180 500,140 C650,100 850,150 1000,120 C1150,90 1300,130 1440,140 L1440,250 L0,250 Z" />
            </svg>
            
            {/* Mid-range mountains */}
            <svg viewBox="0 0 1440 200" className="absolute bottom-4 w-full h-[200px]" preserveAspectRatio="none">
              <path fill="#7DD3FC" opacity="0.3" d="M0,120 C250,80 400,140 650,100 C900,60 1100,130 1440,90 L1440,200 L0,200 Z" />
            </svg>

            {/* Left Coconut Trees */}
            <svg className="absolute bottom-4 left-[3%] w-[180px] h-[180px] text-[#6b9cb3] opacity-80" viewBox="0 0 100 100" fill="currentColor">
              {/* Tall Palm */}
              <g transform="translate(10, 5) scale(0.9) rotate(-3, 50, 50)">
                <path d="M45 100 Q55 50 48 30 L52 30 Q60 50 55 100 Z" />
                <path d="M50 30 Q90 10 95 45 Q75 35 50 35 Z" />
                <path d="M50 30 Q80 -5 50 -10 Q60 15 48 30 Z" />
                <path d="M50 30 Q10 10 5 45 Q25 35 50 35 Z" />
                <path d="M50 30 Q20 -5 50 -10 Q40 15 52 30 Z" />
                <path d="M50 35 Q85 55 65 75 Q70 55 50 40 Z" />
                <path d="M50 35 Q15 55 35 75 Q30 55 50 40 Z" />
              </g>
              {/* Short Palm */}
              <g transform="translate(35, 35) scale(0.65) rotate(8, 50, 50)">
                <path d="M45 100 Q55 50 48 30 L52 30 Q60 50 55 100 Z" />
                <path d="M50 30 Q90 10 95 45 Q75 35 50 35 Z" />
                <path d="M50 30 Q80 -5 50 -10 Q60 15 48 30 Z" />
                <path d="M50 30 Q10 10 5 45 Q25 35 50 35 Z" />
                <path d="M50 30 Q20 -5 50 -10 Q40 15 52 30 Z" />
                <path d="M50 35 Q85 55 65 75 Q70 55 50 40 Z" />
                <path d="M50 35 Q15 55 35 75 Q30 55 50 40 Z" />
              </g>
            </svg>

            {/* Center-Right Coconut Tree */}
            <svg className="absolute bottom-10 right-[35%] w-[100px] h-[100px] text-[#7198a9] opacity-60" viewBox="0 0 100 100" fill="currentColor">
              <g transform="translate(0, 0) scale(0.9) rotate(-6, 50, 50)">
                <path d="M45 100 Q55 50 48 30 L52 30 Q60 50 55 100 Z" />
                <path d="M50 30 Q90 10 95 45 Q75 35 50 35 Z" />
                <path d="M50 30 Q80 -5 50 -10 Q60 15 48 30 Z" />
                <path d="M50 30 Q10 10 5 45 Q25 35 50 35 Z" />
                <path d="M50 30 Q20 -5 50 -10 Q40 15 52 30 Z" />
                <path d="M50 35 Q85 55 65 75 Q70 55 50 40 Z" />
                <path d="M50 35 Q15 55 35 75 Q30 55 50 40 Z" />
              </g>
            </svg>

            {/* Right Coconut Trees */}
            <svg className="absolute bottom-2 right-[5%] w-[160px] h-[160px] text-[#6b9cb3] opacity-80" viewBox="0 0 100 100" fill="currentColor">
              <g transform="translate(-10, 15) scale(0.85) rotate(5, 50, 50)">
                <path d="M45 100 Q55 50 48 30 L52 30 Q60 50 55 100 Z" />
                <path d="M50 30 Q90 10 95 45 Q75 35 50 35 Z" />
                <path d="M50 30 Q80 -5 50 -10 Q60 15 48 30 Z" />
                <path d="M50 30 Q10 10 5 45 Q25 35 50 35 Z" />
                <path d="M50 30 Q20 -5 50 -10 Q40 15 52 30 Z" />
                <path d="M50 35 Q85 55 65 75 Q70 55 50 40 Z" />
                <path d="M50 35 Q15 55 35 75 Q30 55 50 40 Z" />
              </g>
              <g transform="translate(25, 45) scale(0.55) rotate(-12, 50, 50)">
                <path d="M45 100 Q55 50 48 30 L52 30 Q60 50 55 100 Z" />
                <path d="M50 30 Q90 10 95 45 Q75 35 50 35 Z" />
                <path d="M50 30 Q80 -5 50 -10 Q60 15 48 30 Z" />
                <path d="M50 30 Q10 10 5 45 Q25 35 50 35 Z" />
                <path d="M50 30 Q20 -5 50 -10 Q40 15 52 30 Z" />
                <path d="M50 35 Q85 55 65 75 Q70 55 50 40 Z" />
                <path d="M50 35 Q15 55 35 75 Q30 55 50 40 Z" />
              </g>
            </svg>
            
            {/* Foreground hills */}
            <svg viewBox="0 0 1440 120" className="absolute bottom-0 w-full h-[120px]" preserveAspectRatio="none">
              <path fill="#38BDF8" opacity="0.15" d="M0,60 C300,20 500,90 750,50 C1000,10 1200,70 1440,40 L1440,120 L0,120 Z" />
            </svg>

            {/* The Winding Road overlay */}
            <svg viewBox="0 0 1000 120" className="absolute bottom-0 w-full h-[120px]" preserveAspectRatio="none">
              <path fill="#94A3B8" opacity="0.5" d="M 380 120 C 450 60, 500 20, 600 20 L 640 20 C 540 20, 490 60, 420 120 Z" />
              <path fill="transparent" stroke="#FFFFFF" strokeWidth="2" strokeDasharray="15 15" opacity="0.6" d="M 400 120 C 470 60, 520 20, 620 20" />
            </svg>

            {/* Small Passenger Van placed naturally on the road */}
            <div className="absolute z-20 transform -rotate-[12deg] -translate-x-1/2 -translate-y-1/2" style={{ bottom: '25px', left: '44%' }}>
              <div className="w-[44px] h-[24px] bg-white rounded-md shadow-md border border-gray-100 relative flex items-center justify-center">
                <div className="absolute top-1 left-1.5 w-[10px] h-[8px] bg-sky-100 rounded-sm"></div>
                <div className="absolute top-1 right-1.5 w-[14px] h-[8px] bg-sky-100 rounded-sm"></div>
                <div className="absolute -bottom-1 left-2 w-2.5 h-2.5 bg-slate-700 rounded-full"></div>
                <div className="absolute -bottom-1 right-2 w-2.5 h-2.5 bg-slate-700 rounded-full"></div>
                <div className="absolute top-[6px] right-0 w-1.5 h-1.5 bg-yellow-300 rounded-l-full"></div>
              </div>
            </div>
          </div>

          {/* Curved Travel Route Dashed Line (Flows smoothly BELOW cards) */}
          <div className="absolute bottom-[170px] left-0 w-full h-[120px] pointer-events-none z-10 hidden lg:block">
            <svg width="100%" height="100%" preserveAspectRatio="none" viewBox="0 0 1000 120">
              {/* Perfectly calculated bezier curve connecting the location dots */}
              <path d="M 0 70 C 80 120, 120 100, 160 96 C 250 80, 320 85, 390 78 C 480 70, 560 100, 640 90 C 740 80, 810 60, 890 66 C 950 70, 1000 60, 1000 60" fill="transparent" stroke="#3B82F6" strokeWidth="2.5" strokeDasharray="10 10" opacity="0.8" />
            </svg>
            
            {/* Markers placed using the precise mathematical intersection points on the SVG coordinate grid */}
            <div className="absolute w-full h-full top-0 left-0">
              <div className="absolute flex flex-col items-center transform -translate-x-1/2 -translate-y-1/2" style={{ top: '80%', left: '16%' }}>
                <div className="w-[16px] h-[16px] rounded-full bg-[#3B82F6] flex items-center justify-center">
                  <div className="w-1.5 h-1.5 bg-white rounded-full"></div>
                </div>
              </div>
              <div className="absolute flex flex-col items-center transform -translate-x-1/2 -translate-y-1/2" style={{ top: '65%', left: '39%' }}>
                <div className="w-[16px] h-[16px] rounded-full bg-[#3B82F6] flex items-center justify-center">
                  <div className="w-1.5 h-1.5 bg-white rounded-full"></div>
                </div>
              </div>
              <div className="absolute flex flex-col items-center transform -translate-x-1/2 -translate-y-1/2" style={{ top: '75%', left: '64%' }}>
                <div className="w-[16px] h-[16px] rounded-full bg-[#3B82F6] flex items-center justify-center">
                  <div className="w-1.5 h-1.5 bg-white rounded-full"></div>
                </div>
              </div>
              <div className="absolute flex flex-col items-center transform -translate-x-1/2 -translate-y-1/2" style={{ top: '55%', left: '89%' }}>
                <div className="w-[16px] h-[16px] rounded-full bg-[#3B82F6] flex items-center justify-center">
                  <div className="w-1.5 h-1.5 bg-white rounded-full"></div>
                </div>
              </div>
            </div>
          </div>

          <div className="max-w-[1400px] mx-auto px-6 lg:px-12 relative z-20">
            <div className="mb-24 text-center max-w-2xl mx-auto">
              <span className="font-heading text-[11px] font-bold uppercase tracking-[0.2em] text-[#3B82F6] block mb-4">Features</span>
              <h2 className="font-heading text-[42px] sm:text-[50px] font-bold tracking-tight text-text mb-4">How It Works</h2>
              <p className="font-body text-lg text-text-secondary">Plan your trip seamlessly using our intelligent tools.</p>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-8 lg:gap-4 xl:gap-8">
              
              {/* Feature 01 */}
              <div className="relative group w-full max-w-[310px] mx-auto lg:mt-0">
                {/* Pale blue step number positioned behind the card */}
                <div className="absolute -top-6 -left-8 font-heading font-bold text-[85px] leading-none text-[#93C5FD] opacity-90 z-30 pointer-events-none drop-shadow-sm">01</div>
                {/* Rounded Photo Background */}
                <div className="absolute top-0 right-0 w-[95%] h-[180px] rounded-[24px] overflow-hidden z-10 shadow-md">
                  <img src="/feature_01.png" alt="Smart Itineraries" className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-700" />
                </div>
                {/* Overlapping Content Card */}
                <div className="relative z-20 w-full mt-[110px] flex flex-col">
                  {/* Stepped Corner Tab */}
                  <div className="w-[84px] h-[74px] bg-white rounded-tl-[24px] rounded-tr-[24px] flex items-center justify-center z-20 relative">
                    {/* Inward curve filler */}
                    <div className="absolute bottom-0 -right-5 w-5 h-5 bg-transparent rounded-bl-[16px] shadow-[-8px_8px_0_0_#fff]"></div>
                    {/* Light blue icon circle */}
                    <div className="w-[52px] h-[52px] rounded-full bg-[#E0F2FE] flex items-center justify-center mt-2">
                      <span className="material-symbols-outlined text-[#3B82F6]">map</span>
                    </div>
                  </div>
                  {/* Main Body */}
                  <div className="bg-white p-7 pt-4 rounded-[24px] rounded-tl-none shadow-[0_12px_40px_-12px_rgba(0,0,0,0.12)] z-20 min-h-[120px]">
                    <h4 className="font-heading text-[15px] font-bold text-text mb-2 uppercase tracking-wide">Smart Itineraries</h4>
                    <p className="font-body text-[13.5px] text-text-secondary leading-relaxed">Customize daily plans based on your interests.</p>
                  </div>
                </div>
              </div>

              {/* Feature 02 */}
              <div className="relative group w-full max-w-[310px] mx-auto lg:mt-20">
                {/* Pale blue step number */}
                <div className="absolute -top-6 -left-8 font-heading font-bold text-[85px] leading-none text-[#93C5FD] opacity-90 z-30 pointer-events-none drop-shadow-sm">02</div>
                {/* Rounded Photo Background */}
                <div className="absolute top-0 right-0 w-[95%] h-[180px] rounded-[24px] overflow-hidden z-10 shadow-md">
                  <img src="/feature_02.png" alt="Trusted Stays" className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-700" />
                </div>
                {/* Overlapping Content Card */}
                <div className="relative z-20 w-full mt-[110px] flex flex-col">
                  {/* Stepped Corner Tab */}
                  <div className="w-[84px] h-[74px] bg-white rounded-tl-[24px] rounded-tr-[24px] flex items-center justify-center z-20 relative">
                    <div className="absolute bottom-0 -right-5 w-5 h-5 bg-transparent rounded-bl-[16px] shadow-[-8px_8px_0_0_#fff]"></div>
                    <div className="w-[52px] h-[52px] rounded-full bg-[#E0F2FE] flex items-center justify-center mt-2">
                      <span className="material-symbols-outlined text-[#3B82F6]">hotel</span>
                    </div>
                  </div>
                  {/* Main Body */}
                  <div className="bg-white p-7 pt-4 rounded-[24px] rounded-tl-none shadow-[0_12px_40px_-12px_rgba(0,0,0,0.12)] z-20 min-h-[120px]">
                    <h4 className="font-heading text-[15px] font-bold text-text mb-2 uppercase tracking-wide">Trusted Stays</h4>
                    <p className="font-body text-[13.5px] text-text-secondary leading-relaxed">Verified hotel owners offering comfort and convenience.</p>
                  </div>
                </div>
              </div>

              {/* Feature 03 */}
              <div className="relative group w-full max-w-[310px] mx-auto lg:-mt-6">
                {/* Pale blue step number */}
                <div className="absolute -top-6 -left-8 font-heading font-bold text-[85px] leading-none text-[#93C5FD] opacity-90 z-30 pointer-events-none drop-shadow-sm">03</div>
                {/* Rounded Photo Background */}
                <div className="absolute top-0 right-0 w-[95%] h-[180px] rounded-[24px] overflow-hidden z-10 shadow-md">
                  <img src="/feature_03.png" alt="Easy Transport" className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-700" />
                </div>
                {/* Overlapping Content Card */}
                <div className="relative z-20 w-full mt-[110px] flex flex-col">
                  {/* Stepped Corner Tab */}
                  <div className="w-[84px] h-[74px] bg-white rounded-tl-[24px] rounded-tr-[24px] flex items-center justify-center z-20 relative">
                    <div className="absolute bottom-0 -right-5 w-5 h-5 bg-transparent rounded-bl-[16px] shadow-[-8px_8px_0_0_#fff]"></div>
                    <div className="w-[52px] h-[52px] rounded-full bg-[#E0F2FE] flex items-center justify-center mt-2">
                      <span className="material-symbols-outlined text-[#3B82F6]">directions_car</span>
                    </div>
                  </div>
                  {/* Main Body */}
                  <div className="bg-white p-7 pt-4 rounded-[24px] rounded-tl-none shadow-[0_12px_40px_-12px_rgba(0,0,0,0.12)] z-20 min-h-[120px]">
                    <h4 className="font-heading text-[15px] font-bold text-text mb-2 uppercase tracking-wide">Easy Transport</h4>
                    <p className="font-body text-[13.5px] text-text-secondary leading-relaxed">Book reliable local transport providers directly.</p>
                  </div>
                </div>
              </div>

              {/* Feature 04 */}
              <div className="relative group w-full max-w-[310px] mx-auto lg:mt-12">
                {/* Pale blue step number */}
                <div className="absolute -top-6 -left-8 font-heading font-bold text-[85px] leading-none text-[#93C5FD] opacity-90 z-30 pointer-events-none drop-shadow-sm">04</div>
                {/* Rounded Photo Background */}
                <div className="absolute top-0 right-0 w-[95%] h-[180px] rounded-[24px] overflow-hidden z-10 shadow-md">
                  <img src="/ella.png" alt="Curated Activities" className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-700" />
                </div>
                {/* Overlapping Content Card */}
                <div className="relative z-20 w-full mt-[110px] flex flex-col">
                  {/* Stepped Corner Tab */}
                  <div className="w-[84px] h-[74px] bg-white rounded-tl-[24px] rounded-tr-[24px] flex items-center justify-center z-20 relative">
                    <div className="absolute bottom-0 -right-5 w-5 h-5 bg-transparent rounded-bl-[16px] shadow-[-8px_8px_0_0_#fff]"></div>
                    <div className="w-[52px] h-[52px] rounded-full bg-[#E0F2FE] flex items-center justify-center mt-2">
                      <span className="material-symbols-outlined text-[#3B82F6]">hiking</span>
                    </div>
                  </div>
                  {/* Main Body */}
                  <div className="bg-white p-7 pt-4 rounded-[24px] rounded-tl-none shadow-[0_12px_40px_-12px_rgba(0,0,0,0.12)] z-20 min-h-[120px]">
                    <h4 className="font-heading text-[15px] font-bold text-text mb-2 uppercase tracking-wide">Curated Activities</h4>
                    <p className="font-body text-[13.5px] text-text-secondary leading-relaxed">Unique experiences guided by local suppliers.</p>
                  </div>
                </div>
              </div>

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
