# DESIGN.md — Dashboard Design Patterns

**Companion to `docs/FRONTEND_GUIDELINES.md`.** That file is the source of
truth for colors, fonts, and existing components — read it first. This
file adds dashboard-specific patterns (layouts, cards, tables, badges)
extracted from Stitch-generated reference screens, and extends the
design system with a few tokens dashboards need that didn't exist yet.

**IMPORTANT:** The Stitch files these patterns came from used a
different, unrelated color palette (teal-blue Material tokens). Do NOT
use their colors. Every pattern below has already been re-mapped onto
the colors already established and in use on the live site
(`FRONTEND_GUIDELINES.md`). If you go back to the original Stitch HTML
for any reason, ignore its color classes entirely.

---

## New Tokens (add these to `client/src/index.css`'s `@theme` block —
## these are additions, not replacements, for anything already there)

### Status Colors
Every entity in this system has a status, and none of them have colors
defined yet. Add these semantic tokens:

| Token | Suggested hex | Used for |
|---|---|---|
| `status-success` | `#16A34A` (green) | Active, Confirmed |
| `status-warning` | `#D97706` (amber) | PendingApproval, Held |
| `status-danger` | `#DC2626` (red) | Rejected |
| `status-neutral` | `#64748B` (gray) | Suspended, Inactive, Cancelled, Terminated |

### Typography Scale
The site currently only has two font-family tokens (`font-heading`,
`font-body`) with no named sizes. Add this named scale, built from those
same two families:

| Token | Font | Size | Weight | Use |
|---|---|---|---|---|
| `display` | heading | 3rem | 800 | Rare, big hero numbers only |
| `headline-lg` | heading | 2.25rem | 700 | Page titles |
| `headline-md` | heading | 1.5rem | 600 | Section titles |
| `headline-sm` | heading | 1.125rem | 600 | Card titles |
| `body-lg` | body | 1.125rem | 400 | Intro/lead paragraphs |
| `body-md` | body | 0.9375rem | 400 | Default body text |
| `body-sm` | body | 0.8125rem | 400 | Secondary/metadata text |
| `label-button` | heading | 0.875rem | 600 | Button labels |
| `label-badge` | heading | 0.75rem | 700 | Status badges |
| `label-uppercase` | heading | 0.6875rem | 700, wide tracking | Small eyebrow/section labels |

### Spacing Scale
Add a named scale instead of relying on raw Tailwind spacing everywhere:
`space-xs` (0.25rem), `space-sm` (0.5rem), `space-md` (1rem), `space-lg`
(1.5rem), `space-xl` (2.5rem), `gutter` (1.5rem), `margin` (2rem).

### Icons
Standardize on **Material Symbols Outlined** (Google Fonts) for every
icon in the app, including the pages already built. Don't mix in a
different icon library for new dashboard work.

### Radius & Shadow
**No changes** — keep using the already-established `sm/md/lg/xl/pill`
radius scale and the single `shadow-soft` token from
`FRONTEND_GUIDELINES.md`. Don't introduce a second radius system.

---

## Layout Patterns

### Dashboard Shell (use for every role's dashboard)
- Top Navbar: Logo + app name ("EasyPlanner") on the left (links to `/`). A horizontal list of nav links with icon + label (active item gets `bg-primary text-white`, inactive items are neutral with a hover state).
- Top-Right: A small account/organization switcher button, a notification bell with an unread-count badge, and a clickable area showing the current user's name + role + avatar circle which opens a small dropdown menu on click with "Profile" and "Sign Out" (in danger color).
- Mobile: The horizontal nav items collapse into a simple hamburger menu toggle.
- Pinned to the bottom-right corner of the viewport: a small floating support/help widget that expands to show generic contact info.
- Main content area below the header, with generous padding, using the spacing scale above.

### Page Header Pattern (top of most dashboard pages)
- A small uppercase "eyebrow" label with a colored dot, using
  `label-uppercase`.
