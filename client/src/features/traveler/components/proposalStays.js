/**
 * proposalStays.js
 *
 * Pure helpers that turn the saved proposal room selections into display "stays".
 * Kept free of React so they can be unit-tested with `node --test`.
 */

export const UNAVAILABLE = 'Details unavailable';

/** Read a YYYY-MM-DD calendar date from a string; null if missing or not a real date. */
export function parseCalendarDate(value) {
  const m = /^(\d{4})-(\d{2})-(\d{2})/.exec(String(value ?? ''));
  if (!m) return null;
  const [y, mo, d] = [Number(m[1]), Number(m[2]), Number(m[3])];
  const utc = new Date(Date.UTC(y, mo - 1, d));
  const real = utc.getUTCFullYear() === y && utc.getUTCMonth() === mo - 1 && utc.getUTCDate() === d;
  return real ? utc : null;
}

/**
 * Nights between two calendar dates. Both dates are read as UTC midnight, so the browser
 * timezone and daylight-saving changes cannot change the result.
 * Returns null when a date is missing/invalid or check-out is not after check-in.
 */
export function calcNights(checkIn, checkOut) {
  const a = parseCalendarDate(checkIn);
  const b = parseCalendarDate(checkOut);
  if (!a || !b) return null;
  const nights = Math.round((b - a) / 86400000);
  return nights > 0 ? nights : null;
}

/**
 * Group the saved room selections into stays: one stay per hotel + check-in + check-out.
 * Different room types in the same stay stay together; different dates stay separate.
 * A room we could not look up gets its own stay (we cannot tell which hotel it belongs to).
 */
export function groupStays(hotelItems, roomDetails) {
  const detailsByRoomId = new Map((roomDetails || []).map(r => [r.roomId, r]));
  const stays = new Map();
  (hotelItems || []).forEach((item) => {
    const roomId = item.RoomId ?? item.roomId;
    const checkIn = item.CheckInDate ?? item.checkInDate;
    const checkOut = item.CheckOutDate ?? item.checkOutDate;
    const quantity = item.NumberOfRooms ?? item.numberOfRooms;
    const detail = detailsByRoomId.get(roomId) || null;
    const hotelKey = detail ? `hotel-${detail.hotelId}` : `unknown-room-${roomId}`;
    const key = `${hotelKey}|${String(checkIn).slice(0, 10)}|${String(checkOut).slice(0, 10)}`;
    if (!stays.has(key)) {
      stays.set(key, { key, detail, checkIn, checkOut, rooms: [] });
    }
    stays.get(key).rooms.push({ roomId, quantity, detail });
  });
  return [...stays.values()];
}

/** e.g. "Double room · 2 guests per room · 2 rooms selected" */
export function roomLineText(room) {
  const count = room.quantity != null
    ? `${room.quantity} room${room.quantity === 1 ? '' : 's'} selected`
    : 'rooms selected: unavailable';
  if (!room.detail) return `${UNAVAILABLE} · ${count}`;
  const guests = `${room.detail.capacity} guest${room.detail.capacity === 1 ? '' : 's'} per room`;
  return `${room.detail.roomType} · ${guests} · ${count}`;
}
