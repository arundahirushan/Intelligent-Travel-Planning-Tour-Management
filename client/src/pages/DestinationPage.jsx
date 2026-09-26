import React, { useEffect } from 'react';
import { useParams, Navigate, Link } from 'react-router-dom';
import { destinations } from '../data/destinations';
import { useAuth } from '../context/AuthContext';

export default function DestinationPage() {
  const { id } = useParams();
  const destination = destinations[id];
  const { isAuthenticated, user } = useAuth();

  // Scroll to top on load
  useEffect(() => {
    window.scrollTo(0, 0);
  }, [id]);

  if (!destination) {
    return <Navigate to="/" replace />;
  }

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
    <div className="min-h-screen bg-canvas pb-20">
      {/* Navbar overlay header (we'll just use a small dark gradient at top to make nav readable if it's transparent, or assume global nav works) */}
      <header className="fixed top-0 left-0 right-0 z-50 bg-canvas/90 backdrop-blur-md border-b border-border-blue/70 transition-all">
        <div className="max-w-7xl mx-auto h-20 px-6 lg:px-12 flex items-center justify-between">
          <Link to="/" className="flex items-center gap-3 group">
            <img src="/logo.png" alt="Easy Planner Logo" className="h-8 w-auto object-contain" />
            <span className="font-heading text-xl font-bold tracking-tight text-text hidden sm:inline-block">Easy Planner</span>
          </Link>
          <div className="flex items-center gap-4">
            {isAuthenticated ? (
              <Link to={getDashboardPath()} className="inline-flex items-center justify-center px-6 py-2 bg-primary text-white font-heading text-xs font-bold uppercase tracking-widest rounded-pill hover:bg-primary-dark transition-all shadow-sm">
                Dashboard
              </Link>
            ) : (
              <Link to="/login" className="inline-flex items-center justify-center px-6 py-2 bg-primary text-white font-heading text-xs font-bold uppercase tracking-widest rounded-pill hover:bg-primary-dark transition-all shadow-sm">
                Log in
              </Link>
            )}
          </div>
        </div>
      </header>

      {/* 1. HERO SECTION */}
      <section className="relative w-full h-[60vh] min-h-[500px] flex flex-col justify-end">
        <img 
          src={destination.heroImage} 
          alt={destination.name} 
          className="absolute inset-0 w-full h-full object-cover" 
        />
        <div className="absolute inset-0 bg-gradient-to-t from-black/80 via-black/40 to-black/10"></div>
        
        <div className="relative z-10 max-w-5xl mx-auto px-6 lg:px-12 w-full pb-16">
          <span className="inline-block px-3 py-1 bg-white/20 backdrop-blur-md rounded-full text-white font-heading text-[10px] font-bold uppercase tracking-widest border border-white/10 shadow-sm mb-4">
            {destination.region}
          </span>
          <h1 className="font-heading text-4xl sm:text-6xl font-bold text-white uppercase tracking-tight mb-4">
            {destination.name}
          </h1>
          <p className="font-body text-lg sm:text-xl text-white/90 max-w-2xl">
            {destination.tagline}
          </p>
        </div>
      </section>

      {/* 2. QUICK INFORMATION BAR */}
      <div className="max-w-5xl mx-auto px-6 lg:px-12 -mt-8 relative z-20">
        <div className="bg-white rounded-2xl shadow-lg border border-border-blue/50 p-6 flex flex-wrap gap-6 justify-between items-start">
          {Object.entries(destination.quickInfo).map(([key, value]) => (
            <div key={key} className="flex-1 min-w-[150px]">
              <span className="block font-heading text-[10px] uppercase tracking-widest text-text-secondary mb-1">{key}</span>
              <span className="block font-body text-sm font-semibold text-text">{value}</span>
            </div>
          ))}
        </div>
      </div>

      <div className="max-w-5xl mx-auto px-6 lg:px-12 mt-16 grid grid-cols-1 lg:grid-cols-3 gap-16">
        
        {/* Main Content Column */}
        <div className="lg:col-span-2 space-y-16">
          
          {/* 3. OVERVIEW */}
          <section>
            <h2 className="font-heading text-2xl sm:text-3xl font-bold text-text uppercase tracking-tight mb-6">
              About {destination.name.split('–')[0].trim()}
            </h2>
            <div className="space-y-4 font-body text-base text-text-secondary leading-relaxed">
              {destination.overview.map((paragraph, idx) => (
                <p key={idx}>{paragraph}</p>
              ))}
            </div>
          </section>

          {/* 4. WHY VISIT */}
          <section>
            <h2 className="font-heading text-2xl sm:text-3xl font-bold text-text uppercase tracking-tight mb-6">
              Why Visit {destination.name.split('–')[0].trim()}?
            </h2>
            <ul className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              {destination.whyVisit.map((reason, idx) => (
                <li key={idx} className="flex items-start gap-3 bg-surface-light p-4 rounded-xl border border-border-blue/30">
                  <span className="material-symbols-outlined text-primary text-xl shrink-0">check_circle</span>
                  <span className="font-body text-sm text-text font-medium">{reason}</span>
                </li>
              ))}
            </ul>
          </section>

          {/* 5. HISTORY / CULTURAL IMPORTANCE */}
          <section>
            <h2 className="font-heading text-2xl sm:text-3xl font-bold text-text uppercase tracking-tight mb-6">
              {destination.historyTitle || "History & Importance"}
            </h2>
            <div className="space-y-4 font-body text-base text-text-secondary leading-relaxed">
              {destination.history.map((paragraph, idx) => (
                <p key={idx}>{paragraph}</p>
              ))}
            </div>
          </section>

          {/* 6. TOP HIGHLIGHTS */}
          <section>
            <h2 className="font-heading text-2xl sm:text-3xl font-bold text-text uppercase tracking-tight mb-6">
              Top Highlights
            </h2>
            <div className="space-y-6">
              {destination.highlights.map((highlight, idx) => (
                <div key={idx} className="flex flex-col sm:flex-row sm:items-baseline gap-2 sm:gap-6 border-b border-border-blue/50 pb-6 last:border-0 last:pb-0">
                  <h4 className="font-heading text-lg font-bold text-text sm:w-1/3 shrink-0">{highlight.title}</h4>
                  <p className="font-body text-base text-text-secondary sm:w-2/3">{highlight.desc}</p>
                </div>
              ))}
            </div>
          </section>

        </div>

        {/* Sidebar Column */}
        <div className="lg:col-span-1 space-y-12">
          
          {/* 7. THINGS TO DO */}
          <section className="bg-surface-blue rounded-2xl p-6 sm:p-8">
            <h3 className="font-heading text-xl font-bold text-text uppercase tracking-tight mb-6 flex items-center gap-2">
              <span className="material-symbols-outlined text-primary">explore</span>
              Things To Do
            </h3>
            <ul className="space-y-3">
              {destination.thingsToDo.map((item, idx) => (
                <li key={idx} className="flex items-start gap-3 font-body text-sm text-text-secondary">
                  <span className="material-symbols-outlined text-primary/60 text-lg shrink-0">arrow_right</span>
                  {item}
                </li>
              ))}
            </ul>
          </section>

          {/* 8. BEST TIME TO VISIT */}
          <section>
            <h3 className="font-heading text-xl font-bold text-text uppercase tracking-tight mb-4 flex items-center gap-2">
              <span className="material-symbols-outlined text-primary">calendar_month</span>
              Best Time to Visit
            </h3>
            <div className="space-y-3 font-body text-sm text-text-secondary leading-relaxed">
              {destination.bestTime.map((paragraph, idx) => (
                <p key={idx}>{paragraph}</p>
              ))}
            </div>
          </section>

          {/* 9. GOOD TO KNOW */}
          <section>
            <h3 className="font-heading text-xl font-bold text-text uppercase tracking-tight mb-4 flex items-center gap-2">
              <span className="material-symbols-outlined text-primary">info</span>
              Good To Know
            </h3>
            <ul className="space-y-2 font-body text-sm text-text-secondary">
              {destination.goodToKnow.map((item, idx) => (
                <li key={idx} className="flex gap-2">
                  <span className="text-primary">•</span>
                  <span>{item}</span>
                </li>
              ))}
            </ul>
          </section>

          {/* 10. QUICK FACTS */}
          <section className="bg-surface-light border border-border-blue rounded-2xl p-6 sm:p-8">
            <h3 className="font-heading text-xl font-bold text-text uppercase tracking-tight mb-6">
              Quick Facts
            </h3>
            <div className="space-y-4">
              {Object.entries(destination.quickFacts).map(([key, value]) => (
                <div key={key}>
                  <span className="block font-heading text-[10px] uppercase tracking-widest text-text-secondary">{key}</span>
                  <span className="block font-body text-sm font-medium text-text mt-1">{value}</span>
                </div>
              ))}
            </div>
          </section>

        </div>
      </div>

      {/* 11. PLAN YOUR TRIP CTA */}
      <section className="w-full mt-24 px-6 lg:px-12">
        <div className="max-w-4xl mx-auto bg-primary text-white rounded-[32px] p-10 sm:p-16 text-center shadow-lg relative overflow-hidden">
          <div className="absolute inset-0 bg-hero opacity-10 mix-blend-overlay"></div>
          <div className="relative z-10">
            <h2 className="font-heading text-3xl sm:text-4xl font-bold uppercase tracking-tight mb-4">
              Ready to Explore {destination.name.split('–')[0].trim()}?
            </h2>
            <p className="font-body text-base sm:text-lg text-white/90 leading-relaxed mb-8 max-w-xl mx-auto">
              Add this destination to your custom Sri Lankan itinerary and let our intelligent engine craft your perfect route.
            </p>
            <Link to={isAuthenticated ? getDashboardPath() : "/register"} className="inline-flex items-center justify-center px-8 py-3.5 bg-white text-primary font-heading text-xs font-bold uppercase tracking-widest rounded-pill hover:bg-gray-100 transition-all shadow-md">
              Plan A Trip Here
            </Link>
          </div>
        </div>
      </section>

    </div>
  );
}
