import React, { useEffect, useRef, useState } from 'react';
import Button from './Button';

const ALLOWED_TYPES = ['image/jpeg', 'image/png', 'image/webp'];
const MAX_SIZE_BYTES = 5 * 1024 * 1024; // 5 MB — the API enforces the same limit

// Photo picker with a local preview.
// - `file` is the newly chosen File (or null). Nothing is uploaded here; the parent form
//   uploads it when the user presses Save.
// - `currentUrl` is the existing image, shown when no new file is chosen.
export default function PhotoPicker({ file, onFileChange, currentUrl, disabled }) {
  const inputRef = useRef(null);
  const [previewUrl, setPreviewUrl] = useState(null);
  const [pickError, setPickError] = useState(null);

  // Build a temporary local URL for the preview and release it afterwards.
  useEffect(() => {
    if (!file) {
      setPreviewUrl(null);
      return undefined;
    }
    const url = URL.createObjectURL(file);
    setPreviewUrl(url);
    return () => URL.revokeObjectURL(url);
  }, [file]);

  const handlePick = (e) => {
    const chosen = e.target.files?.[0];
    e.target.value = ''; // allow choosing the same file again later
    if (!chosen) return;

    if (!ALLOWED_TYPES.includes(chosen.type)) {
      setPickError('Only JPEG, PNG and WebP images are allowed.');
      return;
    }
    if (chosen.size > MAX_SIZE_BYTES) {
      setPickError('The photo is too large. The maximum size is 5 MB.');
      return;
    }
    setPickError(null);
    onFileChange(chosen);
  };

  const shownUrl = previewUrl || currentUrl;

  return (
    <div className="flex flex-col mb-2">
      <label className="mb-1.5 font-heading text-sm font-semibold text-text-secondary">
        Photo
      </label>

      {shownUrl && (
        <img
          src={shownUrl}
          alt="Preview"
          className="w-full max-h-48 object-cover rounded-md border border-border-neutral mb-2"
          onError={(e) => { e.currentTarget.style.display = 'none'; }}
        />
      )}

      <input
        ref={inputRef}
        type="file"
        accept="image/jpeg,image/png,image/webp"
        className="hidden"
        onChange={handlePick}
        disabled={disabled}
      />

      <div className="flex items-center gap-3">
        <Button type="button" variant="secondary" onClick={() => inputRef.current?.click()} disabled={disabled}>
          {file || currentUrl ? 'Change Photo' : 'Choose Photo'}
        </Button>
        {file && (
          <Button type="button" variant="secondary" onClick={() => { setPickError(null); onFileChange(null); }} disabled={disabled}>
            Remove Selected Photo
          </Button>
        )}
      </div>

      <span className="mt-1 text-xs text-text-secondary">
        JPEG, PNG or WebP, up to 5 MB. The photo is uploaded when you press Save.
      </span>
      {pickError && <span className="mt-1.5 text-xs text-status-danger font-body">{pickError}</span>}
    </div>
  );
}

