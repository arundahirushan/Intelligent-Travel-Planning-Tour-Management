# Design System

## 1. Overall Design Direction

Create a modern, premium, editorial-style web interface that feels clean, spacious, confident, and image-led.

The visual language should combine:

- Minimal layouts with strong visual hierarchy
- Large, cinematic imagery
- Dark hero sections contrasted with bright content sections
- Cool blue accents on a soft off-white canvas
- Rounded cards and buttons with restrained shadows
- Strong uppercase headings with generous letter spacing
- Simple, readable body typography
- Subtle hover motion rather than excessive animation

The design should feel polished and contemporary without becoming visually crowded.

---

## 2. Color System

### Primary Colors

| Token | Hex | Usage |
|---|---|---|
| Primary Blue | `#0284C7` | Main CTA buttons, links, active states, icons, highlights |
| Primary Blue Dark | `#0369A1` | Hover states and darker interactive states |
| Bright Accent Blue | `#38BDF8` | Small highlights, labels, icon accents on dark backgrounds |
| Dark Text | `#0F172A` | Main headings, navigation, important text |
| Deep Hero Background | `#0A1120` | Hero backgrounds, dark visual sections |
| Canvas | `#F8FAFC` | Main page background |
| White | `#FFFFFF` | Cards, section backgrounds, contrast surfaces |

### Supporting Colors

| Token | Hex | Usage |
|---|---|---|
| Light Blue Surface | `#E0F2FE` | Soft cards, category backgrounds, CTA panels |
| Very Light Blue Surface | `#F0F9FF` | Featured content backgrounds |
| Light Border | `#BAE6FD` | Card borders, dividers, navigation borders |
| Neutral Border | `#CBD5E1` | Footer dividers and subtle neutral separators |
| Secondary Text | `#475569` | Paragraphs, descriptions, supporting information |
| Muted Text | `#64748B` | Secondary metadata and low-priority text |
| Soft Neutral | `#F1F5F9` | Footer/background variation |

### Color Rules

- Use blue as the main interaction color, not as the dominant background color throughout the page.
- Keep the majority of content sections light and spacious.
- Use very dark backgrounds only for strong visual moments such as hero sections or featured banners.
- Use light blue surfaces to create grouping without heavy visual borders.
- Maintain high text contrast and avoid using accent blue for large blocks of body text.

---

## 3. Typography

### Font Families

Use two complementary sans-serif fonts:

- **Headings / UI:** `Manrope`
- **Body / Paragraphs:** `Plus Jakarta Sans`

### Heading Style

Headings should be:

- Bold or semi-bold
- Tight in line-height
- Slightly compact in letter spacing
- Frequently uppercase for major section headings
- Visually dominant without excessive decoration

Suggested weights:

- 600 — medium emphasis
- 700 — main headings
- 800 — hero/display headings

### Body Style

Body text should be:

- Regular or medium weight
- Comfortable line-height
- Neutral in color
- Easy to scan

Recommended body characteristics:

- `14px–18px` for normal content
- `1.5–1.8` line-height
- Maximum readable line width around `600–700px`

### Labels / Eyebrows

Small section labels should use:

- Uppercase text
- Bold weight
- Small font size around `11px–13px`
- Increased letter spacing
- Primary blue or accent blue

---

## 4. Layout System

### Page Width

Use a centered content container with a comfortable maximum width.

Recommended:

- Max width: approximately `1200–1280px`
- Horizontal padding: `24px` on smaller screens
- Horizontal padding: `48px` or more on large screens

### Spacing

Use a generous spacing scale to create a premium editorial feel.

Suggested spacing:

- Small: `8px`
- Compact: `12px`
- Medium: `16px`
- Large: `24px`
- Section: `48px`
- Large section: `80px–96px`

Avoid tightly packing unrelated elements together.

### Section Rhythm

Most major sections should have approximately `80px–96px` vertical padding on desktop and reduced spacing on mobile.

Use whitespace as a primary visual tool instead of adding decorative elements.

---

## 5. Navigation

### Header Style

Use a clean fixed/sticky navigation bar.

Characteristics:

- Height around `72–80px`
- Soft off-white background
- Slight transparency is acceptable
- Subtle backdrop blur
- Thin bottom border
- Centered content container
- Simple horizontal navigation links
- One prominent primary CTA

