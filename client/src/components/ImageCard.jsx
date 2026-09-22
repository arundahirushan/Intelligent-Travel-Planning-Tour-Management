import React from 'react';

export default function ImageCard({ title, description, imageUrl, actionText = 'Plan a trip here', onClick }) {
  return (
    <div 
      className="group relative bg-hero rounded-lg overflow-hidden shadow-soft aspect-[4/5] flex flex-col justify-end p-8 cursor-pointer"
      onClick={onClick}
    >
      <img 
        src={imageUrl} 
        alt={title} 
        className="absolute inset-0 w-full h-full object-cover transition-transform duration-700 ease-out group-hover:scale-105" 
      />
      <div className="absolute inset-0 bg-gradient-to-t from-hero/90 via-hero/40 to-transparent opacity-90 group-hover:opacity-95 transition-opacity"></div>
      
      <div className="relative z-10 text-white">
        <h3 className="font-heading text-2xl font-bold uppercase tracking-tight mb-2 group-hover:text-accent transition-colors">
          {title}
        </h3>
        <p className="font-body text-sm text-white/85 line-clamp-2 mb-4">
          {description}
        </p>
        <div className="inline-flex items-center gap-1 font-heading text-xs font-bold uppercase tracking-wider text-accent">
          <span>{actionText}</span>
          <span className="material-symbols-outlined text-[16px] group-hover:translate-x-1 transition-transform">
            arrow_forward
          </span>
        </div>
      </div>
    </div>
  );
}
