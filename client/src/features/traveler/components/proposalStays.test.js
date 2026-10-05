import test from 'node:test';
import assert from 'node:assert/strict';
import { calcNights, groupStays, roomLineText, UNAVAILABLE } from './proposalStays.js';

const rooms = [
  { roomId: 1, hotelId: 10, hotelName: 'Sea View', destinationName: 'Galle', roomType: 'Double room', capacity: 2 },
  { roomId: 2, hotelId: 10, hotelName: 'Sea View', destinationName: 'Galle', roomType: 'Triple room', capacity: 3 },
  { roomId: 3, hotelId: 20, hotelName: 'Hill Inn', destinationName: 'Kandy', roomType: 'Single room', capacity: 1 },
];
const item = (RoomId, CheckInDate, CheckOutDate, NumberOfRooms) => ({ RoomId, CheckInDate, CheckOutDate, NumberOfRooms });

test('mixed room types in one hotel stay are grouped into a single card', () => {
  const stays = groupStays([item(1, '2026-10-01', '2026-10-03', 2), item(2, '2026-10-01', '2026-10-03', 1)], rooms);
  assert.equal(stays.length, 1);
  assert.equal(stays[0].rooms.length, 2);
  assert.equal(roomLineText(stays[0].rooms[0]), 'Double room · 2 guests per room · 2 rooms selected');
  assert.equal(roomLineText(stays[0].rooms[1]), 'Triple room · 3 guests per room · 1 room selected');
});

test('different hotels and different stay dates stay separate', () => {
  const stays = groupStays([
    item(1, '2026-10-01', '2026-10-03', 1),
    item(3, '2026-10-01', '2026-10-03', 1),
    item(1, '2026-10-03', '2026-10-05', 1),
  ], rooms);
  assert.equal(stays.length, 3);
});

test('timestamps on the same calendar dates still group together', () => {
  const stays = groupStays([item(1, '2026-10-01T00:00:00Z', '2026-10-03T00:00:00Z', 1), item(2, '2026-10-01', '2026-10-03', 1)], rooms);
  assert.equal(stays.length, 1);
});

test('a missing room record keeps its ID, dates and quantity and shows "Details unavailable"', () => {
  const stays = groupStays([item(99, '2026-10-01', '2026-10-03', 2)], rooms);
  assert.equal(stays.length, 1);
  assert.equal(stays[0].detail, null);
  assert.equal(stays[0].rooms[0].roomId, 99);
  assert.equal(stays[0].checkIn, '2026-10-01');
  assert.equal(roomLineText(stays[0].rooms[0]), `${UNAVAILABLE} · 2 rooms selected`);
});

test('missing displayDetails entirely does not crash', () => {
  assert.equal(groupStays([item(1, '2026-10-01', '2026-10-02', 1)], undefined).length, 1);
  assert.deepEqual(groupStays(undefined, undefined), []);
});

test('nights are counted from calendar dates', () => {
  assert.equal(calcNights('2026-10-01', '2026-10-03'), 2);
  assert.equal(calcNights('2026-10-01T00:00:00Z', '2026-10-05T00:00:00'), 4);
  assert.equal(calcNights('2026-02-28', '2026-03-01'), 1);
});

test('nights are not changed by a daylight-saving change', () => {
  // US clocks go back on 2026-11-01; UTC date arithmetic still gives whole nights.
  assert.equal(calcNights('2026-10-31', '2026-11-02'), 2);
  assert.equal(calcNights('2026-03-07', '2026-03-09'), 2);
});

test('missing, invalid or reversed dates give null instead of an invented number', () => {
  assert.equal(calcNights(undefined, '2026-10-03'), null);
  assert.equal(calcNights('2026-10-01', null), null);
  assert.equal(calcNights('2026-13-01', '2026-13-03'), null);
  assert.equal(calcNights('2026-02-30', '2026-03-02'), null);
  assert.equal(calcNights('2026-10-03', '2026-10-01'), null);
  assert.equal(calcNights('2026-10-01', '2026-10-01'), null);
});