### Navigation Links

Navigation links should be:

- Small to medium size
- Semi-bold
- Dark neutral by default
- Blue on hover/active state
- Spaced generously rather than tightly grouped

### CTA

Primary navigation CTA:

- Blue background
- White text
- Rounded pill shape
- Uppercase small text
- Bold typography
- Subtle shadow
- Darker blue hover state

---

## 6. Hero Section

The hero should be visually powerful but structurally simple.

### Visual Treatment

- Full-width section
- Approximately `90vh` on desktop when appropriate
- Large background image or visual
- Dark cinematic overlay/gradient for text readability
- Centered or strongly focused content
- Minimal number of actions

### Hero Text

Use:

- Small eyebrow label
- Large display heading
- Short supporting paragraph
- One primary CTA, with a secondary CTA only when necessary

### Hero Image Rules

- Prefer high-quality photography or strong visual assets
- Use `object-fit: cover`
- Maintain a clear focal point
- Apply a dark gradient rather than a heavy opaque overlay
- Avoid placing important visual details directly behind text

---

## 7. Section Headers

Major sections should follow a consistent pattern:

1. Small uppercase eyebrow label
2. Large section heading
3. Short supporting sentence

Recommended alignment:

- Center aligned for showcase/grid sections
- Left aligned for editorial or split-content sections

Keep section headers visually compact so the actual content remains the focus.

---

## 8. Image Cards

Use image-first cards for visually important content.

### Card Structure

- Large image covering the full card
- Rounded corners
- Hidden overflow
- Content positioned near the bottom
- Dark gradient from bottom upward
- White text over the image
- Small blue accent for action text or metadata

### Shape

Recommended:

- Border radius: `16px–24px`
- Consistent card aspect ratios
- Vertical cards can use approximately `4:5`

### Hover Effect

On hover:

- Image scales slightly, around `1.03–1.06`
- Overlay can become slightly darker
- Accent text changes or brightens
- Arrow/icon moves subtly to the right

Keep hover movement smooth and restrained.

---

## 9. Category / Feature Cards

Use simpler cards for grouped categories, features, or filters.

Style:

- Soft blue background
- Thin light-blue border
- Rounded corners
- Centered icon
- Short title
- Small description
- Minimal supporting text

Icon container:

- Circular or softly rounded
- White background
- Blue icon
- Small shadow or border

Hover state:

- Slightly darker light-blue background
- Small scale increase on icon
- Subtle shadow

---

## 10. Featured Content / Editorial Block

Use a large split layout for a highlighted item or important content.

Recommended structure:

- Large visual area on one side
- Text/content area on the other
- Approximately `7/12` visual and `5/12` content on desktop
- Stacked vertically on mobile

Container style:

- Very light blue background
- Thin light-blue border
- Large rounded corners (`24px–32px`)
- Soft shadow
- Generous internal padding

This component should feel like a premium editorial feature rather than a standard card.

---

## 11. CTA / Conversion Section

Use a visually distinct CTA section near the end of a page.

Style:

- Soft blue background
- Rounded large container
- Centered content
- Strong heading
- Short supporting paragraph
- One clear primary CTA
- Minimal decorative elements

The CTA should stand out through spacing, contrast, and scale rather than excessive graphics.

---

## 12. Buttons

### Primary Button

- Background: `#0284C7`
- Text: white
- Hover: `#0369A1`
- Rounded pill or large radius
- Medium horizontal padding
- Bold uppercase/small-label typography
- Optional arrow/icon

### Secondary Button

Use when needed:

- Transparent or white background
- Dark text
- Light border
- Same radius as the primary button

### Button Behavior

Use subtle transitions for:

- Background color
- Text/icon movement
- Shadow
- Small scale changes

Avoid large bounce or exaggerated motion.

---

## 13. Forms / Inputs

Inputs should be clean and low-noise.

Style:

- White background
- Light blue or neutral border
- Rounded corners
- Comfortable height
- Clear placeholder text
- Blue focus state

Focus state should use:

- Primary blue border
- Small blue focus ring

Forms should visually match the button system.

---

## 14. Footer

The footer should be minimal and structured.

Recommended layout:

