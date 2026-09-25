import React from 'react';

export default function StatusBadge({ status }) {
  let colorClass = 'bg-status-neutral/15 text-status-neutral';
  
  if (['Active', 'Confirmed', 'Available Today'].includes(status)) {
    colorClass = 'bg-status-success/15 text-status-success';
  } else if (['PendingApproval', 'Held', 'Booked Today'].includes(status)) {
    colorClass = 'bg-status-warning/15 text-status-warning';
  } else if (['Rejected'].includes(status)) {
    colorClass = 'bg-status-danger/15 text-status-danger';
  }

  // Format "PendingApproval" to "Pending Approval" for readability
  const formattedStatus = status
    ? status.replace(/([A-Z])/g, ' $1').trim()
    : 'Unknown';

  // For badges, if it's "Pending Approval", just show "Pending" to save space if preferred, 
  // but "Pending Approval" is clearer. Let's just use the formatted string but shorten if needed.
  const displayText = status === 'PendingApproval' ? 'Pending' : formattedStatus;

  return (
    <span className={`inline-flex items-center px-2 py-0.5 rounded-pill font-heading font-bold text-label-badge whitespace-nowrap ${colorClass}`}>
      {displayText}
    </span>
  );
}
