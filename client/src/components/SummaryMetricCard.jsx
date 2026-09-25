import React from 'react';
import ProgressBar from './ProgressBar';

export default function SummaryMetricCard({ 
  icon, 
  label, 
  value, 
  badge, 
  progressPercentage, 
  progressLabel 
}) {
  return (
    <div className="bg-white border border-border-blue rounded-xl shadow-soft p-[var(--space-lg)] flex flex-col">
      <div className="flex items-center gap-3 mb-4">
        <div className="w-8 h-8 flex items-center justify-center bg-surface-blue rounded-md text-primary">
          <span className="material-symbols-outlined text-lg">{icon}</span>
        </div>
        <span className="text-label-uppercase text-text-secondary tracking-widest">{label}</span>
      </div>
      
      <div className="flex items-baseline gap-3 mb-2">
        <span className="text-headline-lg font-heading font-bold text-text">{value}</span>
        {badge && (
          <span className={`inline-flex px-2 py-0.5 rounded-pill text-label-badge font-heading font-bold ${badge.colorClass}`}>
            {badge.text}
          </span>
        )}
      </div>

      {progressPercentage !== undefined && (
        <div className="mt-auto pt-2">
          <ProgressBar percentage={progressPercentage} label={progressLabel} />
        </div>
      )}
    </div>
  );
}