- Soft neutral background such as `#F1F5F9`
- Top border/divider
- Logo or brand area
- Short description
- Useful navigation links
- Optional newsletter/form area
- Bottom legal/social bar

Footer typography should be smaller than the main page while maintaining strong readability.

---

## 15. Borders, Shadows & Radius

### Border Radius

Use rounded geometry consistently:

- Small controls: `8px–12px`
- Cards: `16px–24px`
- Large feature/CTA containers: `24px–32px`
- Primary buttons: pill / `9999px`

### Shadows

Use soft, subtle shadows only.

Preferred style:

- Low opacity
- Large blur
- Small vertical offset

Avoid dark or heavy drop shadows.

### Borders

Borders should be thin and understated.

Preferred colors:

- `#BAE6FD` for blue-tinted borders
- `#CBD5E1` for neutral separators

---

## 16. Iconography

Use a consistent icon set such as Material Symbols or another clean outline icon library.

Icon style:

- Simple
- Modern
- Mostly outlined
- Consistent stroke/weight
- Blue for emphasis

Do not mix many unrelated icon styles.

---

## 17. Motion & Interaction

Animations should enhance usability, not dominate the design.

Recommended transitions:

- `200ms–300ms` for buttons and controls
- `500ms–700ms` for large image hover effects

Good motion examples:

- Slight image zoom
- Arrow moving a few pixels
- Icon scaling slightly
- Color transition on hover
- Soft shadow appearing on hover

Avoid:

- Excessive parallax
- Large rotations
- Constant animated elements
- Distracting loading effects

---

## 18. Responsive Design

### Desktop

- Wide centered content
- Multi-column grids
- Large hero typography
- Spacious section layouts

### Tablet

- Reduce horizontal spacing
- Reduce number of columns where necessary
- Maintain large touch-friendly controls

### Mobile

- Single-column layouts for major content
- Stack split sections vertically
- Reduce heading size while keeping strong hierarchy
- Collapse navigation into a mobile menu
- Keep buttons easy to tap
- Maintain generous vertical spacing
- Preserve image quality and visual impact

The design must remain visually balanced rather than simply shrinking desktop elements.

---

## 19. Accessibility & Usability

- Maintain strong text/background contrast
- Use meaningful hover and focus states
- Keep interactive elements clearly identifiable
- Provide visible keyboard focus states
- Use semantic HTML
- Use descriptive image alt text
- Do not rely on color alone to communicate important states
- Keep touch targets large enough for mobile interaction

---

## 20. Design Principles

### Do

- Prioritize whitespace
- Use strong photography or visual media
- Keep the color palette controlled
- Make typography a major part of the visual identity
- Use consistent rounded surfaces
- Create clear visual hierarchy
- Keep interactions subtle
- Make CTAs obvious without making the page feel sales-heavy

### Avoid

- Excessive gradients
- Too many accent colors
- Crowded layouts
- Heavy shadows
- Excessive rounded shapes everywhere
- Too many different font families
- Excessive animations
- Large blocks of low-contrast text
- Overloading sections with too many cards or controls

---

## 21. Recommended Design Tokens

```css
:root {
  --color-primary: #0284C7;
  --color-primary-dark: #0369A1;
  --color-accent: #38BDF8;
  --color-text: #0F172A;
  --color-text-secondary: #475569;
  --color-canvas: #F8FAFC;
  --color-white: #FFFFFF;
  --color-surface-blue: #E0F2FE;
  --color-surface-light: #F0F9FF;
  --color-surface-neutral: #F1F5F9;
  --color-border-blue: #BAE6FD;
  --color-border-neutral: #CBD5E1;
  --color-hero: #0A1120;

  --radius-sm: 10px;
  --radius-md: 16px;
  --radius-lg: 24px;
  --radius-xl: 32px;
  --radius-pill: 9999px;

  --shadow-soft: 0 8px 30px rgba(15, 23, 42, 0.08);

  --font-heading: "Manrope", sans-serif;
  --font-body: "Plus Jakarta Sans", sans-serif;
}
```

---

## 22. Visual Summary

The final interface should feel:

**Modern + Editorial + Premium + Minimal + Image-led + Spacious + Blue-accented**

The design should communicate quality through composition, typography, photography, spacing, and subtle interactions rather than through excessive decoration.
