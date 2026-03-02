# RegulatorIQ

AI-powered regulatory change tracking and compliance framework management for the natural gas industry.

## What this project does

RegulatorIQ helps compliance teams:

- monitor federal/state regulatory inputs,
- analyze documents with AI,
- map regulations to internal compliance frameworks,
- run per-document impact assessments,
- receive near real-time UI notifications for major compliance events.

## Current implementation status

### Core backend (implemented)

- ASP.NET Core API (`net8.0`) with EF Core + PostgreSQL.
- Regulatory documents API: list, detail, search, recent, stats, analyze, bulk analyze.
- Regulatory alerts API includes acknowledgment flow and audit metadata.
- Regulatory alerts API supports acknowledgment, resolution state, and audit trail entries.
- Compliance frameworks API: CRUD-lite, sync, impact assessment, impact assessment history.
- Compliance framework lifecycle fields/actions: status, owner, next review date.
- Agencies API: list and detail.
- Hangfire integrated in API + dedicated background worker service.
- Background monitoring jobs now pull federal/state documents from ML monitoring endpoints and ingest them into the document store.
- Monitoring run telemetry persisted for federal/state jobs (counts, duration, failures, and per-source metrics).
- Health endpoint (`/health`) and Hangfire dashboard (`/hangfire`).

### AI/ML services (implemented)

- FastAPI service for:
  - document analysis,
  - impact assessment,
  - federal/state monitoring endpoints,
  - batch analysis,
  - entity extraction and classification helpers.
- Pluggable analysis strategy in API/background services:
  - `AI` mode (ML service only),
  - `Rules` mode (deterministic rules engine only),
  - `Auto` mode (AI first, rules fallback).

### Frontend (implemented)

- React + TypeScript + Material UI + React Query.
- Views:
  - Dashboard
  - Monitoring operations page (run history, filters, expandable run details)
  - Monitoring job schedules/status table (last run, next run, cron, state)
  - Alerts history and filtering (status/severity/type)
  - Alerts SLA analytics (time-to-ack, time-to-resolve, unresolved aging, 7/30-day trends, per-severity breakdown)
  - Documents list
  - Document analysis detail
  - Compliance frameworks list
  - Compliance framework detail (regulation mappings + run impact assessments)

### Real-time notifications (implemented)

- SignalR hub: `/hubs/notifications`.
- Backend emits:
  - `impactAssessmentCompleted`
  - `frameworkSynced`
  - `alertAcknowledged`
  - `alertResolved`
  - `frameworkLifecycleUpdated`
- Frontend subscribes and shows snackbar notifications, then refreshes relevant React Query caches.

## Recent advancements

Latest completed work:

- Added Compliance Framework Detail workflow:
  - regulation mappings table,
  - per-document impact-run action,
  - recent assessment sidebar.
- Extended framework detail payload to include mapped document details.
- Added end-to-end SignalR notifications for sync and impact completion.
- Added alert acknowledgment flow from dashboard to API persistence.
- Added framework lifecycle management (status/owner/review date) with API + UI controls.
- Added alerts history screen with filters and resolve workflow (resolved state + notes).
- Added audit logging for alert acknowledgment/resolution transitions.
- Added alerts SLA metrics on history view (average time-to-ack, average time-to-resolve, unresolved aging with >24h/>72h counters).
- Added 7/30-day SLA trend lines and per-severity SLA breakdown to alerts analytics.
- Added pluggable analysis providers (`IAIAnalysisProvider`, `IRulesAnalysisProvider`) with configurable `Analysis:Mode` and automatic AI→rules failover in `Auto` mode.
- Added live federal/state monitoring ingestion in background jobs by calling ML services (`/monitor/federal`, `/monitor/state`) with source-based flattening and synthetic-ID dedupe fallback.

## Repository structure

- `src/RegulatorIQ.API` — ASP.NET Core API
- `src/RegulatorIQ.BackgroundServices` — Hangfire worker host
- `src/RegulatorIQ.MLServices` — FastAPI ML service
- `frontend` — React application
- `scripts/init-db.sql` — DB initialization script
- `docker-compose.yml` — local container orchestration
- `build_sheet.md` — target product spec and roadmap source

