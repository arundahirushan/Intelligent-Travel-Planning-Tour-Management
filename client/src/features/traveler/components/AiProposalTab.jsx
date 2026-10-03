/**
 * AiProposalTab.jsx
 *
 * Traveler-facing AI Proposal tab within TripDetailsPage.
 *
 * Lifecycle:
 *  1. On mount: fetch the latest saved proposal (GET .../workflows/proposal).
 *  2. If none exists → show "No proposal yet" + generate button.
 *  3. If Generating → show generating state; poll every 8 s (≤ 5 polls).
 *  4. If any terminal state → render full proposal display.
 *
 * Rules enforced here:
 *  - Accept only when status === "Generated" AND m4 FinalOutcome === "Pass".
 *  - Reject available for Generated or PendingAdminApproval.
 *  - Regenerate available for any terminal state (backend guards its own lifecycle).
 *  - A browser timeout does NOT auto-trigger regeneration; we check the saved state first.
 *  - Payload and InputSnapshot arrive as parsed JS objects from the C# mapping.
 *  - Malformed payload surfaces a readable error instead of crashing the page.
 *
 * Does NOT:
 *  - Replace manual itinerary/accommodation/transport tabs.
 *  - Create holds or bookings.
 *  - Call Python or Gemini directly.
 */

import React, { useState, useEffect, useCallback, useRef } from 'react';
import Button from '../../../components/Button';
import ErrorBanner from '../../../components/ErrorBanner';
import LoadingSpinner from '../../../components/LoadingSpinner';
import Modal from '../../../components/Modal';
import {
  generateAiProposal,
  getLatestProposal,
  acceptProposal,
  rejectProposal,
} from '../../../services/travelerApi';
import {
  POLL_INTERVAL_MS,
  hasPollsLeft,
  describeGenerating,
  LEAVE_PAGE_NOTE,
} from './proposalPolling';

// ── Constants ────────────────────────────────────────────────────────────────

/** Statuses the backend can return for a TripProposal. */
const STATUS = {
  GENERATING: 'Generating',
  GENERATED: 'Generated',
  GENERATION_FAILED: 'GenerationFailed',
  NEEDS_INPUT: 'NeedsInput',
  PENDING_ADMIN: 'PendingAdminApproval',
  TRAVELER_REJECTED: 'TravelerRejected',
  ADMIN_REJECTED: 'AdminRejected',
  HOLD_PLACED: 'HoldPlaced',
  APPROVED_NO_BOOKING: 'ApprovedNoBookingRequired',
  SUPERSEDED: 'Superseded',
};

// All terminal statuses (documented for reference — used by callers).
// eslint-disable-next-line no-unused-vars
const _TERMINAL_STATUSES = new Set([
  STATUS.GENERATED,
  STATUS.GENERATION_FAILED,
  STATUS.NEEDS_INPUT,
  STATUS.PENDING_ADMIN,
  STATUS.TRAVELER_REJECTED,
  STATUS.ADMIN_REJECTED,
  STATUS.HOLD_PLACED,
  STATUS.APPROVED_NO_BOOKING,
  STATUS.SUPERSEDED,
]);

// Statuses that allow generating a new proposal (backend guards lifecycle).
const CAN_REGENERATE_STATUSES = new Set([
  STATUS.GENERATED,
  STATUS.GENERATION_FAILED,
  STATUS.NEEDS_INPUT,
  STATUS.TRAVELER_REJECTED,
  STATUS.ADMIN_REJECTED,
  STATUS.SUPERSEDED,
]);

// ── Formatting helpers ────────────────────────────────────────────────────────

