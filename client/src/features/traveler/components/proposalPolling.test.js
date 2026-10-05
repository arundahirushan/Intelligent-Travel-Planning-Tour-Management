import test from 'node:test';
import assert from 'node:assert/strict';
import {
  MAX_POLLS,
  hasPollsLeft,
  describeGenerating,
  LEAVE_PAGE_NOTE,
} from './proposalPolling.js';

test('auto-polling shows a spinner while the poll budget lasts', () => {
  assert.equal(hasPollsLeft(0), true);
  assert.equal(hasPollsLeft(MAX_POLLS - 1), true);
  const view = describeGenerating({ pollsExhausted: false, checking: false });
  assert.equal(view.showSpinner, true);
  assert.match(view.title, /in progress/i);
});

test('when polling stops there is no spinner and the message says it is still processing', () => {
  assert.equal(hasPollsLeft(MAX_POLLS), false);
  const view = describeGenerating({ pollsExhausted: true, checking: false });
  assert.equal(view.showSpinner, false);
  assert.match(view.title, /still processing/i);
  assert.match(view.detail, /check status/i);
});

test('a manual Check Status shows activity again, even after polling stopped', () => {
  const view = describeGenerating({ pollsExhausted: true, checking: true });
  assert.equal(view.showSpinner, true);
  assert.match(view.title, /checking/i);
});

test('leave-page note says the server keeps working and what happens on interruption', () => {
  assert.match(LEAVE_PAGE_NOTE, /keeps running on the server/i);
  assert.match(LEAVE_PAGE_NOTE, /marked as failed/i);
  assert.doesNotMatch(LEAVE_PAGE_NOTE, /will be saved when complete\./i);
});