## Quick start

## Prerequisites

- Docker + Docker Compose (recommended path), or
- Local toolchain:
  - .NET SDK 8+
  - Python 3.11+
  - Node.js (recommend Node 20 LTS)
  - PostgreSQL + Redis (if not using containers)

## Environment setup

1. Copy `.env.example` to `.env`.
2. Fill required values, especially:

- `DB_USER`
- `DB_PASSWORD`
- `OPENAI_API_KEY`
- `HUGGINGFACE_API_TOKEN`

## Run with Docker Compose (recommended)

From repository root:

```bash
docker compose up --build
```

Expected services:

- Frontend: `http://localhost:3000`
- API: `http://localhost:5000`
- ML service: `http://localhost:8000`
- Nginx: `http://localhost`

## Run locally (without full compose)

### 1) API

```bash
dotnet run --project src/RegulatorIQ.API
```

### 2) Background services

```bash
dotnet run --project src/RegulatorIQ.BackgroundServices
```

### 3) ML services

```bash
cd src/RegulatorIQ.MLServices
pip install -r requirements.txt
python main.py
```

### 4) Frontend

```bash
cd frontend
npm install --legacy-peer-deps
npm start
```

Note: `react-scripts@5` can conflict with newer TypeScript/Node combinations. If you hit dependency resolution/build issues, use Node 20 LTS and `npm install --legacy-peer-deps`.

## API overview

### Agencies

- `GET /api/agencies`
- `GET /api/agencies/{id}`

### Regulatory documents

- `GET /api/regulatorydocuments`
- `GET /api/regulatorydocuments/{id}`
- `GET /api/regulatorydocuments/{id}/analysis`
- `POST /api/regulatorydocuments/{id}/analyze`
- `GET /api/regulatorydocuments/search`
- `GET /api/regulatorydocuments/recent`
- `GET /api/regulatorydocuments/stats`
- `GET /api/regulatorydocuments/alerts`
- `POST /api/regulatorydocuments/alerts/{id}/acknowledge`
- `POST /api/regulatorydocuments/alerts/{id}/resolve`
- `POST /api/regulatorydocuments/bulk-analyze`

### Monitoring operations

- `GET /api/monitoring/runs?runType={federal|state}&status={completed|failed}&take=50`
- `GET /api/monitoring/summary?hours=24`
- `POST /api/monitoring/trigger` with body `{ "runType": "all|federal|state" }`
- `GET /api/monitoring/jobs`
- `POST /api/monitoring/jobs/{jobId}/trigger`
- `POST /api/monitoring/jobs/{jobId}/pause`
- `POST /api/monitoring/jobs/{jobId}/resume`
- `GET /api/monitoring/jobs/audit?jobId={jobId}&action={action}&actor={actor}&fromUtc={isoUtc}&toUtc={isoUtc}&sortBy={createdAt|action|actor|job}&sortDir={asc|desc}&skip=0&take=100`
- `GET /api/monitoring/jobs/audit/summary?jobId={jobId}&actor={actor}&fromUtc={isoUtc}&toUtc={isoUtc}`
- `GET /api/monitoring/jobs/audit/export?jobId={jobId}&action={action}&actor={actor}&fromUtc={isoUtc}&toUtc={isoUtc}&take=5000` (CSV)

Each monitoring action endpoint accepts optional actor metadata in body:

- `{ "actedBy": "user@company.com", "reason": "optional note" }`

Monitoring actions (`trigger`, `pause`, `resume`) now write per-job audit records in `RegulatoryAuditLogs` with:

- action (`monitoring_job_triggered|monitoring_job_paused|monitoring_job_resumed`),
- actor (`ChangedBy`),
- timestamp (`CreatedAt`),
- structured before/after state in `OldData`/`NewData`.

Returns execution history and operational metrics including:

