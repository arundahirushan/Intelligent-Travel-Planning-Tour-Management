# Frontend Guidelines — Intelligent Travel Planning & Tour Management

**Read this file before writing any frontend code in this project.** Each
team member works on their own machine with their own AI coding session
that has no memory of past conversations or other members' work — this
file is the single source of truth that keeps everyone's frontend
consistent.

If anything you're about to do conflicts with a rule here, or a
requirement is ambiguous, **stop and ask the project owner rather than
guessing.** Also: **this file describes the state of the project as of
when it was written — always verify against the actual code in the repo
first**, since the codebase moves faster than this document. If something
here doesn't match what you find in `client/src/`, trust the actual code
and flag the mismatch rather than assuming this file is right.

---

## Design System

The design is Tailwind CSS **v4**, and — important, differs from most
tutorials — theme tokens are **NOT** in `tailwind.config.js` (that file
doesn't exist in this project). They live directly inside
`client/src/index.css`, inside an `@theme` block. Check that file for the
authoritative, current list before assuming anything below is still
accurate.

### Colors
| Token | Hex | Utility class examples |
|---|---|---|
| `primary` | `#0284C7` | `bg-primary`, `text-primary`, `border-primary` |
| `primary-dark` | `#0369A1` | `hover:bg-primary-dark` |
| `accent` | `#38BDF8` | `text-accent` |
| `text` | `#0F172A` | `text-text` |
| `text-secondary` | `#475569` | `text-text-secondary` |
| `canvas` | `#F8FAFC` | `bg-canvas` (main page background) |
| `surface-blue` | `#E0F2FE` | `bg-surface-blue` |
| `surface-light` | `#F0F9FF` | `bg-surface-light` |
| `surface-neutral` | `#F1F5F9` | `bg-surface-neutral` |
| `border-blue` | `#BAE6FD` | `border-border-blue` |
| `border-neutral` | `#CBD5E1` | `border-border-neutral` |
| `hero` | `#0A1120` | `bg-hero`, `from-hero` (dark hero sections) |

Do not introduce new raw hex colors anywhere in the app. If a screen
needs a color not listed here (e.g. status colors for badges), add it to
`index.css`'s `@theme` block first, using a clear semantic name, then use
that token — don't hardcode.

### Border Radius
`sm` (10px), `md` (16px), `lg` (24px), `xl` (32px), `pill` (9999px) —
used as `rounded-sm`, `rounded-md`, etc. Buttons typically use `pill`;
cards typically use `lg` or `xl`.

### Shadow
One token: `shadow-soft` → `0 8px 30px rgba(15, 23, 42, 0.08)`. This is a
deliberately soft, low-opacity shadow — never use a heavy/dark
box-shadow anywhere in this app.

### Typography
Two font families: `font-heading` (Manrope — for headings, labels,
buttons, uppercase small text) and `font-body` (Plus Jakarta Sans — for
paragraphs and general body text). Don't introduce a third font family.

---

## Existing Shared Components (`client/src/components/`)

These already exist — **use them, don't rebuild your own versions**:

- **`Button.jsx`** — supports `primary` and `secondary` variants.
- **`Input.jsx`** — text/password input with label, validation error message support, and focus styling.
- **`ErrorBanner.jsx`** — inline error alert (icon + message).
- **`LoadingSpinner.jsx`** — supports `sm`/`md`/`lg` sizes.
- **`FeatureCard.jsx`** — icon + uppercase title + description, used on the landing page.
- **`ImageCard.jsx`** — image background with gradient overlay and hover action indicator, used for destination cards.
- **`DashboardLayout.jsx`** — one-column responsive layout with a top navbar (including profile dropdown and mobile hamburger menu), main content area, and a floating help widget. Accepts navItems.
- **`StatusBadge.jsx`** — maps a string status to a semantic color pill badge.
- **`ProgressBar.jsx`** — simple rounded-pill horizontal progress bar with optional label.
- **`SummaryMetricCard.jsx`** — dashboard summary block with icon, label, large value, optional badge, and progress bar.
- **`SearchFilterBar.jsx`** — responsive bar above lists with a search input, custom filters (passed as children), and result counts.
- **`DataTable.jsx`** — standard list view table with pagination, empty states, and loading states.
- **`Modal.jsx`** — base modal with backdrop, accessible close button, and sizes.
- **`RoomModal.jsx`** — shared room form modal supporting single-hotel or multi-hotel context.
- **`ConfirmDialog.jsx`** — small modal for confirming destructive or critical actions.
- **`EmptyState.jsx`** — centered icon + title + description for empty lists or zero search results.

If a dashboard needs a data table, modal, confirm dialog, status badge,
or search/filter bar, **check first whether one has already been built**
by an earlier component (see "Build Order" below) before creating a new
one.

---

## Auth State — `context/AuthContext.jsx`

The `useAuth()` hook exposes exactly this shape — don't assume a
different shape, and don't add new fields to it without updating this
document:

```
{
  user,              // { id, fullName, email, role } or null
  token,             // JWT string or null
  loading,           // boolean, true while auth state is initializing
  isAuthenticated,   // boolean, derived from !!token
  login(email, password),   // returns { success, role?, error? }
  logout()
}
```

**Token handling note:** this project deliberately uses a manual
`setAuthToken(token)` call (in `services/api.js`) inside `AuthContext`'s
login/logout, rather than an axios request interceptor. This is an
intentional, confirmed choice — do not "improve" it into an interceptor
without checking with the project owner first, even if an interceptor
seems like the more standard pattern.

Token is stored in memory (React state) only — **never localStorage.**

## Protected Routes — `routes/ProtectedRoute.jsx`

Usage pattern (already established, follow it exactly):
```jsx
<Route element={<ProtectedRoute allowedRoles={['HotelOwner']} />}>
  <Route path="/hotel-owner" element={<YourDashboardPage />} />
</Route>
```
- Redirects to `/login` if not authenticated.
- Shows a "Not Authorized" screen if authenticated but role doesn't match.
- Renders `<Outlet />` (nested routes) if authorized.

## API Service Layer — `services/api.js`

- `apiClient` — the default-exported, configured axios instance. Add new
  API functions here (or in a feature-specific service file that imports
  `apiClient`) — never call `axios` directly from inside a page or
  component.
- Before building any form or table, **check the actual backend DTOs**
  in `server/TourManagement.Api/Dtos/` for the real field names and
  shapes — don't guess or invent them.

---

## Dashboard Ownership & Build Order

Each of the 4 core components has its own dashboard owner. This is
deliberately **not** a 1-to-1 split with the backend components — read
carefully:

- **M1 (Trip)** — owns the entire Traveler-facing experience: trip creation/list/detail, AND all three traveler-facing booking flows (browse/book hotels, browse/book vehicles, order supplies), since a traveler manages all of this from inside one trip. **Built last.**
- **M2 (Accommodation)** — **[COMPLETED]** owns ONLY the Hotel Owner's own management dashboard (manage their hotels/rooms, view their bookings). Built first to establish the shared UI component kit.
- **M3 (Transport)** — owns ONLY the Transport Provider's own management dashboard. Built after M2. Reuses M2's UI components.
- **M4 (Supplier)** — owns ONLY the Supplier's own management dashboard (including contract/contract-request views). Built after M2. Reuses M2's UI components.

**Why M2 goes first:** rather than guessing upfront what "shared UI" every dashboard will need, M2 was built as the pilot. The generic, reusable pieces that came out of building it for real (data table with pagination, modal, confirm dialog, status badge component, search/filter bar, dashboard layout with sidebar navigation, summary metric cards) are now the shared kit. **If you are building M3, M4, or M1: check `client/src/components/` for what M2 already produced before building your own version of a table, modal, badge, etc.**

Each dashboard lives in its own feature folder
(`client/src/features/<name>/`) with its own `components/`, `pages/`,
and `hooks/` as needed — this keeps merge conflicts low between members
working at the same time.

---

## Code Style

Same spirit as the backend guidelines (`docs/CODING_GUIDELINES.md`):

- Simple, readable React code. Functional components and hooks only.
- No unnecessary abstraction — don't build a generic system for
  something that's only used once.
- Comment the *why*, not the *what*, where a decision isn't obvious.
- Reusable components over duplicated UI — if you're copy-pasting a
  table or card structure a second time, that's a signal to extract it.
- Consistent naming: PascalCase for components/files, camelCase for
  functions/variables.
- A student with basic React knowledge should be able to open any file
  and understand it without external explanation.

---

## Accessibility & Responsiveness (applies to every page)

- Proper `<label>` association on every form input.
- Visible keyboard focus states (don't remove browser focus outlines
  without replacing them with something visible).
- Sufficient color contrast — this palette's combinations should already
  satisfy this, but double-check rather than assume, especially for any
  new color you add.
- Every page/section works on desktop, tablet, and mobile — collapse to
  single-column layouts on small screens, keep touch targets large
  enough to tap comfortably.
- Every data-fetching view has distinct loading, empty, and error
  states — never leave a screen blank while something is happening.

---

## For AI Coding Assistants (Antigravity, etc.)

Before writing any new frontend code in this project:
1. Read this file in full.
2. Open `client/src/index.css` to confirm the current, actual `@theme`
   tokens — don't trust a value here if the real file disagrees.
3. Look at `client/src/components/` to see what shared components
   already exist before building a new one.
4. Check the real backend DTOs in `server/TourManagement.Api/Dtos/`
   before assuming any API request/response shape.
5. If a new requirement conflicts with something here, or is ambiguous,
   stop and ask before proceeding.