- A `headline-lg` page title.
- A short `body-md` description sentence beneath it.
- A primary pill-shaped action button aligned to the right on desktop,
  stacking below on mobile (e.g. "+ Add New Hotel").

### Summary Metric Card Row (dashboards with KPIs — e.g. "My Hotels")
A row of 4 cards, each containing: a small uppercase label + icon at the
top, a large `headline-lg` number (optionally with a smaller "/ Total"
suffix), a small status pill badge to the side, and a thin horizontal
progress bar underneath representing a percentage. Use this pattern
anywhere a dashboard needs at-a-glance stats — not just hotels.

### Filterable Card/List Pattern (e.g. "My Hotels" grid, any "My X" list)
- A search input, plus one or more filter dropdowns (e.g. status,
  region/category), plus a "Reset Filters" link/button, all in one row
  above the results.
- A small "Showing X–Y of Z" count, updating live as filters change.
- The results themselves as a responsive card grid (not a plain table)
  when the items are visually rich (have an image, a status, a few key
  facts) — reserve plain data tables for dense, less visual data (like a
  bookings list, see below).
- A distinct, friendly empty-state message + icon shown when filters
  produce zero results, separate from the "no items exist yet" empty
  state.
- IMPORTANT: implement all filtering as real React state
  (`useState`/`useMemo`), not the vanilla DOM `querySelectorAll`/
  `addEventListener` approach the original Stitch HTML used — that
  approach doesn't apply to a React app and should not be carried over.

### Detail Page Pattern (e.g. "Hotel Detail")
- A "Back to [list]" link at the top left.
- A cluster of secondary action buttons at the top right (e.g. "Edit,"
  "Delete") — destructive actions use the danger color, others stay
  neutral/primary.
- A "master info" card near the top: an image on one side, key facts and
  a description on the other, in a responsive grid that stacks on
  mobile.
- A second section below for related sub-items (e.g. Room Types), shown
  as its own list/grid with its own "Add [item]" button — this section
  manages the sub-items, while the master card above manages the parent
  entity itself.

### Simple Data Table Pattern (e.g. "Bookings" — read-only, dense data)
- A page header (per the Page Header Pattern above), optionally with a
  decorative but subtle background accent behind it — keep this purely
  visual, no interactive function.
- A plain table below: clear column headers, a status pill badge in its
  own column (colored per the Status Colors above — never rely on color
  alone, keep the status word visible in the badge too), pagination
  controls beneath.
- No row-level action buttons needed for a purely read-only oversight
  view like this.

### "Danger Zone" Pattern (any destructive account/entity action)
- Visually separated from the rest of the page (a distinct card, tinted
  with the danger color at low opacity, or a clear heading in the danger
  color).
- A short, plain-language explanation of what the action does and that
  it's irreversible.
- If the action is currently blocked by a business rule (e.g. "can't
  delete while you still have active hotels"), show the button in a
  visibly disabled state AND a clear explanation of exactly why, with a
  helper link straight to where the user can resolve it (e.g. "Review My
  Hotels (6 active)"). Don't just silently disable the button with no
  explanation.

---

## What NOT to carry over from the Stitch files

- Their color palette entirely (see note at the top).
- Their specific border-radius values (`1rem`/`2rem`/`3rem`) — use the
  already-established `sm/md/lg/xl/pill` scale instead.
- Any inline `<script>` DOM manipulation — this is a React app, all
  interactivity is React state/hooks.
- Placeholder business content (hotel names like "Amanwella & Heritage,"
  fake phone numbers, etc.) — real content comes from the API.
- Hardcoded image URLs pointing to Google-hosted Stitch assets — these
  won't work outside the generated preview.

---

## For AI Coding Assistants

Before building any dashboard page: read this file AND
`docs/FRONTEND_GUIDELINES.md` together. Check `client/src/index.css` to
confirm which of the new tokens above have already been added by earlier
work (so you don't redefine them) versus which you need to add now. If a
pattern here doesn't cleanly fit what you're building, ask before
inventing a new one from scratch — check first whether an earlier
dashboard already solved the same problem.
