import React from 'react';

export default function FeatureCard({ icon, title, description }) {
  return (
    <div className="group flex flex-col items-center text-center p-6 rounded-md bg-surface-blue/70 hover:bg-border-blue/60 border border-border-blue/40 hover:shadow-soft transition-all">
      <div className="w-14 h-14 rounded-full bg-white flex items-center justify-center text-primary group-hover:scale-110 transition-transform mb-4 shadow-sm border border-border-blue/50">
        <span className="material-symbols-outlined text-[28px]">{icon}</span>
      </div>
      <h4 className="font-heading text-base font-bold text-text uppercase mb-1">{title}</h4>
      <p className="font-body text-xs text-text-secondary">{description}</p>
    </div>
  );
}
