# Northstar — News Intelligence Platform

**Production-oriented portfolio project for news search, monitoring and analytics using ASP.NET Core 9, Next.js 15, PostgreSQL, Redis and Webz.io.**

Northstar is a full-stack intelligence dashboard designed as a realistic developer demo for the Webz.io News API ecosystem. It supports live Webz.io integration and a deterministic mock mode, so the complete application can be evaluated without exposing or requiring an API token.

> **Important:** This repository demonstrates hands-on Webz.io integration code. Do not describe it as paid/client Webz.io experience unless you have actually used it in a client engagement.

> **Runtime lifecycle note:** This repository intentionally targets .NET 9. Microsoft currently lists .NET 9 as an STS release in maintenance support through November 10, 2026. For a long-lived deployment beyond that date, upgrade the backend to .NET 10 LTS before .NET 9 reaches end of support.

## Highlights

- ASP.NET Core **9 / C# 13** API
- Next.js **15 + React 19 + TypeScript** frontend
- Clean project boundaries: Domain / Application / Infrastructure / API
- PostgreSQL persistence with EF Core migrations
- Optional Redis distributed search cache
- JWT access tokens + rotating refresh tokens
- PBKDF2 password hashing (no plaintext passwords)
- Webz.io provider with server-side token handling
- Mock provider for zero-secret demo mode
- Scheduled monitor worker that stores newly observed articles
- Dashboard analytics: volume, sources, sentiment, recent coverage
- Search UI, monitor management, login flow, responsive dark interface
- Nginx gateway + Docker Compose deployment
- Liveness/readiness health endpoints, OpenAPI document, CORS and API rate limiting
- xUnit backend tests and GitHub Actions CI

## Architecture

```text
Browser
   │
   ▼
Nginx :8080
   ├──────────────► Next.js :3000
   │
   └──── /api ───► ASP.NET Core 9 :8080
                       │
             ┌─────────┼───────────────┐
             ▼         ▼               ▼
         PostgreSQL   Redis       Webz.io News API
             ▲                         ▲
             └──── Monitor Worker ─────┘
```

More detail: [`docs/architecture.md`](docs/architecture.md)

## Webz.io integration

The live provider uses the current News API endpoint:

```text
GET https://api.webz.io/api/news
```

Authentication is sent as a server-side Bearer token. Search requests include `q`, `ts` (Unix milliseconds) and `size`. `ts` is clamped to the supported recent-news window. The response mapper is intentionally defensive and understands common Webz envelope/property variants (`posts`/`articles`, `total_results`/`totalResults`) so minor response-shape differences fail gracefully.

The browser **never receives the Webz.io API token**.

## Repository layout

```text
.
├── backend/
│   ├── src/
│   │   ├── NewsIntelligence.Domain/
│   │   ├── NewsIntelligence.Application/
│   │   ├── NewsIntelligence.Infrastructure/
│   │   └── NewsIntelligence.Api/
│   └── tests/NewsIntelligence.Tests/
├── frontend/
├── nginx/
├── docs/
├── preview/
├── docker-compose.yml
└── .github/workflows/ci.yml
```

# Quick start — Docker (recommended)

## Prerequisites

- Docker Desktop / Docker Engine with Compose v2
- Git

No local .NET, Node, PostgreSQL or Redis installation is required for Docker mode.

## 1. Configure environment

From the repository root:

```bash
cp .env.example .env
```

For an immediate portfolio demo, keep:

```env
WEBZ_MODE=Mock
SEED_DEMO_DATA=true
```

Change at least these values before any internet-facing deployment:

```env
JWT_SECRET=<generate-a-long-random-secret>
POSTGRES_PASSWORD=<strong-password>
```

## 2. Start the stack

```bash
docker compose up --build -d
```

Then open:

- Application: `http://localhost:8080`
- Health: `http://localhost:8080/health`
- Readiness: `http://localhost:8080/health/ready`
- OpenAPI JSON: `http://localhost:8080/openapi/v1.json`

Seeded demo credentials:

```text
Email:    demo@newsintel.dev
Password: Demo123!
```

## 3. Stop

```bash
docker compose down
```

To also remove database/Redis volumes:

```bash
docker compose down -v
```

# Enable real Webz.io data

1. Obtain a Webz.io token from your Webz.io account/dashboard.
2. Edit `.env`:

```env
WEBZ_MODE=Real
WEBZ_API_TOKEN=your-real-token
```

3. Recreate the backend:

```bash
docker compose up -d --build backend gateway
```

Do **not** commit `.env` or your token. `.gitignore` already excludes local env files.

# Local development without Docker

You can run everything with an in-memory database and mock news provider.

## Requirements

- .NET SDK 9.x
- Node.js 20.9+ (Node 22 recommended)
- npm

## Backend

```bash
cd backend

dotnet restore NewsIntelligence.sln
dotnet run --project src/NewsIntelligence.Api --urls http://localhost:5080
```

Default development configuration uses:

```text
Database = InMemory
Redis    = disabled
Webz     = Mock
Seed     = enabled
```

API health check:

