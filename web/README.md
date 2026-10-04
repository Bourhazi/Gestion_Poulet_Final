# React frontend

Run from `web` with Node.js 24 or later:

```powershell
npm ci
npm run dev
npm test
npm run build
```

The development UI runs at http://127.0.0.1:5173 and proxies `/api` to the ASP.NET Core API on port 5080. See `../README.md` for the backend startup commands.

```text
src/
  App.tsx                 Application composition and authentication guard
  app/                    Shared context and page selection
  pages/                  Dashboard, suppliers, clients, chambers, purchases, sales, etc.
  layouts/                Sidebar, header, workspace and filter toolbar
  components/             Reusable controls, tables and dialogs
    ui/                   Table, Cards and accessible Dialog
  features/
    forms/                Form configuration, editor and API serialization
    details/              Purchase, chamber and receipt presentation
  hooks/                  Session, preferences, reports and application state
  services/               HTTP client and CSRF handling
  types/                  API models and form types
  locales/                French, English and Arabic translations
  utils/                  Date, number and sales formatting
  styles.css              Shared application styles
tests/                    Form and request regression tests
```

`App.tsx` composes the provider, login screen, layout, current page and dialogs. Each business screen lives in `pages/`. Suppliers and clients share presentation components with explicit variants to avoid duplicating their table logic.

`app/PageRouter.tsx` selects the current screen using application state. Navigation preserves the existing behavior; it does not introduce URL routes. API access lives in `services/api.ts`. Page components consume the authenticated workspace; hooks manage state and requests. Pure form configuration and serialization functions can be tested without a browser.

To add a screen, create its component in `pages/`, register it in `app/PageRouter.tsx`, and add its navigation entry in `hooks/useAppController.ts`. Add labels in `locales/i18n.ts`. For an editable record, define its form in `features/forms/createEditor.ts` and any request conversion in `serializeEditor.ts`.
