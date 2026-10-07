# Professional Admin Dashboard Design

## Goal

Modernize the React frontend of DajajPro into a clean, professional admin dashboard. The work improves visual consistency and usability while preserving all existing business workflows, API calls, translations, and application state.

## Scope

The frontend will adopt Material UI (MUI) as its visual foundation. The ASP.NET Core API, data models, authentication flow, page routing, and existing controller hooks remain unchanged.

The rollout focuses on shared components so all current business pages benefit without rewriting their domain-specific rendering.

## Visual system

- A MUI theme provides matching light and dark palettes using navy navigation, blue primary actions, green success/status accents, neutral backgrounds, and accessible contrast.
- `CssBaseline` provides predictable browser defaults. Existing CSS remains responsible for specialized domain layouts and print styles.
- Typography, spacing, border radii, shadows, focus indicators, and breakpoints are centrally defined by the theme.

## Component architecture

`main.tsx` will wrap the application with `ThemeProvider` and `CssBaseline`, deriving its active palette mode from the existing workspace preference.

`AppLayout` will use MUI layout primitives and icons for the navigation, header, alerts, page title, and primary actions. It will retain the existing navigation, authorization, translation, logout, form-opening, and assistant behavior.

The shared `Cards`, `Table`, `Dialog`, and `PageToolbar` components will migrate to MUI primitives. Their public props and current data flow remain stable, so pages do not need behavior changes. The table retains its current client-side pagination rather than introducing a data-grid dependency.

## Responsive and accessibility behavior

- The desktop sidebar remains visible at larger widths and becomes a mobile navigation experience at small widths.
- Buttons, inputs, selects, dialogs, alerts, and table pagination retain keyboard access and accessible labels.
- Existing right-to-left and translated content continues to flow through the current localization system.
- Existing print-specific styles continue to render receipt dialogs correctly.

## Error handling and verification

No backend error contracts change. Existing error and success state is rendered with MUI alerts.

Verification includes the existing frontend test suite and TypeScript/Vite production build. Visual changes are checked in a local browser after the build succeeds.

## Out of scope

- Backend/API changes.
- Rewriting domain pages or business logic.
- Adding MUI X, server-side table pagination, routing, or a new state-management system.
