# Client Architecture & Project Structure

This document provides a complete overview of the directory hierarchy, folder roles, and files within the `client/` frontend application of the Intelligent Travel Planner & Tour Management project.

---

## 📁 Directory Tree Structure

```text
client/
├── .env.example              # Environment variables template
├── .env.local                # Local environment variable overrides (git-ignored)
├── .gitignore                # Git ignore rules for frontend assets/dependencies
├── .oxlintrc.json            # Oxlint configuration for JS/JSX linting
├── index.html                # Entry HTML template for Vite SPA
├── package-lock.json         # Dependency tree lockfile
├── package.json              # Project dependencies, metadata, and scripts
├── README.md                 # Basic client setup and run guidelines
├── vite.config.js            # Vite build and dev server configuration
├── public/                   # Static assets served directly at root URL
│   ├── 1.jpg                 # Destination / gallery card image 1
│   ├── 2.jpg                 # Destination / gallery card image 2
│   ├── 3.jpg                 # Destination / gallery card image 3
│   ├── 4.jpg                 # Destination / gallery card image 4
│   ├── 5.jpg                 # Destination / gallery card image 5
│   ├── 6.jpg                 # Destination / gallery card image 6
│   ├── favicon.svg           # Website browser tab favicon
│   ├── Hero.jpg              # Main hero banner image
│   ├── icons.svg             # SVG icon sprite definitions
│   ├── logo.png              # Public branding logo image
│   └── Nine arch Bridge.jpg  # Featured attraction image (Demodara Nine Arch Bridge)
└── src/                      # Source code root
    ├── App.css               # Main layout and application shell styling
    ├── App.jsx               # Top-level React component shell
    ├── index.css             # Base CSS styles and framework resets
    ├── main.jsx              # React DOM render entry point
    ├── assets/               # Bundled static assets
    │   ├── .gitkeep          # Directory placeholder
    │   ├── hero.png          # Hero section graphic asset
    │   ├── logo.png          # App brand logo graphic
    │   ├── react.svg         # React logo asset
    │   └── vite.svg          # Vite logo asset
    ├── components/           # Reusable atomic UI components
    │   ├── .gitkeep          # Directory placeholder
    │   ├── Button.jsx        # Customizable button component (variants, loading states)
    │   ├── ErrorBanner.jsx   # Error banner & notification banner component
    │   ├── FeatureCard.jsx   # Card component for feature highlights
    │   ├── ImageCard.jsx     # Interactive image card for destinations/tours
    │   ├── Input.jsx         # Styled form input field with error handling
    │   └── LoadingSpinner.jsx# Asynchronous spinner / loading indicator
    ├── context/              # Global state providers
    │   ├── .gitkeep          # Directory placeholder
    │   └── AuthContext.jsx   # Auth state management (JWT, user login/logout state)
    ├── features/             # Modular domain feature components (planner, bookings, etc.)
    │   └── .gitkeep          # Directory placeholder
    ├── hooks/                # Custom React hooks
    │   └── .gitkeep          # Directory placeholder
    ├── pages/                # Top-level page views (route destinations)
    │   ├── DashboardPlaceholder.jsx # User dashboard placeholder page
    │   ├── HomePage.jsx      # Main landing page (hero, features, destinations)
    │   ├── LoginPage.jsx     # User authentication / sign-in page
    │   └── RegisterPage.jsx  # User account registration page
    ├── routes/               # Application navigation and route definitions
    │   ├── AppRoutes.jsx     # React Router setup defining public & private routes
    │   └── ProtectedRoute.jsx# Auth wrapper guarding protected routes
    ├── services/             # Backend API communications
    │   └── api.js            # Axios client instance with auth headers & interceptors
    ├── styles/               # Styling sheets and design tokens
    │   └── global.css        # Global CSS variables and component utility classes
    └── utils/                # Helper utilities and shared constants
        ├── .gitkeep          # Directory placeholder
        ├── constants.js      # App constants (API routes, storage keys)
        └── formatters.js     # Data, currency, and date formatting helpers
```

