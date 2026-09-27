import React, { useState, useEffect, useCallback } from 'react';
import { MapContainer, TileLayer, Marker, useMapEvents } from 'react-leaflet';
import L from 'leaflet';
import Modal from './Modal';
import Input from './Input';

// Fix Vite asset bundling breaking Leaflet's default marker icon.
delete L.Icon.Default.prototype._getIconUrl;
L.Icon.Default.mergeOptions({
  iconRetinaUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png',
  iconUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png',
  shadowUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png',
});

// Sri Lanka bounding box — used as the default map center.
const SRI_LANKA_CENTER = [7.8731, 80.7718];
const SRI_LANKA_ZOOM = 8;

// Internal component that handles click-to-place-marker interaction.
// Must be rendered inside a <MapContainer>.
function ClickHandler({ onPlace }) {
  useMapEvents({
    click(e) {
      onPlace(e.latlng.lat, e.latlng.lng);
    },
  });
  return null;
}

// Interactive map modal for selecting a pickup latitude/longitude.
// Unlike PickupLocationModal (display-only), this lets the traveler click
// the map to place or move a single marker.
//
// Props:
//   isOpen       — controls visibility
//   onClose      — close handler
//   initialLat   — pre-set latitude (optional)
//   initialLng   — pre-set longitude (optional)
//   initialNote  — pre-set pickup note (optional)
//   onConfirm    — called with { lat, lng, note } when the user confirms
export default function PickupLocationPickerModal({
  isOpen,
  onClose,
  initialLat,
  initialLng,
  initialNote = '',
  onConfirm,
}) {
  const [lat, setLat] = useState(null);
  const [lng, setLng] = useState(null);
  const [note, setNote] = useState('');

  // Reset to initial values each time the modal opens.
  useEffect(() => {
    if (isOpen) {
      const parsedLat = parseFloat(initialLat);
      const parsedLng = parseFloat(initialLng);
      setLat(!isNaN(parsedLat) ? parsedLat : null);
      setLng(!isNaN(parsedLng) ? parsedLng : null);
      setNote(initialNote || '');
    }
  }, [isOpen, initialLat, initialLng, initialNote]);

  const handlePlace = useCallback((newLat, newLng) => {
    setLat(parseFloat(newLat.toFixed(7)));
    setLng(parseFloat(newLng.toFixed(7)));
  }, []);

  const handleConfirm = () => {
    if (lat === null || lng === null) return;
    onConfirm({ lat, lng, note });
    onClose();
  };

  const hasLocation = lat !== null && lng !== null;
  const googleMapsUrl = hasLocation
    ? `https://www.google.com/maps/search/?api=1&query=${lat},${lng}`
    : null;

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Select Pickup Location" size="lg">
      <div className="space-y-4">
        <p className="text-body-sm text-text-secondary">
          Click anywhere on the map to place your pickup marker. You can click again to move it.
        </p>

        {/* Map — height fixed so the modal is a predictable size */}
        <div
          className="rounded-lg overflow-hidden border border-border-neutral"
          style={{ height: '340px' }}
        >
          {/* MapContainer key forces a full remount when the modal reopens,
              which prevents the "map already initialized" Leaflet error. */}
          {isOpen && (
            <MapContainer
              key={isOpen ? 'open' : 'closed'}
              center={hasLocation ? [lat, lng] : SRI_LANKA_CENTER}
              zoom={hasLocation ? 13 : SRI_LANKA_ZOOM}
              style={{ width: '100%', height: '100%' }}
              scrollWheelZoom={false}
            >
              {/* OpenStreetMap — attribution required by OSM usage terms */}
              <TileLayer
                url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
                attribution='&copy; <a href="https://www.openstreetmap.org/copyright" target="_blank" rel="noopener noreferrer">OpenStreetMap</a> contributors'
              />
              <ClickHandler onPlace={handlePlace} />
              {hasLocation && <Marker position={[lat, lng]} />}
            </MapContainer>
          )}
        </div>

        {/* Coordinate display */}
        {hasLocation ? (
          <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-2 bg-surface-neutral rounded-md p-3">
            <div>
              <p className="text-body-sm text-text-secondary font-heading font-bold mb-0.5">Selected coordinates</p>
              <p className="text-body-md text-text font-body">
                {lat.toFixed(6)}, {lng.toFixed(6)}
              </p>
            </div>
            {googleMapsUrl && (
              <a
                href={googleMapsUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="inline-flex items-center gap-1.5 text-primary hover:underline text-body-sm font-heading font-bold whitespace-nowrap"
              >
                <span className="material-symbols-outlined text-sm">open_in_new</span>
                Open in Google Maps
              </a>
            )}
          </div>
        ) : (
          <div className="bg-surface-neutral rounded-md p-3 text-body-sm text-text-secondary text-center">
            No location selected yet — click the map to place a marker.
          </div>
        )}

        {/* Optional pickup note */}
        <Input
          label="Pickup Note (optional)"
          value={note}
          onChange={(e) => setNote(e.target.value)}
          placeholder='e.g. "Gate code 1234, call on arrival"'
        />

        {/* Action buttons */}
        <div className="flex justify-end gap-3 border-t border-border-neutral pt-4">
          <button
            type="button"
            onClick={onClose}
            className="px-5 py-2.5 rounded-pill font-heading font-bold text-label-button border border-border-neutral bg-white hover:bg-surface-neutral transition-colors"
          >
            Cancel
          </button>
          <button
            type="button"
            onClick={handleConfirm}
            disabled={!hasLocation}
            className="px-5 py-2.5 rounded-pill font-heading font-bold text-label-button bg-primary hover:bg-primary-dark text-white transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
          >
            Confirm Location
          </button>
        </div>
      </div>
    </Modal>
  );
}