function formatLKR(amount) {
  const num = Number(amount);
  if (isNaN(num)) return 'N/A';
  return `LKR ${num.toLocaleString('en-LK', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

/**
 * Format a date-only string (YYYY-MM-DD) without shifting timezone.
 * We slice the ISO string to avoid UTC→local conversion for date-only values.
 */
function formatDateStr(dateStr) {
  if (!dateStr) return 'N/A';
  // If it's a date-only string (10 chars), parse directly.
  const s = String(dateStr).slice(0, 10);
  const [y, m, d] = s.split('-').map(Number);
  if (!y || !m || !d) return dateStr;
  return new Date(y, m - 1, d).toLocaleDateString('en-US', {
    weekday: 'short', month: 'short', day: 'numeric', year: 'numeric',
  });
}

function formatDateTime(isoStr) {
  if (!isoStr) return 'N/A';
  return new Date(isoStr).toLocaleString('en-US', {
    month: 'short', day: 'numeric', year: 'numeric',
    hour: '2-digit', minute: '2-digit',
  });
}

function humanStatus(status) {
  const map = {
    [STATUS.GENERATING]: 'Generating…',
    [STATUS.GENERATED]: 'Ready for Review',
    [STATUS.GENERATION_FAILED]: 'Generation Failed',
    [STATUS.NEEDS_INPUT]: 'Needs Revision',
    [STATUS.PENDING_ADMIN]: 'Pending Admin Approval',
    [STATUS.TRAVELER_REJECTED]: 'Rejected by You',
    [STATUS.ADMIN_REJECTED]: 'Rejected by Admin',
    [STATUS.HOLD_PLACED]: 'Hold Placed',
    [STATUS.APPROVED_NO_BOOKING]: 'Approved (No Booking Required)',
    [STATUS.SUPERSEDED]: 'Superseded',
  };
  return map[status] || status || 'Unknown';
}

function statusColorClass(status) {
  if (status === STATUS.GENERATED || status === STATUS.HOLD_PLACED || status === STATUS.APPROVED_NO_BOOKING) {
    return 'bg-status-success/15 text-status-success';
  }
  if (status === STATUS.GENERATING || status === STATUS.PENDING_ADMIN) {
    return 'bg-status-warning/15 text-status-warning';
  }
  if ([STATUS.GENERATION_FAILED, STATUS.NEEDS_INPUT, STATUS.TRAVELER_REJECTED, STATUS.ADMIN_REJECTED].includes(status)) {
    return 'bg-status-danger/15 text-status-danger';
  }
  return 'bg-status-neutral/15 text-status-neutral';
}

// ── Input validation (mirrors backend requirements) ───────────────────────────

function validateTripInputs(trip) {
  const missing = [];
  if (!trip.startDate) missing.push('Start date');
  if (!trip.endDate) missing.push('End date');
  if (!trip.groupSize || trip.groupSize < 1) missing.push('Group size (must be ≥ 1)');
  if (!trip.budget || trip.budget <= 0) missing.push('Budget (must be > 0)');
  // Destinations are encoded as itineraryItems
  if (!trip.itineraryItems || trip.itineraryItems.length === 0) {
    missing.push('At least one destination (add via the Itinerary tab)');
  }
  // Pickup: null means not set. 0,0 is a valid coordinate (Gulf of Guinea).
  // We treat null as missing; we do NOT treat numeric 0 as missing.
  if (trip.pickupLatitude === null || trip.pickupLatitude === undefined ||
      trip.pickupLongitude === null || trip.pickupLongitude === undefined) {
    missing.push('Pickup location (set in trip edit)');
  }
  return missing;
}

// ── Safe payload extraction ───────────────────────────────────────────────────
// The C# mapping deserializes InputSnapshot and Payload as parsed JS objects.
// We wrap access to avoid crashing when data is incomplete.

function safeGet(obj, ...keys) {
  return keys.reduce((acc, k) => (acc != null && typeof acc === 'object' ? acc[k] : undefined), obj);
}

function parsePayload(proposal) {
  try {
    const p = proposal.payload;
    if (!p || typeof p !== 'object') return null;
    return p;
  } catch {
    return null;
  }
}

// ── Sub-components ────────────────────────────────────────────────────────────

function SectionCard({ title, icon, children, className = '' }) {
  return (
    <div className={`bg-white border border-border-neutral rounded-xl shadow-soft overflow-hidden ${className}`}>
      <div className="flex items-center gap-3 px-5 py-3 bg-surface-blue border-b border-border-blue">
        <span className="material-symbols-outlined text-primary text-sm">{icon}</span>
        <p className="font-heading font-bold text-sm text-primary">{title}</p>
      </div>
      <div className="p-5">{children}</div>
    </div>
  );
}

function InfoRow({ label, value }) {
  return (
    <div className="flex items-start justify-between gap-4 py-1.5 border-b border-border-neutral last:border-b-0">
      <p className="text-body-sm text-text-secondary shrink-0 min-w-28">{label}</p>
      <p className="text-body-sm font-heading font-bold text-text text-right">{value ?? <span className="text-text-secondary italic">N/A</span>}</p>
    </div>
  );
}

function Pill({ children, color = 'blue' }) {
  const cls = color === 'green'
    ? 'bg-status-success/10 text-status-success'
    : color === 'orange'
    ? 'bg-status-warning/10 text-status-warning'
    : color === 'red'
    ? 'bg-status-danger/10 text-status-danger'
    : 'bg-surface-blue text-primary';
  return (
    <span className={`inline-flex items-center px-2.5 py-0.5 rounded-pill font-heading font-bold text-label-badge whitespace-nowrap ${cls}`}>
      {children}
    </span>
  );
}

// ── A. Proposal Overview ──────────────────────────────────────────────────────

function ProposalOverview({ proposal, trip }) {
  const snap = proposal.inputSnapshot || {};
  const m4 = (proposal.executionSummaries || []).find(s => s.agentIdentity === 'm4_validation');
  const m4Result = m4?.resultSummary;
  const travelerMsg = safeGet(m4Result, 'travelerMessage');

  return (
    <SectionCard title="Proposal Overview" icon="info">
      <div className="space-y-0">
        <InfoRow label="Version" value={`v${proposal.version}`} />
        <InfoRow label="Created" value={formatDateTime(proposal.createdAt)} />
        <InfoRow label="Status" value={
          <span className={`inline-flex items-center px-2.5 py-0.5 rounded-pill font-heading font-bold text-label-badge ${statusColorClass(proposal.status)}`}>
            {humanStatus(proposal.status)}
          </span>
        } />
        <InfoRow label="Proposal Dates" value={
          snap.startDate
            ? `${formatDateStr(snap.startDate)} → ${formatDateStr(snap.endDate)}`
            : `${formatDateStr(trip.startDate)} → ${formatDateStr(trip.endDate)}`
        } />
        <InfoRow label="Group Size" value={snap.groupSize != null ? `${snap.groupSize} people` : `${trip.groupSize} people`} />
        <InfoRow label="Budget" value={formatLKR(snap.budget ?? trip.budget)} />
        {proposal.failureReason && (
          <InfoRow label="Failure Reason" value={
            <span className="text-status-danger">{proposal.failureReason}</span>
          } />
        )}
        {travelerMsg && (
          <div className="mt-3 p-3 bg-surface-blue rounded-lg">
            <p className="text-body-sm text-text">{travelerMsg}</p>
          </div>
        )}
      </div>
    </SectionCard>
  );
}

// ── B. Daily Itinerary (from M1 plan) ────────────────────────────────────────

function DailyItinerary({ proposal }) {
  const payload = parsePayload(proposal);
  const plan = safeGet(payload, 'plan') || safeGet(proposal, 'inputSnapshot', 'plan');
  // M1 output is in execution summary resultSummary (agentIdentity: m1_planning)
  const m1 = (proposal.executionSummaries || []).find(s => s.agentIdentity === 'm1_planning');
  const m1Result = m1?.resultSummary;

  const rawDailyVisits = safeGet(plan, 'daily_visits') || safeGet(m1Result, 'daily_visits') || [];
  const dailyVisits = Array.isArray(rawDailyVisits) ? rawDailyVisits : [];
  
  const rawOvernightSections = safeGet(plan, 'overnight_sections') || safeGet(m1Result, 'overnight_sections') || [];
  const overnightSections = Array.isArray(rawOvernightSections) ? rawOvernightSections : [];
  const planningSummary = safeGet(plan, 'planning_summary') || safeGet(m1Result, 'planning_summary');

  const isOneDayTrip = proposal.inputSnapshot?.startDate &&
    String(proposal.inputSnapshot.startDate).slice(0, 10) === String(proposal.inputSnapshot.endDate).slice(0, 10);

  if (dailyVisits.length === 0 && m1?.status !== 'Success') {
    const outcome = m1?.finalOutcome || 'Unknown';
    return (
      <SectionCard title="Daily Itinerary" icon="map">
        <p className="text-body-sm text-text-secondary">
          {m1 ? `M1 Planning: ${outcome} — itinerary data unavailable.` : 'No itinerary data in this proposal.'}
        </p>
      </SectionCard>
    );
  }

  return (
    <SectionCard title="Daily Itinerary (M1)" icon="map">
      {planningSummary?.explanation && (
        <p className="text-body-sm text-text-secondary mb-4">{planningSummary.explanation}</p>
      )}
      {isOneDayTrip && overnightSections.length === 0 && (
        <div className="mb-3 px-3 py-2 bg-surface-blue rounded-lg text-body-sm text-primary font-heading font-bold">
          No overnight accommodation required for a same-day trip.
        </div>
      )}
      <div className="space-y-3">
        {dailyVisits.map((day, i) => (
          <div key={i} className="border border-border-neutral rounded-lg overflow-hidden">
            <div className="px-4 py-2 bg-surface-blue flex items-center gap-2">
              <span className="w-6 h-6 rounded-full bg-primary text-white text-xs flex items-center justify-center font-heading font-bold shrink-0">
                {day.day_number ?? i + 1}
              </span>
              <p className="font-heading font-bold text-sm text-text">
                Day {day.day_number ?? i + 1}
                {day.date ? ` — ${formatDateStr(day.date)}` : ''}
              </p>
            </div>
            <div className="px-4 py-3">
              {Array.isArray(day.visited_area_names) && day.visited_area_names.length > 0 && (
                <div className="flex flex-wrap gap-1.5 mb-2">
                  {day.visited_area_names.map((name, j) => (
                    <Pill key={j}>{name}</Pill>
                  ))}
                </div>
              )}
              {day.overnight_area_name && (
                <p className="text-body-sm text-text-secondary">
                  <span className="font-heading font-bold">Overnight:</span> {day.overnight_area_name}
                </p>
              )}
              {day.explanation && (
                <p className="text-body-sm text-text-secondary mt-1">{day.explanation}</p>
              )}
            </div>
          </div>
        ))}
      </div>
      {Array.isArray(planningSummary?.warnings_or_limitations) && planningSummary.warnings_or_limitations.length > 0 && (
        <div className="mt-3 p-3 bg-status-warning/10 rounded-lg">
          <p className="text-body-sm font-heading font-bold text-status-warning mb-1">Planning Notes</p>
          <ul className="list-disc list-inside space-y-0.5">
            {planningSummary.warnings_or_limitations.map((w, i) => (
              <li key={i} className="text-body-sm text-text-secondary">{w}</li>
            ))}
          </ul>
        </div>
      )}
    </SectionCard>
  );
}

// ── C. Accommodation ─────────────────────────────────────────────────────────

function AccommodationDisplay({ proposal }) {
  const payload = parsePayload(proposal);
  // M2 stores selections in Hotels array inside the payload
  const hotels = safeGet(payload, 'Hotels') || safeGet(payload, 'hotels') || [];
  const m2 = (proposal.executionSummaries || []).find(s => s.agentIdentity === 'm2_accommodation');

  const isOneDayTrip = proposal.inputSnapshot?.startDate &&
    String(proposal.inputSnapshot.startDate).slice(0, 10) === String(proposal.inputSnapshot.endDate).slice(0, 10);

  if (isOneDayTrip) {
    return (
      <SectionCard title="Accommodation (M2)" icon="hotel">
        <p className="text-body-sm text-text-secondary">No overnight accommodation required for a same-day trip.</p>
      </SectionCard>
    );
  }

  if (hotels.length === 0) {
    return (
      <SectionCard title="Accommodation (M2)" icon="hotel">
        <p className="text-body-sm text-text-secondary">
          {m2?.status === 'Success' ? 'No accommodation data in payload.' : `M2 Accommodation: ${m2?.finalOutcome || 'not available'}.`}
        </p>
      </SectionCard>
    );
  }

  return (
    <SectionCard title="Accommodation (M2)" icon="hotel">
      <div className="space-y-4">
        {hotels.map((acc, i) => {
          const sectionCost = acc.SectionCost ?? acc.sectionCost;
          const roomLines = acc.RoomLines ?? acc.roomLines ?? [];
          return (
            <div key={i} className="border border-border-neutral rounded-lg overflow-hidden">
              <div className="px-4 py-3 bg-surface-blue border-b border-border-blue">
                <p className="font-heading font-bold text-text">
                  {acc.HotelName ?? acc.hotelName ?? 'Hotel (name unavailable)'}
                </p>
                <p className="text-body-sm text-text-secondary">
                  {acc.OvernightAreaName ?? acc.overnightAreaName ?? 'Area unavailable'}
                </p>
              </div>
              <div className="px-4 py-3 space-y-1">
                <InfoRow label="Check-in" value={formatDateStr(acc.CheckInDate ?? acc.checkInDate)} />
                <InfoRow label="Check-out" value={formatDateStr(acc.CheckOutDate ?? acc.checkOutDate)} />
                <InfoRow label="Nights" value={acc.NightCount ?? acc.nightCount ?? 'N/A'} />
                {sectionCost != null && (
                  <InfoRow label="Section Cost" value={<span className="text-primary">{formatLKR(sectionCost)}</span>} />
                )}
                {roomLines.length > 0 && (
                  <div className="mt-2">
                    <p className="text-body-sm font-heading font-bold text-text-secondary mb-1">Room Breakdown</p>
                    <div className="space-y-1">
                      {roomLines.map((line, j) => {
                        const type = line.RoomType ?? line.roomType ?? 'Room';
                        const qty = line.Quantity ?? line.quantity ?? 'N/A';
                        const cap = line.CapacityPerRoom ?? line.capacityPerRoom;
                        const ppn = line.PricePerNight ?? line.pricePerNight;
                        const lc = line.LineCost ?? line.lineCost;
                        return (
                          <div key={j} className="flex flex-wrap items-center gap-x-3 gap-y-0.5 p-2 bg-surface-neutral rounded">
                            <span className="text-body-sm font-heading font-bold text-text">{type}</span>
                            <span className="text-body-sm text-text-secondary">× {qty}</span>
                            {cap != null && <span className="text-body-sm text-text-secondary">{cap} guests/room</span>}
                            {ppn != null && <span className="text-body-sm text-text-secondary">{formatLKR(ppn)}/night</span>}
                            {lc != null && <span className="text-body-sm font-heading font-bold text-primary ml-auto">{formatLKR(lc)}</span>}
                          </div>
                        );
                      })}
                    </div>
                  </div>
                )}
                {acc.Explanation && (
                  <p className="text-body-sm text-text-secondary mt-2 pt-2 border-t border-border-neutral">{acc.Explanation}</p>
                )}
              </div>
            </div>
          );
        })}
      </div>
    </SectionCard>
  );
}

// ── D. Transport ─────────────────────────────────────────────────────────────

function TransportDisplay({ proposal }) {
  const payload = parsePayload(proposal);
  const vehicle = safeGet(payload, 'Vehicle') || safeGet(payload, 'vehicle');
  const m3 = (proposal.executionSummaries || []).find(s => s.agentIdentity === 'm3_transport_weather');
  const m3Result = m3?.resultSummary;
  const selectedVehicle = vehicle || safeGet(m3Result, 'selectedVehicle');

  if (!selectedVehicle) {
    return (
      <SectionCard title="Transport (M3)" icon="directions_car">
        <p className="text-body-sm text-text-secondary">
          {m3?.status === 'Success' ? 'No vehicle data in payload.' : `M3 Transport: ${m3?.finalOutcome || 'not available'}.`}
        </p>
      </SectionCard>
    );
  }

  const vid = selectedVehicle.VehicleId ?? selectedVehicle.vehicleId;
  const cap = selectedVehicle.PassengerCapacity ?? selectedVehicle.passengerCapacity ?? selectedVehicle.Capacity ?? selectedVehicle.capacity;
  const ppd = selectedVehicle.PricePerDay ?? selectedVehicle.pricePerDay;
  const start = selectedVehicle.StartDate ?? selectedVehicle.startDate;
  const end = selectedVehicle.EndDate ?? selectedVehicle.endDate;
  const days = selectedVehicle.ChargedDays ?? selectedVehicle.chargedDays;
  const transportCost = selectedVehicle.TransportCost ?? selectedVehicle.transportCost;
  const pickupLat = selectedVehicle.PickupLatitude ?? selectedVehicle.pickupLatitude;
  const pickupLng = selectedVehicle.PickupLongitude ?? selectedVehicle.pickupLongitude;
  const pickupNote = selectedVehicle.PickupNote ?? selectedVehicle.pickupNote;
  const inclusions = selectedVehicle.Inclusions ?? selectedVehicle.inclusions;
  const exclusions = selectedVehicle.Exclusions ?? selectedVehicle.exclusions;

  return (
    <SectionCard title="Transport (M3)" icon="directions_car">
      <div className="space-y-1">
        {vid != null && <InfoRow label="Vehicle ID" value={vid} />}
        {cap != null && <InfoRow label="Passenger Capacity" value={`${cap} passengers`} />}
        {start && <InfoRow label="Rental Start" value={formatDateStr(start)} />}
        {end && <InfoRow label="Rental End" value={formatDateStr(end)} />}
        {days != null && <InfoRow label="Charged Days" value={`${days} day${days !== 1 ? 's' : ''}`} />}
        {ppd != null && <InfoRow label="Price Per Day" value={formatLKR(ppd)} />}
        {transportCost != null && <InfoRow label="Transport Cost" value={<span className="text-primary font-heading font-bold">{formatLKR(transportCost)}</span>} />}
        {(pickupLat != null || pickupNote) && (
          <InfoRow
            label="Pickup"
            value={
              pickupNote
                ? pickupNote
                : pickupLat != null
                ? `${Number(pickupLat).toFixed(4)}, ${Number(pickupLng).toFixed(4)}`
                : 'Location recorded'
            }
          />
        )}
        {inclusions && <InfoRow label="Inclusions" value={inclusions} />}
        {exclusions && <InfoRow label="Exclusions" value={exclusions} />}
      </div>
    </SectionCard>
  );
}

// ── E. Cost Summary ───────────────────────────────────────────────────────────

function CostSummary({ proposal, trip }) {
  const m4 = (proposal.executionSummaries || []).find(s => s.agentIdentity === 'm4_validation');
  const validationResults = m4?.validationResults;
  const costBreakdown = safeGet(validationResults, 'costBreakdown');

  const accomCost = costBreakdown?.accommodationCost;
  const transCost = costBreakdown?.transportCost;
  const totalCost = costBreakdown?.totalCost;
  const budget = costBreakdown?.budget ?? trip.budget;
  const currency = costBreakdown?.currency || 'LKR';

  const remaining = (budget != null && totalCost != null) ? budget - totalCost : null;
  const overBudget = remaining != null && remaining < 0;

  const hasCosts = accomCost != null || transCost != null || totalCost != null;
  if (!hasCosts) {
    return (
      <SectionCard title="Cost Summary" icon="payments">
        <p className="text-body-sm text-text-secondary">
          Authoritative cost data is available after M4 validation completes.
        </p>
        <InfoRow label="Trip Budget" value={formatLKR(trip.budget)} />
      </SectionCard>
    );
  }

  return (
    <SectionCard title="Cost Summary" icon="payments">
      {accomCost != null && <InfoRow label="Accommodation" value={formatLKR(accomCost)} />}
      {transCost != null && <InfoRow label="Transport" value={formatLKR(transCost)} />}
      {totalCost != null && (
        <InfoRow
          label="Quoted Total"
          value={<span className="text-primary font-heading font-bold text-base">{formatLKR(totalCost)}</span>}
        />
      )}
      <div className="mt-2 border-t border-border-neutral pt-2">
        <InfoRow label="Trip Budget" value={formatLKR(budget)} />
        {remaining != null && (
          <InfoRow
            label="Remaining Budget"
            value={
              <span className={overBudget ? 'text-status-danger font-heading font-bold' : 'text-status-success font-heading font-bold'}>
                {overBudget ? `Over budget by ${formatLKR(Math.abs(remaining))}` : formatLKR(remaining)}
              </span>
            }
          />
        )}
      </div>
      <p className="text-label-badge text-text-secondary mt-3">
        Currency: {currency}. Quoted costs are from M4 backend validation and do not include supplier taxes or other charges not listed.
      </p>
    </SectionCard>
  );
}

// ── F. Weather ────────────────────────────────────────────────────────────────

function WeatherDisplay({ proposal }) {
  const m3 = (proposal.executionSummaries || []).find(s => s.agentIdentity === 'm3_transport_weather');
  const m3Result = m3?.resultSummary;
  const weatherData = safeGet(m3Result, 'weather') || safeGet(m3Result, 'weatherData');

  if (!weatherData || (Array.isArray(weatherData) && weatherData.length === 0)) {
    return (
      <SectionCard title="Weather Advisory" icon="wb_sunny">
        <p className="text-body-sm text-text-secondary italic">
          Weather forecast unavailable. This does not affect the validity of the proposal.
        </p>
      </SectionCard>
    );
  }

  const entries = Array.isArray(weatherData) ? weatherData : [weatherData];

  return (
    <SectionCard title="Weather Advisory" icon="wb_sunny">
      <p className="text-body-sm text-text-secondary mb-3">
        Weather is informational only. It does not affect acceptance eligibility.
      </p>
      <div className="space-y-2">
        {entries.map((w, i) => {
          const advisory = w.advisory || w.status || w.condition;
          const isFavorable = advisory && String(advisory).toLowerCase().includes('favorable');
          const isAdverse = advisory && (
            String(advisory).toLowerCase().includes('adverse') ||
            String(advisory).toLowerCase().includes('warning') ||
            String(advisory).toLowerCase().includes('unavailable')
          );
          return (
            <div key={i} className={`p-3 rounded-lg border ${isFavorable ? 'bg-status-success/5 border-status-success/20' : isAdverse ? 'bg-status-warning/5 border-status-warning/20' : 'bg-surface-neutral border-border-neutral'}`}>
              <p className="font-heading font-bold text-sm text-text">{w.destination || `Forecast ${i + 1}`}</p>
              {w.date && <p className="text-body-sm text-text-secondary">{formatDateStr(w.date)}</p>}
              {advisory && <p className="text-body-sm text-text mt-1">{advisory}</p>}
              <div className="flex flex-wrap gap-x-4 gap-y-0.5 mt-1">
                {w.temperature != null && <span className="text-body-sm text-text-secondary">{w.temperature}°{w.tempUnit || 'C'}</span>}
                {w.humidity != null && <span className="text-body-sm text-text-secondary">Humidity: {w.humidity}%</span>}
                {w.windSpeed != null && <span className="text-body-sm text-text-secondary">Wind: {w.windSpeed} {w.windUnit || 'km/h'}</span>}
                {w.precipitation != null && <span className="text-body-sm text-text-secondary">Precip: {w.precipitation} {w.precipUnit || 'mm'}</span>}
              </div>
            </div>
          );
        })}
      </div>
    </SectionCard>
  );
}

// ── G. Validation & Agent Results ─────────────────────────────────────────────

function AgentSummaries({ proposal }) {
  const [expanded, setExpanded] = useState(false);
  const summaries = proposal.executionSummaries || [];
  const m4 = summaries.find(s => s.agentIdentity === 'm4_validation');
  const validationResults = m4?.validationResults;
  const m4ResultSummary = m4?.resultSummary;

  const AGENT_LABELS = {
    m1_planning: 'M1 Planning',
    m2_accommodation: 'M2 Accommodation',
    m3_transport_weather: 'M3 Transport & Weather',
    m4_validation: 'M4 Validation',
  };

  const outcomeColor = (outcome) => {
    if (outcome === 'Pass') return 'text-status-success';
    if (outcome === 'Fail') return 'text-status-danger';
    return 'text-status-neutral';
  };

  return (
    <SectionCard title="Validation & Agent Results" icon="verified">
      {/* M4 issues */}
      {validationResults && (
        <div className="mb-4">
          {Array.isArray(validationResults.issues) && validationResults.issues.length > 0 && (
            <div className="p-3 bg-status-danger/10 rounded-lg mb-3">
              <p className="font-heading font-bold text-sm text-status-danger mb-1">Validation Issues</p>
              <ul className="list-disc list-inside space-y-0.5">
                {validationResults.issues.map((issue, i) => (
                  <li key={i} className="text-body-sm text-text">{issue}</li>
                ))}
              </ul>
            </div>
          )}
          {Array.isArray(validationResults.warnings) && validationResults.warnings.length > 0 && (
            <div className="p-3 bg-status-warning/10 rounded-lg mb-3">
              <p className="font-heading font-bold text-sm text-status-warning mb-1">Advisories</p>
              <ul className="list-disc list-inside space-y-0.5">
                {validationResults.warnings.map((w, i) => (
                  <li key={i} className="text-body-sm text-text">{w}</li>
                ))}
              </ul>
            </div>
          )}
          {validationResults.staleInputDetected && (
            <div className="p-3 bg-status-warning/10 rounded-lg mb-3">
              <p className="font-heading font-bold text-sm text-status-warning">Stale Inputs Detected</p>
              <p className="text-body-sm text-text-secondary mt-0.5">
                Your trip details changed after this proposal was generated. Generate a new proposal to use updated inputs.
              </p>
            </div>
          )}
          {Array.isArray(m4ResultSummary?.issueExplanations) && m4ResultSummary.issueExplanations.length > 0 && (
            <div className="p-3 bg-surface-blue rounded-lg mb-3">
              <p className="font-heading font-bold text-sm text-primary mb-1">M4 Explanation</p>
              <ul className="list-disc list-inside space-y-0.5">
                {m4ResultSummary.issueExplanations.map((e, i) => (
                  <li key={i} className="text-body-sm text-text">{e}</li>
                ))}
              </ul>
            </div>
          )}
        </div>
      )}

      {/* Expandable agent list */}
      <button
        onClick={() => setExpanded(v => !v)}
        className="flex items-center gap-2 text-body-sm font-heading font-bold text-primary hover:text-primary-dark transition-colors"
        aria-expanded={expanded}
      >
        <span className="material-symbols-outlined text-sm">{expanded ? 'expand_less' : 'expand_more'}</span>
        {expanded ? 'Hide' : 'Show'} agent execution details
      </button>

      {expanded && (
        <div className="mt-3 space-y-2">
          {summaries.length === 0 && (
            <p className="text-body-sm text-text-secondary">No execution summaries recorded.</p>
          )}
          {summaries.map((s, i) => {
            const label = AGENT_LABELS[s.agentIdentity] || s.agentIdentity;
            return (
              <div key={i} className="border border-border-neutral rounded-lg overflow-hidden">
                <div className="flex items-center justify-between px-4 py-2.5 bg-surface-neutral">
                  <p className="font-heading font-bold text-sm text-text">{label}</p>
                  <div className="flex items-center gap-2">
                    {s.status && <Pill>{s.status}</Pill>}
                    {s.finalOutcome && (
                      <span className={`font-heading font-bold text-sm ${outcomeColor(s.finalOutcome)}`}>
                        {s.finalOutcome}
                      </span>
                    )}
                  </div>
                </div>
                {s.resultSummary?.overallSummary && (
                  <div className="px-4 py-2 border-t border-border-neutral">
                    <p className="text-body-sm text-text">{s.resultSummary.overallSummary}</p>
                  </div>
                )}
                {s.errors && (
                  <div className="px-4 py-2 bg-status-danger/5 border-t border-status-danger/20">
                    <p className="text-body-sm text-status-danger">
                      {typeof s.errors === 'string' ? s.errors : JSON.stringify(s.errors)}
                    </p>
                  </div>
                )}
              </div>
            );
          })}
        </div>
      )}
    </SectionCard>
  );
}

// ── Proposal display ──────────────────────────────────────────────────────────

function ProposalDisplay({ proposal, trip, tripId, onProposalUpdated, onStartEdit }) {
  const [accepting, setAccepting] = useState(false);
  const [acceptError, setAcceptError] = useState(null);
  const [rejectModalOpen, setRejectModalOpen] = useState(false);
  const [rejectReason, setRejectReason] = useState('');
  const [rejectError, setRejectError] = useState(null);
  const [submittingReject, setSubmittingReject] = useState(false);

  const m4 = (proposal.executionSummaries || []).find(s => s.agentIdentity === 'm4_validation');
  const m4Pass = m4?.finalOutcome === 'Pass';
  const validationResults = m4?.validationResults;
  const isValid = validationResults?.isValid === true;

  // Acceptance eligibility: status must be Generated AND M4 must have passed.
  // We do NOT infer eligibility from Gemini sentences.
  const canAccept = proposal.status === STATUS.GENERATED && m4Pass && isValid;

  const canReject = [STATUS.GENERATED, STATUS.PENDING_ADMIN].includes(proposal.status);
  const canRegenerate = CAN_REGENERATE_STATUSES.has(proposal.status);

  const handleAccept = async () => {
    if (!canAccept) return;
    setAccepting(true);
    setAcceptError(null);
    try {
      const updated = await acceptProposal(tripId, proposal.proposalId);
      onProposalUpdated(updated);
    } catch (err) {
      const msg = err.response?.data?.message || 'Failed to accept proposal.';
      setAcceptError(msg);
    } finally {
      setAccepting(false);
    }
  };

  const handleReject = async () => {
    setSubmittingReject(true);
    setRejectError(null);
    try {
      const updated = await rejectProposal(tripId, proposal.proposalId, rejectReason);
      setRejectModalOpen(false);
      onProposalUpdated(updated);
    } catch (err) {
      setRejectError(err.response?.data?.message || 'Failed to reject proposal.');
    } finally {
      setSubmittingReject(false);
    }
  };

  return (
    <div className="space-y-6">
      {/* Action bar */}
      <div className="bg-white border border-border-neutral rounded-xl shadow-soft p-5">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
          <div>
            <div className="flex items-center gap-2 flex-wrap">
              <span className={`inline-flex items-center px-2.5 py-0.5 rounded-pill font-heading font-bold text-label-badge ${statusColorClass(proposal.status)}`}>
                {humanStatus(proposal.status)}
              </span>
              <span className="text-body-sm text-text-secondary">Version {proposal.version}</span>
            </div>
            {proposal.status === STATUS.PENDING_ADMIN && (
              <p className="text-body-sm text-text-secondary mt-1">
                Your proposal is awaiting Admin review. Nothing is reserved yet.
              </p>
            )}
            {(proposal.status === STATUS.HOLD_PLACED || proposal.status === STATUS.APPROVED_NO_BOOKING) && (
              <p className="text-body-sm text-status-success font-heading font-bold mt-1">
                {proposal.status === STATUS.HOLD_PLACED
                  ? 'Admin approved. Inventory hold has been placed. Check your bookings for details.'
                  : 'Admin approved. No inventory booking is required for this proposal.'}
              </p>
            )}
            {proposal.status === STATUS.ADMIN_REJECTED && (
              <p className="text-body-sm text-status-danger mt-1">
                Admin rejected this proposal.{proposal.failureReason ? ` Reason: ${proposal.failureReason}` : ''}
              </p>
            )}
          </div>

          <div className="flex gap-3 flex-wrap">
            {canAccept && (
              <Button
                onClick={handleAccept}
                disabled={accepting}
                aria-label="Accept proposal and send for admin review"
              >
                {accepting
                  ? <LoadingSpinner size="sm" />
                  : <span className="flex items-center gap-2">
                      <span className="material-symbols-outlined text-base">check_circle</span>
                      Accept Proposal
                    </span>
                }
              </Button>
            )}
            {proposal.status === STATUS.GENERATED && !canAccept && m4 && (
              <p className="text-body-sm text-status-danger self-center">
                Acceptance requires M4 validation to pass.
              </p>
            )}
            {canReject && (
              <Button
                variant="secondary"
                onClick={() => { setRejectReason(''); setRejectError(null); setRejectModalOpen(true); }}
                disabled={accepting}
                aria-label="Reject this proposal"
              >
                <span className="flex items-center gap-2">
                  <span className="material-symbols-outlined text-base">cancel</span>
                  Reject
                </span>
              </Button>
            )}
            {canRegenerate && (
              <Button
                variant="secondary"
                onClick={onStartEdit}
                aria-label="Generate a new proposal version"
              >
                <span className="flex items-center gap-2">
                  <span className="material-symbols-outlined text-base">refresh</span>
                  New Proposal
                </span>
              </Button>
            )}
            {[STATUS.PENDING_ADMIN, STATUS.HOLD_PLACED, STATUS.APPROVED_NO_BOOKING].includes(proposal.status) && (
              <Button
                variant="secondary"
                onClick={() => onProposalUpdated(null)}
                aria-label="Refresh proposal status"
              >
                <span className="flex items-center gap-2">
                  <span className="material-symbols-outlined text-base">sync</span>
                  Check Status
                </span>
              </Button>
            )}
          </div>
        </div>

        {acceptError && <ErrorBanner message={acceptError} className="mt-3" />}

        {canAccept && (
          <p className="text-body-sm text-text-secondary mt-3 p-3 bg-surface-blue rounded-lg">
            <strong>Accepting</strong> sends this proposal for Admin review. Nothing is reserved or booked until an Admin approves.
          </p>
        )}
      </div>

      {/* Proposal sections */}
      <ProposalOverview proposal={proposal} trip={trip} />
      <DailyItinerary proposal={proposal} />
      <AccommodationDisplay proposal={proposal} />
      <TransportDisplay proposal={proposal} />
      <CostSummary proposal={proposal} trip={trip} />
      <WeatherDisplay proposal={proposal} />
      <AgentSummaries proposal={proposal} />

      {/* Reject modal */}
      <Modal isOpen={rejectModalOpen} onClose={() => setRejectModalOpen(false)} title="Reject Proposal" size="sm">
        <div className="space-y-4">
          <p className="text-body-sm text-text-secondary">
            Rejecting removes this proposal from review. You can generate a new one at any time.
          </p>
          <div className="flex flex-col gap-1.5">
            <label className="font-heading text-sm font-semibold text-text-secondary" htmlFor="reject-reason">
              Reason <span className="text-text-secondary/60 font-normal">(optional)</span>
            </label>
            <textarea
              id="reject-reason"
              rows={3}
              value={rejectReason}
              onChange={e => setRejectReason(e.target.value)}
              className="bg-white border border-border-neutral rounded-md px-4 py-2.5 font-body text-text outline-none focus:border-primary focus:ring-1 focus:ring-primary transition-all resize-none"
              placeholder="e.g. Accommodation doesn't match preferences"
            />
          </div>
          {rejectError && <ErrorBanner message={rejectError} />}
          <div className="flex justify-end gap-3 border-t border-border-neutral pt-4">
            <Button variant="secondary" onClick={() => setRejectModalOpen(false)} disabled={submittingReject}>
              Cancel
            </Button>
            <button
              onClick={handleReject}
              disabled={submittingReject}
              className="inline-flex items-center justify-center font-heading text-xs font-bold uppercase tracking-widest rounded-pill transition-all duration-200 px-6 py-2.5 shadow-soft text-white bg-status-danger hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-status-danger disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {submittingReject ? <LoadingSpinner size="sm" /> : 'Confirm Reject'}
            </button>
          </div>
        </div>
      </Modal>
    </div>
  );
}

// ── No-proposal / pre-generation state ───────────────────────────────────────

function NoProposalState({ trip, onGenerate, generating, genError, onOpenEdit }) {
  const missingInputs = validateTripInputs(trip);
  const hasAllInputs = missingInputs.length === 0;

  return (
    <div className="space-y-6">
      {/* Explainer */}
      <div className="bg-gradient-to-br from-primary/5 to-accent/5 border border-border-blue rounded-xl p-6">
        <div className="flex items-start gap-4">
          <div className="w-12 h-12 rounded-xl bg-primary/10 flex items-center justify-center shrink-0">
            <span className="material-symbols-outlined text-primary">smart_toy</span>
          </div>
          <div>
            <h3 className="text-headline-sm font-heading font-bold text-text mb-2">AI Proposal Generation</h3>
            <p className="text-body-sm text-text-secondary mb-3">
              Our AI will analyse your trip dates, destinations, group size and budget to suggest
              a day-by-day itinerary, accommodation, and transport — all validated against live availability.
            </p>
            <p className="text-body-sm text-text-secondary">
              <strong>Generation does not reserve anything.</strong> You review the proposal first and
              choose to accept or reject before any Admin review takes place.
            </p>
          </div>
        </div>
      </div>

      {/* Trip input summary */}
      <div className="bg-white border border-border-neutral rounded-xl shadow-soft p-5">
        <p className="font-heading font-bold text-sm text-text mb-3">Saved Trip Inputs</p>
        <div className="space-y-1">
          <InfoRow label="Dates" value={trip.startDate && trip.endDate ? `${formatDateStr(trip.startDate)} → ${formatDateStr(trip.endDate)}` : <span className="text-status-danger">Missing</span>} />
          <InfoRow label="Group Size" value={trip.groupSize ? `${trip.groupSize} people` : <span className="text-status-danger">Missing</span>} />
          <InfoRow label="Budget" value={trip.budget > 0 ? formatLKR(trip.budget) : <span className="text-status-danger">Missing</span>} />
          <InfoRow
            label="Destinations"
            value={
              trip.itineraryItems?.length > 0
                ? `${trip.itineraryItems.length} area${trip.itineraryItems.length !== 1 ? 's' : ''} in itinerary`
                : <span className="text-status-danger">None added</span>
            }
          />
          <InfoRow
            label="Pickup Location"
            value={
              trip.pickupLatitude != null && trip.pickupLongitude != null
                ? trip.pickupNote || `${Number(trip.pickupLatitude).toFixed(4)}, ${Number(trip.pickupLongitude).toFixed(4)}`
                : <span className="text-status-danger">Not set</span>
            }
          />
        </div>
      </div>

      {/* Missing inputs warning */}
      {!hasAllInputs && (
        <div className="p-4 bg-status-warning/10 border border-status-warning/30 rounded-xl">
          <div className="flex gap-3">
            <span className="material-symbols-outlined text-status-warning shrink-0">warning</span>
            <div>
              <p className="font-heading font-bold text-sm text-status-warning mb-2">Required inputs missing</p>
              <ul className="list-disc list-inside space-y-0.5 mb-3">
                {missingInputs.map((item, i) => (
                  <li key={i} className="text-body-sm text-text">{item}</li>
                ))}
              </ul>
              <Button variant="secondary" onClick={onOpenEdit}>
                <span className="flex items-center gap-2">
                  <span className="material-symbols-outlined text-sm">edit</span>
                  Edit Trip to Complete Inputs
                </span>
              </Button>
            </div>
          </div>
        </div>
      )}

      {genError && <ErrorBanner message={genError} />}

      {/* Generate button */}
      <div className="flex gap-3 flex-wrap">
        <Button
          onClick={onGenerate}
          disabled={!hasAllInputs || generating}
          aria-label="Generate AI proposal"
          aria-disabled={!hasAllInputs || generating}
        >
          {generating
            ? <LoadingSpinner size="sm" />
            : <span className="flex items-center gap-2">
                <span className="material-symbols-outlined text-base">smart_toy</span>
                Generate AI Proposal
              </span>
          }
        </Button>
        {!hasAllInputs && (
          <p className="text-body-sm text-text-secondary self-center">
            Complete the missing inputs above to enable generation.
          </p>
        )}
      </div>

      {generating && (
        <div className="p-4 bg-surface-blue border border-border-blue rounded-xl" role="status" aria-live="polite">
          <div className="flex items-center gap-3">
            <LoadingSpinner size="md" />
            <div>
              <p className="font-heading font-bold text-sm text-text">Generating your proposal…</p>
              <p className="text-body-sm text-text-secondary">
                This may take up to 2 minutes. You can leave and come back — the result will be saved.
              </p>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

// ── Generating state (proposal exists but status = Generating) ────────────────

function GeneratingState({ proposal, onCheckStatus, checking, pollsExhausted, lastCheckedAt }) {
  const view = describeGenerating({ pollsExhausted, checking });
  return (
    <div className="space-y-4">
      <div className="p-6 bg-surface-blue border border-border-blue rounded-xl" role="status" aria-live="polite">
        <div className="flex items-center gap-4">
          {view.showSpinner ? (
            <LoadingSpinner size="lg" />
          ) : (
            <span className="material-symbols-outlined text-3xl text-text-secondary">hourglass_top</span>
          )}
          <div>
            <p className="font-heading font-bold text-text">{view.title}</p>
            <p className="text-body-sm text-text-secondary mt-1">
              Version {proposal.version} was started at {formatDateTime(proposal.createdAt)}. {view.detail}
              {lastCheckedAt && ` Last checked at ${lastCheckedAt.toLocaleTimeString()}.`}
            </p>
            <p className="text-body-sm text-text-secondary mt-1">{LEAVE_PAGE_NOTE}</p>
          </div>
        </div>
      </div>
      <Button variant="secondary" onClick={onCheckStatus} disabled={checking} aria-label="Check current proposal status">
        <span className="flex items-center gap-2">
          <span className="material-symbols-outlined text-base">sync</span>
          Check Status
        </span>
      </Button>
    </div>
  );
}

// ── Main AiProposalTab ────────────────────────────────────────────────────────

export default function AiProposalTab({ trip, onOpenEditTrip }) {
  const tripId = trip.id;
  const tripIdRef = useRef(tripId);

  // proposal: null = not loaded yet; 'none' = no proposal exists; object = proposal dto
  const [proposal, setProposal] = useState(null);
  const [loadingProposal, setLoadingProposal] = useState(true);
  const [loadError, setLoadError] = useState(null);

  const [generating, setGenerating] = useState(false);
  const [genError, setGenError] = useState(null);

  // Display state for the Generating panel (see proposalPolling.js).
  const [pollsExhausted, setPollsExhausted] = useState(false); // auto-polling used up its budget
  const [checking, setChecking] = useState(false); // a manual "Check Status" is in flight
  const [lastCheckedAt, setLastCheckedAt] = useState(null);

  const pollCountRef = useRef(0);
  const pollTimerRef = useRef(null);

  // Clear polling on unmount or trip change. Also resets the poll budget and the paused display.
  const stopPolling = useCallback(() => {
    if (pollTimerRef.current) {
      clearTimeout(pollTimerRef.current);
      pollTimerRef.current = null;
    }
    pollCountRef.current = 0;
    setPollsExhausted(false);
  }, []);

  // Fetch the latest saved proposal. Does NOT auto-generate.
  const fetchProposal = useCallback(async (silent = false) => {
    if (!silent) setLoadingProposal(true);
    setLoadError(null);
    try {
      const data = await getLatestProposal(tripId);
      // Guard against stale responses if the user navigated to a different trip.
      if (tripIdRef.current !== tripId) return;
      setProposal(data);
      setLastCheckedAt(new Date());
    } catch (err) {
      if (tripIdRef.current !== tripId) return;
      const status = err.response?.status;
      if (status === 404) {
        setProposal('none'); // backend returns 404 when no proposal exists
      } else {
        setLoadError(err.response?.data?.message || 'Could not load proposal. Check your connection and try again.');
      }
    } finally {
      if (!silent) setLoadingProposal(false);
    }
  }, [tripId]);

  // Start bounded polling while status = Generating. When the budget is used up we say so
  // (pollsExhausted) instead of leaving a spinner that implies live progress.
  const schedulePoll = useCallback(() => {
    if (!hasPollsLeft(pollCountRef.current)) {
      setPollsExhausted(true);
      return;
    }
    pollTimerRef.current = setTimeout(async () => {
      pollCountRef.current += 1;
      await fetchProposal(true);
    }, POLL_INTERVAL_MS);
  }, [fetchProposal]);

  // When proposal changes, decide whether to keep polling.
  useEffect(() => {
    if (!proposal || proposal === 'none') {
      stopPolling();
      return;
    }
    if (proposal.status === STATUS.GENERATING) {
      schedulePoll();
    } else {
      stopPolling();
    }
  }, [proposal, schedulePoll, stopPolling]);

  // Fetch once on mount (or when tripId changes).
  useEffect(() => {
    tripIdRef.current = tripId;
    stopPolling();
    setProposal(null);
    setGenError(null);
    fetchProposal();
    return () => stopPolling();
  }, [tripId, fetchProposal, stopPolling]);

  const handleGenerate = async () => {
    if (generating) return; // prevent double-click
    setGenerating(true);
    setGenError(null);
    try {
      const result = await generateAiProposal(tripId);
      if (tripIdRef.current !== tripId) return;
      setProposal(result);
    } catch (err) {
      if (tripIdRef.current !== tripId) return;
      const status = err.response?.status;
      const msg = err.response?.data?.message;

      if (err.code === 'ECONNABORTED' || err.message?.includes('timeout')) {
        // Timeout: generation may still be running on the server.
        // Attempt to retrieve the saved state rather than showing a failure.
        setGenError(
          'The request timed out. Generation may still be running on the server. Use "Check Status" to see the current result.'
        );
        await fetchProposal(true);
      } else if (status === 400 && msg?.toLowerCase().includes('already being generated')) {
        // Idempotency: a generation is already in progress.
        setGenError('A proposal is already being generated. Checking current status…');
        await fetchProposal(true);
      } else {
        setGenError(msg || 'Failed to start generation. Please try again.');
      }
    } finally {
      setGenerating(false);
    }
  };

  const handleProposalUpdated = async (updatedProposal) => {
    if (updatedProposal === null) {
      // Explicit refresh request.
      await fetchProposal();
    } else {
      setProposal(updatedProposal);
    }
  };

  // Manual refresh: fetch fresh data in place (the panel shows "Checking status…"), then either show the
  // new result or a clear "still processing" message. A fresh check also restarts auto-polling.
  const handleCheckStatus = async () => {
    if (checking) return;
    setChecking(true);
    stopPolling(); // resets the poll budget and the paused display
    try {
      await fetchProposal(true);
    } finally {
      setChecking(false);
    }
  };

  // ── Render ────────────────────────────────────────────────────────────────

  if (loadingProposal) {
    return (
      <div className="flex items-center justify-center py-16" role="status" aria-label="Loading proposal">
        <LoadingSpinner size="lg" />
      </div>
    );
  }

  if (loadError) {
    return (
      <div className="space-y-4">
        <ErrorBanner message={loadError} />
        <p className="text-body-sm text-text-secondary">
          This could be a network or authentication issue. Your previous proposal (if any) is still saved on the server.
        </p>
        <Button variant="secondary" onClick={handleCheckStatus}>
          <span className="flex items-center gap-2">
            <span className="material-symbols-outlined text-base">refresh</span>
            Retry
          </span>
        </Button>
      </div>
    );
  }

  // No proposal yet — or user wants to generate a new one after rejecting.
  if (!proposal || proposal === 'none') {
    return (
      <NoProposalState
        trip={trip}
        onGenerate={handleGenerate}
        generating={generating}
        genError={genError}
        onOpenEdit={onOpenEditTrip}
      />
    );
  }

  // Currently generating — show bounded polling state.
  if (proposal.status === STATUS.GENERATING) {
    return (
      <GeneratingState
        proposal={proposal}
        onCheckStatus={handleCheckStatus}
        checking={checking}
        pollsExhausted={pollsExhausted}
        lastCheckedAt={lastCheckedAt}
      />
    );
  }

  // Terminal state — display the full proposal.
  return (
    <ProposalDisplay
      proposal={proposal}
      trip={trip}
      tripId={tripId}
      onProposalUpdated={handleProposalUpdated}
      onStartEdit={() => setProposal('none')} // let user initiate new generation
    />
  );
}