```bash
curl http://localhost:5080/health
```

## Frontend

In another terminal:

```bash
cd frontend
cp .env.local.example .env.local
npm install
npm run dev
```

Open `http://localhost:3000`.

# Local PostgreSQL development

Override the API configuration with environment variables:

```bash
export Database__Provider=Postgres
export ConnectionStrings__Postgres='Host=localhost;Port=5432;Database=news_intelligence;Username=newsintel;Password=change-me'
export Redis__Enabled=false

dotnet run --project backend/src/NewsIntelligence.Api --urls http://localhost:5080
```

EF Core migrations are applied automatically at startup when the provider is PostgreSQL.

# Database migrations

A local `dotnet-ef` tool manifest and an initial PostgreSQL migration/snapshot are included. From `backend/`:

```bash
dotnet tool restore
dotnet ef migrations add YourMigrationName \
  --project src/NewsIntelligence.Infrastructure \
  --startup-project src/NewsIntelligence.Api
```

At runtime, relational migrations are applied automatically during startup. For stricter production release processes, move migration execution into a dedicated deployment job instead of running it from every API replica.

# Tests

Backend:

```bash
cd backend
dotnet test NewsIntelligence.sln
```

Frontend type check:

```bash
cd frontend
npm install
npm run typecheck
```

Production frontend build:

```bash
npm run build
```

Full Docker smoke test:

```bash
docker compose up --build -d
curl --fail http://localhost:8080/health
```

# API overview

| Method | Route | Auth | Purpose |
|---|---|---:|---|
| `POST` | `/api/auth/register` | No | Create account |
| `POST` | `/api/auth/login` | No | Access + refresh tokens |
| `POST` | `/api/auth/refresh` | No | Rotate refresh token |
| `GET` | `/api/dashboard` | Yes | Analytics dashboard |
| `POST` | `/api/news/search` | Yes | Search provider |
| `GET` | `/api/monitors` | Yes | List monitors |
| `POST` | `/api/monitors` | Yes | Create + initial run |
| `POST` | `/api/monitors/{id}/run` | Yes | Manual refresh |
| `DELETE` | `/api/monitors/{id}` | Yes | Delete monitor/data |
| `GET` | `/api/meta` | Yes | Active provider/version |
| `GET` | `/health` | No | Liveness check |
| `GET` | `/health/ready` | No | Database readiness check |

# Security notes

This portfolio implementation includes sensible application-level defaults, but "production-ready" still requires environment-specific operational work.

Implemented in the repository:

- Webz API secret stays backend-only.
- JWT signing secret is environment driven.
- Access tokens are short-lived; refresh tokens are random, hashed at rest and rotated.
- Passwords are PBKDF2-HMAC-SHA256 hashed with per-password salt.
- ASP.NET rate limiting protects API routes.
- Nginx adds basic security headers and edge rate limiting.
- CORS is explicit rather than `AllowAnyOrigin`.
- EF Core parameterization protects normal query paths from SQL injection.
- Container services expose only the gateway publicly in Compose.

Recommended before a real public launch:

- Put the gateway behind HTTPS/TLS (Cloudflare, Azure Front Door, ALB, Caddy, Traefik, etc.).
- Store secrets in a managed secret store rather than `.env`.
- Disable `Seed:Enabled`.
- Restrict CORS to the final domain.
- Add centralized logging/metrics/tracing and alerting.
- Add database backups and restore testing.
- Add an email-verification/password-reset provider if public registration is enabled.
- Pin container digests as part of your release process.
- Review Webz.io licensing/redistribution terms for your intended use case.

# Production deployment pattern

A straightforward cloud deployment:

```text
Cloudflare / managed TLS
        │
        ▼
Nginx / load balancer
   ├── Next.js container (2+ replicas)
   └── ASP.NET API (1+ replicas)
             ├── Managed PostgreSQL
             ├── Managed Redis
             └── Webz.io
```

The included `docker-compose.yml` is ideal for a portfolio VPS/demo. For serious scale, deploy the services independently and use managed PostgreSQL/Redis. If the API is horizontally scaled, run the scheduled monitor worker as a singleton/dedicated worker or add a distributed lease so the same monitor is not executed concurrently by multiple replicas.

# Configuration reference

## Backend

| Setting | Default | Description |
|---|---|---|
| `Database__Provider` | `InMemory` | `InMemory` or `Postgres` |
| `ConnectionStrings__Postgres` | local dev value | PostgreSQL connection |
| `Redis__Enabled` | `false` | Enable distributed cache |
| `Redis__ConnectionString` | `localhost:6379` | Redis endpoint |
| `Jwt__Secret` | dev-only value | Signing secret; 32+ chars |
| `Jwt__Issuer` | `NewsIntelligence` | JWT issuer |
| `Jwt__Audience` | `NewsIntelligence.Web` | JWT audience |
| `Webz__Mode` | `Mock` | `Mock` or `Real` |
| `Webz__BaseUrl` | `https://api.webz.io/` | Webz API base URL |
| `Webz__ApiToken` | empty | Webz token, server-side only |
| `Seed__Enabled` | `true` in dev | Create demo user/monitors |