- document counts (`fetched`, `added`, `skipped`),
- run duration,
- run failure counts,
- aggregated per-source breakdown.

Frontend route:

- `/monitoring` — dedicated operations view with run history filters, expandable source-level metrics, per-job actions (`Trigger now`, `Pause`, `Resume`), and a Job Action Audit panel (action/actor/time/reason).
- `/monitoring` query params now support sharing operational context, including audit filters/paging/sort: `auditJob`, `auditAction`, `auditActor`, `auditWindow`, `auditSort`, `auditDir`, `auditPage`, `auditPageSize`.
- `/monitoring` includes a `Copy View Link` action to share the current route + query state.
- `/monitoring` Job Action Audit panel includes `Export CSV` for the active audit filters/window.
- `/monitoring` Job Action Audit panel includes action breakdown chips (`Total`, `Triggered`, `Paused`, `Resumed`) for current audit filters/window.
- `/monitoring` Job Action Audit panel includes mini leaderboards for `Top actors` and `Top jobs` in the active audit filter window.
- `/monitoring` Job Action Audit panel includes `Clear Audit Filters` to reset audit filters/window/sort/paging to defaults.

### Compliance frameworks

- `GET /api/complianceframeworks?companyId={guid}`
- `POST /api/complianceframeworks`
- `GET /api/complianceframeworks/{id}`
- `PUT /api/complianceframeworks/{id}`
- `PUT /api/complianceframeworks/{id}/lifecycle`
- `POST /api/complianceframeworks/{id}/sync`
- `POST /api/complianceframeworks/{id}/impact-assessment`
- `GET /api/complianceframeworks/{id}/impact-assessments`

## SignalR contract

Hub endpoint:

- `/hubs/notifications`

Server events:

- `impactAssessmentCompleted`
  - payload includes `frameworkId`, `documentId`, `assessmentId`, `impactScore`, `riskLevel`, `assessmentDate`, `documentTitle`
- `frameworkSynced`
  - payload includes `frameworkId`, `frameworkName`, `newRegulationsFound`, `syncedAt`
- `alertAcknowledged`
  - payload includes `alertId`, `title`, `frameworkId`, `acknowledgedBy`, `acknowledgedAt`
- `alertResolved`
  - payload includes `alertId`, `title`, `frameworkId`, `resolvedBy`, `resolvedAt`, `resolutionNotes`
- `frameworkLifecycleUpdated`
  - payload includes `frameworkId`, `frameworkName`, `status`, `owner`, `nextReviewDate`, `updatedAt`

## Analysis mode configuration

Set `Analysis:Mode` in appsettings/environment for API and background services:

- `Auto` (default): attempts AI analysis; if unavailable/fails, falls back to rules analysis.
- `AI`: AI analysis only; failures return default low-confidence fallback summary.
- `Rules`: rules-based analysis only (no ML dependency).

Examples:

- `Analysis__Mode=Auto`
- `Analysis__Mode=AI`
- `Analysis__Mode=Rules`

## Monitoring source configuration

Background monitoring supports source selection and request timeout via config:

- `Monitoring__RequestTimeoutSeconds=60`
- `Monitoring__federal__Sources__0=federal_register`
- `Monitoring__federal__Sources__1=ferc`
- `Monitoring__state__Sources__0=texas`

## Operations and diagnostics

- API Swagger (Development): `/swagger`
- API health: `/health`
- Hangfire dashboard: `/hangfire`

## Keep this README up to date

When shipping new work, update these sections in the same PR:

- `Current implementation status`
- `Recent advancements`
- `Quick start` (if env vars/scripts changed)
- `API overview` and/or `SignalR contract` (if interfaces changed)

Recommended PR checklist item:

- [ ] README updated for user-visible feature, endpoint, event, or runbook changes

## Next build-sheet alignment targets

After current delivery, likely next areas to implement from `build_sheet.md`:

- deeper monitoring source integrations and reliability hardening,
- alert analytics and SLA-style workflow reporting,
- production readiness/security hardening and docs.
