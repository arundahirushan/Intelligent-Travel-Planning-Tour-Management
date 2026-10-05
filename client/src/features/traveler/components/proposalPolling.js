/**
 * proposalPolling.js
 *
 * Small pure helpers for the "Generating" panel of the AI Proposal tab.
 * Kept free of React so the display rules can be unit-tested with `npm test`.
 */

export const POLL_INTERVAL_MS = 8_000;
// Stop auto-refreshing after ~40 s; the user can keep checking with "Check Status".
export const MAX_POLLS = 5;

/** True while the automatic polling budget has not been used up. */
export function hasPollsLeft(pollCount) {
  return pollCount < MAX_POLLS;
}

/**
 * What the Generating panel should say.
 *  - spinner only while we are really refreshing (auto-polling or a manual check);
 *  - once automatic polling stops we say so, instead of an endless spinner implying live progress.
 */
export function describeGenerating({ pollsExhausted, checking }) {
  if (checking) {
    return {
      showSpinner: true,
      title: 'Checking status…',
      detail: 'Asking the server for the latest result.',
    };
  }
  if (pollsExhausted) {
    return {
      showSpinner: false,
      title: 'Still processing',
      detail: 'Automatic refresh has paused. Press “Check Status” to look again.',
    };
  }
  return {
    showSpinner: true,
    title: 'Proposal generation in progress',
    detail: 'This page refreshes automatically.',
  };
}

/** Accurate description of what happens if the user leaves the page. */
export const LEAVE_PAGE_NOTE =
  'You can leave this page: generation keeps running on the server and the result is saved when it ' +
  'finishes. Come back and press “Check Status”. If an attempt is interrupted (for example the server ' +
  'restarts) it is marked as failed after a few minutes and you can generate again.';
