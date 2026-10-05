# Poulet — ASP.NET Core + React

A parallel implementation of the existing Flask application. The original source and `poulet.db` are untouched. This app uses its own `src/Poulet.Api/poulet-modern.db`, initially containing only the admin account and SOUK chamber. The local database may also contain clearly named demonstration records created during browser verification.

## Run locally

Prerequisites: .NET 10 SDK and Node.js 22.12+ (Node 24 recommended).

From the repository root:

```powershell
dotnet restore Poulet.slnx --configfile NuGet.Config
dotnet run --project src/Poulet.Api
```

In a second terminal:

```powershell
cd web
npm ci
npm run dev
```

Open http://127.0.0.1:5173. Development login: **admin / admin123**. Vite proxies `/api` to http://localhost:5080, so the browser uses same-origin cookies and CSRF protection.

Windows shortcut: `./start.ps1` starts both processes; Ctrl+C stops the process trees it created. `./publish.ps1` creates a combined API + React publish directory at `artifacts/app` without deploying it. Stop the Vite development server before publishing on Windows, because `npm ci` replaces native dependency files that a running server can lock.

## Assistant d’analyse

Les administrateurs disposent d’un bouton **Assistant** qui peut répondre en français aux questions sur les achats, ventes, stock, alimentation et bénéfice net estimé. L’assistant ne reçoit jamais un accès direct à SQLite : il ne peut appeler que des résumés d’analyse prévus par l’API.

Configurez une clé OpenAI uniquement sur le serveur, puis redémarrez l’API :

```powershell
$env:OpenAI__ApiKey = "votre-cle-api"
# Facultatif : $env:OpenAI__Model = "gpt-6-astra"
```

Ne placez jamais cette clé dans `web/`, dans Git, ou dans le navigateur. Sans clé, le bouton reste visible pour l’administrateur mais indique clairement que l’assistant doit être configuré.

## Architecture

```text
React/TypeScript ── HTTP ── ASP.NET Core API
                             │ ISender
                             ▼
                       Application
                  Commands / Queries / Handlers
                  Transaction pipeline / Interfaces
                             │
                             ▼
                           Domain
                   Entities / Business rules

Infrastructure implements Application interfaces:
EF Core + SQLite, repositories, unit of work, password hashing.
The API is the dependency injection composition root.
```

Domain has no framework or database dependency. Application references Domain and MediatR. Infrastructure references Application. API references Application and Infrastructure. Request handlers implement the business workflows; ASP.NET Core controllers in `src/Poulet.Api/Controllers` bind HTTP requests, enforce authorization and dispatch requests through `ISender`. `Program.cs` configures dependency injection and middleware, then registers controllers with `MapControllers()`. Queries and commands share one database (CQRS does not require separate databases).

Controllers: Auth, Health, Dashboard, Suppliers, Clients, Chambers, Purchases, Feed, Sales, Users, Reports and Records. Their routes retain the existing `/api/...` URLs, so React continues to use the same API contract. The Records controller handles the existing generic delete endpoint.

The React frontend is organized into separate pages, layouts, reusable components, hooks, services, types and translations. `web/src/App.tsx` only composes the application and its authentication guard. See [the frontend guide](web/README.md) for the folder structure, page registration and form configuration.

MediatR is explicitly pinned to 12.5.0; upgrade deliberately after reviewing the newer package's licensing requirements. EF Core is 10.0.12, with SQLitePCLRaw 3.0.5 explicitly selected to avoid the older vulnerable native SQLite dependency.

## Features

- Cookie authentication, admin/grossiste roles, password hashing/reset, logout, login rate limiting, CSRF protection and session invalidation after password changes.
- Supplier/client/chamber CRUD, validated phones, used-record deletion guards, chamber stock/history/profitability.
- Chicken purchases, edits, external client allocations, full quantity distribution, capacity validation aggregated across allocation lines, normal/BIBI stock separation, departure/actual weights.
- Feed costs by chamber.
- Wholesale/detail chicken sales and Monday lots. Monday lots reserve stock, prioritizing SOUK, then other chambers; details consume the reserved kg/pieces rather than deducting stock again.
- Monday numbered pieces, client-specific kg pricing, payment toggles, combined search/payment filters and printable receipts.
- Dashboard, SOUK overview, date/type/search filters, financial reports and revenue chart.
- French, English and Arabic interface, RTL layout, responsive screens and light/dark themes. API validation messages currently use English.

Chicken stock quantities are kg. A capacity of zero means unlimited. Monday lot chicken type is fixed for all its lines to preserve inventory consistency.

## Validation

```powershell
dotnet test Poulet.slnx
cd web
npm test
npm run build
```

Tests use isolated SQLite databases and exercise real EF Core persistence, MediatR transactions, stock validation, type isolation, Monday kg/piece/payment rules, corrections/deletions, proportional actual-weight costs, reports, login/logout, CSRF and grossiste authorization. Browser verification covers purchase allocation, Monday sale/payment and receipts.

Frontend regression tests use the Node.js 24 test runner and cover form endpoints, allocation isolation, Monday lot/line requests, rejection of unsupported forms, and role/client fields. The frontend refactor was also checked in the browser across every business page, add/edit dialogs, purchase/chamber details, Monday lot details and receipts, report rendering and delete confirmation cancellation.

## Financial calculation

Sales store a purchase-cost estimate at creation time using the weighted average of chamber purchases (actual weight costs prorated by allocation). Monday COGS is recognized in proportion to kg actually sold; unconsumed reserved kg are not reported as sales. Reports include wholesale and retail chicken sales and the quantities actually sold from Monday lots. Net estimate subtracts COGS, feed and crate costs. This is an operational estimate rather than formal accounting; historical purchase edits do not restate cost snapshots, and backdated entries do not produce FIFO valuation.

## Deployment and boundaries

Run `npm run build`, copy the contents of `web/dist` into `src/Poulet.Api/wwwroot`, then `dotnet publish src/Poulet.Api -c Release`. The API serves the React build at the same origin. Configure HTTPS and set `Seed__AdminPassword` for initial production provisioning. Persist both the SQLite database and `App_Data/keys`. For an existing database, reset the development password through the user management screen before real use.

Schema creation currently uses `EnsureCreated` for the separate initial database. Introduce versioned EF migrations before evolving a populated production database. Reads currently load the relevant tables into memory; pagination/server-side filtering should precede large datasets. The SQLite transaction pipeline serializes writes within one API process; use a database with appropriate transaction isolation before deploying multiple API instances.

Flask data import, standalone live-to-Madbouh conversions (only partially present in Flask), stock returns for unfinished Monday lots, exports, backup UI, audit trail and comprehensive supplier/customer credit accounting are outside this implementation. Madbouh remains a sale-mode label as in the existing app. Fonts have local system fallbacks if Google Fonts is unavailable.