---

## 🛠 Directory & File Breakdown

### 1. Root Configuration & Project Files (`client/`)
- **`package.json`**: Defines client dependencies (`react`, `react-router-dom`, `axios`, `lucide-react`, `vite`, etc.) and runnable NPM scripts (`dev`, `build`, `lint`, `preview`).
- **`vite.config.js`**: Configures Vite dev server (port settings, alias mappings, API backend proxy setup).
- **`index.html`**: Root HTML page containing `#root` mount point for Vite single-page application.
- **`.env.example` & `.env.local`**: Environment variable configurations specifying backend API endpoint URLs (`VITE_API_BASE_URL`).
- **`.oxlintrc.json`**: Linter configuration file enforcing codebase syntax rules and standard practices.

### 2. Static Public Assets (`client/public/`)
- Contains static assets served as-is by the web server without bundling. Includes destination preview imagery (`1.jpg` – `6.jpg`, `Nine arch Bridge.jpg`), hero graphics (`Hero.jpg`), brand assets (`logo.png`, `favicon.svg`), and SVG icon bundles (`icons.svg`).

### 3. Source Code (`client/src/`)

#### 🔹 Core Entry Files
- **`main.jsx`**: Bootstraps the application, mounts `App` inside `ReactDOM`, and sets up global providers (`AuthProvider`, `BrowserRouter`).
- **`App.jsx`**: Root component wrapping layout structure and router view rendering.
- **`App.css` & `index.css`**: Defines baseline stylesheet, typography setup, reset rules, and application shell styles.

#### 🔹 Components (`client/src/components/`)
Reusable, isolated UI primitives used across various pages:
- **`Button.jsx`**: Standardized button component with support for primary/secondary variants, disabled states, and loading spinners.
- **`Input.jsx`**: Text/password input field supporting labels, placeholder text, icon adornments, and error message rendering.
- **`ErrorBanner.jsx`**: Displays formatted error/warning alert banners.
- **`FeatureCard.jsx`**: Displays feature highlights with icon, title, and descriptive text.
- **`ImageCard.jsx`**: Image card displaying tour/destination thumbnail, title, rating, and action buttons.
- **`LoadingSpinner.jsx`**: Visual loading spinner used during API fetching or state transitions.

#### 🔹 Context (`client/src/context/`)
- **`AuthContext.jsx`**: Manages global user authentication state (`user`, `token`, `isAuthenticated`, `loading`), provides `login()`, `register()`, and `logout()` functions, and synchronizes auth status with browser `localStorage`.

#### 🔹 Pages (`client/src/pages/`)
Page views rendered according to current router URL:
- **`HomePage.jsx`**: Primary landing page featuring hero section, search controls, featured Sri Lankan destinations, AI travel planning promos, and footer links.
- **`LoginPage.jsx`**: User login screen supporting credential input, submit handling, error display, and redirection upon success.
- **`RegisterPage.jsx`**: Registration view allowing new users to create accounts.
- **`DashboardPlaceholder.jsx`**: Protected page accessible only after authenticating, showing welcome stats and user overview.

#### 🔹 Routes (`client/src/routes/`)
- **`AppRoutes.jsx`**: Main route definition file using `react-router-dom` to map paths (`/`, `/login`, `/register`, `/dashboard`) to page components.
- **`ProtectedRoute.jsx`**: Higher-Order Component (HOC) / wrapper that checks authentication status. If unauthenticated, it redirects the user to `/login`.

#### 🔹 Services (`client/src/services/`)
- **`api.js`**: Centralized Axios HTTP client instance. Automatically attaches `Authorization: Bearer <token>` headers to outgoing requests and handles network response errors globally.

#### 🔹 Styles & Utilities (`client/src/styles/` & `client/src/utils/`)
- **`global.css`**: Global design tokens, color palettes, and global layout classes.
- **`constants.js`**: Holds static configuration values such as API routes, local storage keys, and fixed UI options.
- **`formatters.js`**: Common formatting utility functions for dates, currency values, and strings.