## Frontend

```env
NEXT_PUBLIC_API_BASE_URL=http://localhost:5080/api
```

Docker builds the frontend with `/api` so browser calls go through Nginx on the same origin.

# Portfolio / résumé description

You can describe this project like this:

> Built a full-stack news intelligence platform with ASP.NET Core 9, Next.js, PostgreSQL, Redis and Webz.io integration. Implemented Clean Architecture boundaries, JWT/refresh-token authentication, scheduled news monitoring, distributed caching, persistent analytics, responsive dashboards, Docker deployment and automated tests/CI.

Suggested résumé bullets:

- Integrated Webz.io News API behind a secure ASP.NET Core provider abstraction with mock/live modes and defensive response mapping.
- Built scheduled monitoring pipelines that deduplicate and persist news articles for trend, source and sentiment analytics.
- Designed a responsive Next.js dashboard and containerized the full stack with PostgreSQL, Redis and Nginx.
- Implemented JWT authentication, rotating hashed refresh tokens, PBKDF2 password storage, API rate limiting and CI checks.

# راهنمای سریع فارسی

برای اجرای سریع پروژه فقط Docker لازم داری:

```bash
cp .env.example .env
docker compose up --build -d
```

بعد برو به:

```text
http://localhost:8080
```

ورود نمونه:

```text
demo@newsintel.dev
Demo123!
```

در حالت پیش‌فرض پروژه روی `Mock` است و بدون Webz API Key کاملاً قابل نمایش است. برای داده واقعی در فایل `.env` مقدارهای زیر را تغییر بده:

```env
WEBZ_MODE=Real
WEBZ_API_TOKEN=YOUR_TOKEN
```

سپس:

```bash
docker compose up -d --build backend gateway
```

توکن Webz را هیچ‌وقت داخل Frontend یا GitHub قرار نده.

# UI preview without installing dependencies

Open `preview/index.html` directly in a browser for a lightweight static preview of the visual language. The actual interactive interface lives in `frontend/`.


# Upstream references

- Webz.io News Search documentation: `https://docs.webz.io/docs/webz/news-search`
- Webz.io API reference: `https://docs.webz.io/docs/webz/news-search-api-reference`
- .NET support policy: `https://dotnet.microsoft.com/platform/support/policy/dotnet-core`
- Next.js security releases: `https://nextjs.org/blog/tag/security`

# License

MIT. Webz.io and any publisher names appearing in demo data remain the property of their respective owners.


## Docker startup troubleshooting

If Compose reports `backend is unhealthy`, inspect the backend before changing anything:

```bash
docker compose ps
docker compose logs backend --tail=200
docker inspect --format='{{json .State.Health}}' news-intelligence-platform-backend-1
```

The backend image uses a liveness probe at `/health`; database readiness remains available separately at `/health/ready`. Database migrations include transient retry/backoff so a slow first PostgreSQL startup does not immediately kill the API.

To retry from a clean local database during development only:

```bash
docker compose down -v
docker compose up --build -d
```

`down -v` deletes local PostgreSQL/Redis volumes, so do not use it on data you need to keep.


## Troubleshooting

### Backend crashes with `Microsoft.EntityFrameworkCore.Relational` missing

If you are upgrading from v1.1 and previously saw:

```text
Could not load file or assembly 'Microsoft.EntityFrameworkCore.Relational, Version=9.0.19.0'
```

use v1.2 or newer. v1.2 makes `Microsoft.EntityFrameworkCore.Relational` an explicit runtime dependency in both the Infrastructure and API projects and verifies the required EF/Npgsql assemblies during the Docker build. Rebuild without stale images:

```bash
docker compose down -v
docker compose build --no-cache backend
docker compose up -d
docker compose ps
```

Then, if needed:

```bash
docker compose logs backend --tail=200
```

## EF Core 9 migration consistency

EF Core 9 treats pending model changes as an error when `Migrate()`/`MigrateAsync()` runs. This repository therefore keeps complete migration metadata and includes a CI guard:

```bash
cd backend
dotnet tool restore
dotnet ef migrations has-pending-model-changes \
  --project src/NewsIntelligence.Infrastructure/NewsIntelligence.Infrastructure.csproj \
  --startup-project src/NewsIntelligence.Api/NewsIntelligence.Api.csproj \
  --context AppDbContext
```

If you intentionally change an entity or mapping, create and commit a migration before deployment. Do not suppress `PendingModelChangesWarning` as a routine fix; synchronize the migration metadata instead.

## v1.4 clean-rebuild note

If a backend build fails, do **not** immediately run `docker compose up -d`: Docker may reuse an older local image with the same Compose image name. For v1.4, use:

```powershell
docker compose down -v --remove-orphans
docker image rm news-intelligence-platform-backend -f 2>$null
docker compose build --no-cache backend
# Continue only after a successful build.
docker compose up -d
curl http://localhost:8080/health
```

The health response must contain `"version":"1.4.0"`; otherwise an older image is still running.
