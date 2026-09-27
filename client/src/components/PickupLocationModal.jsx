import React, { useEffect } from 'react';
import { MapContainer, TileLayer, Marker, Popup } from 'react-leaflet';
import L from 'leaflet';
import Modal from './Modal';

// Fix the default marker icon broken by Vite's asset bundling.
// Leaflet tries to load marker images relative to its own CSS file, which doesn't
// work in a Vite bundle — so we point it at the correct URLs manually.
delete L.Icon.Default.prototype._getIconUrl;
L.Icon.Default.mergeOptions({
  iconRetinaUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png',
  iconUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png',
  shadowUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png',
});

// Shared component for displaying a pickup location on a map.
// Used by BookingsPage (transport-provider) and any future booking screen that
// needs to show a coordinate-based pickup point.
//
// Props:
//   isOpen    — controls modal visibility
//   onClose   — close handler
//   latitude  — decimal latitude (number or string)
//   longitude — decimal longitude (number or string)
//   note      — optional pickup note text
export default function PickupLocationModal({ isOpen, onClose, latitude, longitude, note }) {
  const lat = parseFloat(latitude);
  const lng = parseFloat(longitude);
  const isValid = !isNaN(lat) && !isNaN(lng);

  const googleMapsUrl = `https://www.google.com/maps/search/?api=1&query=${lat},${lng}`;

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Pickup Location" size="lg">
      {!isValid ? (
        <p className="text-body-md text-text-secondary text-center py-8">
          No location coordinates available.
        </p>
      ) : (
        <div className="space-y-4">
          {/* Leaflet map — height is fixed so the modal stays a predictable size */}
          <div className="rounded-lg overflow-hidden border border-border-neutral" style={{ height: '320px' }}>
            <MapContainer
              center={[lat, lng]}
              zoom={14}
              style={{ width: '100%', height: '100%' }}
              // Disable scroll zoom so the user doesn't accidentally zoom while
              // scrolling the page — they can still pinch-zoom or use +/- controls.
              scrollWheelZoom={false}
            >
              {/* OpenStreetMap tile layer — attribution is required by OSM usage terms */}
              <TileLayer
                url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
                attribution='&copy; <a href="https://www.openstreetmap.org/copyright" target="_blank" rel="noopener noreferrer">OpenStreetMap</a> contributors'
              />
              <Marker position={[lat, lng]}>
                <Popup>Pickup location</Popup>
              </Marker>
            </MapContainer>
          </div>

          {/* Optional pickup note */}
          {note && (
            <div className="bg-surface-neutral rounded-md p-3">
              <p className="text-body-sm text-text-secondary font-heading font-bold mb-1">Pickup Note</p>
              <p className="text-body-md text-text">{note}</p>
            </div>
          )}

          {/* Coordinates + Google Maps link */}
          <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-3 pt-2">
            <p className="text-body-sm text-text-secondary font-body">
              {lat.toFixed(6)}, {lng.toFixed(6)}
            </p>
            <a
              href={googleMapsUrl}
              target="_blank"
              rel="noopener noreferrer"
              className="inline-flex items-center gap-2 px-4 py-2 rounded-pill bg-primary hover:bg-primary-dark text-white font-heading font-bold text-label-button transition-colors"
            >
              <span className="material-symbols-outlined text-base">open_in_new</span>
              Open in Google Maps
            </a>
          </div>
        </div>
      )}
    </Modal>
  );
}

