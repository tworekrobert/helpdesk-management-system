# QA Report

Local checks: 2026-07-04. Integration tests and `docker compose config` also run in GitHub Actions on every push.

## Scope Decision

The project plan separates the core Definition of Done from possible future improvements.

Core delivery remains focused on a working internal IT helpdesk and asset management application:

- seeded Employee, Technician and Admin accounts
- ticket workflow from creation through assignment, work, resolution and closure
- comments, internal comment visibility rules and attachments
- SLA calculation, audit history and dashboard metrics
- asset creation, assignment, CSV import/export and ticket linking
- Docker Compose setup, CI workflow, README, screenshots and architecture docs

Left as future improvements:

- SMTP worker for processing queued email notifications
- frontend subscription to the existing SignalR ticket update hub
- LDAP/Active Directory proof-of-concept adapter

Several future-scope items from the plan are already implemented, including attachments, CSV/PDF dashboard export, ticket tags, asset CSV import and a backend SignalR hub.

## Verification Matrix

| Check | Command | Result |
| --- | --- | --- |
| Frontend lint | `npm run lint` in `frontend` | Passed |
| Frontend production build | `npm run build` in `frontend` | Passed |
| Backend build | `dotnet build --no-restore -v minimal` | Passed, 0 warnings |
| Backend unit tests | `dotnet test tests/Helpdesk.UnitTests/Helpdesk.UnitTests.csproj` | Passed, 14/14 |
| Browser E2E tests | `PLAYWRIGHT_PORT=5175 VITE_API_URL=http://127.0.0.1:5080 npm run e2e -- --workers=1` in `frontend` | Passed, 9/9 against the local API |
| Role permission smoke test | local HTTP smoke script against `http://127.0.0.1:5080/api` | Passed: Technician ticket creation is forbidden; Employee assign, internal comment and audit access are forbidden; Employee lists remain own-only |
| README screenshots | `SCREENSHOT_BASE_URL=http://127.0.0.1:5175 npm run screenshots` in `frontend` | Passed |
| Backend integration tests | `dotnet test tests/Helpdesk.IntegrationTests/Helpdesk.IntegrationTests.csproj` | Passed, 23/23 in GitHub Actions (Testcontainers PostgreSQL) |

## E2E Coverage

The Playwright suite currently verifies:

- PL/EN language switch for operational labels and stable SPA URLs
- Employee login and ticket creation
- Technician ticket assignment, status changes and resolution
- Public and internal comments
- Employee cannot see internal technician comments
- Attachment upload and download in ticket details
- Admin asset CSV import/export
- Admin asset creation and user assignment
- Asset assignment history
- Technician links an asset to a ticket
- Admin creates a user and changes their role
- Admin creates and deactivates a ticket category

## Docker Note

Integration tests and the full stack need Docker with the Compose plugin:

```bash
docker compose config
dotnet test tests/Helpdesk.IntegrationTests/Helpdesk.IntegrationTests.csproj
docker compose up --build
```
