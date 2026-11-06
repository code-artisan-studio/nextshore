# NextShore — Deploy

| Part | Host | Config |
|---|---|---|
| API | Render web service, Docker runtime | `render.yaml` (Blueprint), `api/Dockerfile` |
| Web | Vercel, root directory `web` | `web/vercel.json` (SPA rewrites) |
| DB | Neon Postgres, `prod` branch | migrations + seed via `api/scripts/migrate-and-seed.sh` |

No secrets live in the repo. Every value below is set in the host's dashboard.

## Environment variables

### API — Render (service → Environment)

| Key | Required | Notes |
|---|---|---|
| `ConnectionStrings__Db` | yes | Neon prod branch, **Npgsql key-value form**: `Host=…;Database=…;Username=…;Password=…;SSL Mode=Require`. Neon's `postgresql://` URL is not accepted as-is. |
| `Anthropic__ApiKey` | yes | Resume parse + summary. |
| `Clerk__Authority` | yes | Clerk Frontend API URL (token issuer). Missing → every `/admin` request is 401 (warned at startup). |
| `Cors__Origins` | yes | Vercel URL(s), comma-separated, no trailing slash. No default outside Development (warned at startup). |
| `Engine__ExchangeRates__CAD` / `__EUR` / `__AUD` | yes for demo | Units per 1 USD. `appsettings.json` has approximate placeholders; set real rates. |
| `PORT` | no | Injected by Render; image defaults to `10000`. |
| `ASPNETCORE_ENVIRONMENT` | no | Defaults to `Production`. Never `Development` in prod. |
| `Anthropic__ParseModel`, `Anthropic__SummaryModel`, `Anthropic__ParseTimeout`, `Anthropic__SummaryTimeout`, `Anthropic__MaxRetries` | no | Defaults in `appsettings.json`. |
| `Engine__NearEligibleMaxGaps`, `Engine__NearEligiblePointsRatio`, `Engine__StaleAfterDays` | no | Defaults in `appsettings.json`. |
| `Assessments__LifetimeDays`, `Assessments__CleanupInterval` | no | Defaults 30 days / 1 day. |

`Rules__Source=seed-preview` is Development-only; startup fails if set in prod.

The keys marked required are declared in `render.yaml` with `sync: false`, so Render prompts for them when the Blueprint is created.

### Web — Vercel (project → Settings → Environment Variables)

| Key | Required | Notes |
|---|---|---|
| `VITE_API_URL` | yes | Render service URL, no trailing slash. Build-time: redeploy after changing. |
| `VITE_CLERK_PUBLISHABLE_KEY` | yes (admin) | Clerk publishable key (public by design). |

### One-off — your machine

| Key | Notes |
|---|---|
| `ConnectionStrings__Db` | Same value as on Render, exported in your shell only for the migrate/seed run. |

## First deploy

1. **Neon**: create a `prod` branch; copy its connection details into the Npgsql form above.
2. **DB**: from the repo root
   ```sh
   export ConnectionStrings__Db='<prod connection string>'
   api/scripts/migrate-and-seed.sh
   unset ConnectionStrings__Db
   ```
   Applies EF migrations, then loads `api/seed/*.json` as **drafts**. Needs `dotnet tool install --global dotnet-ef`.
3. **Render**: New → Blueprint → this repo. Fill the prompted env vars (`Cors__Origins` can wait for step 4). Health check: `/api/v1/health` (also checks the DB).
4. **Vercel**: import the repo, root directory `web`, framework Vite. Set `VITE_API_URL` to the Render URL and the Clerk key. Then set `Cors__Origins` on Render to the Vercel URL(s).
5. **Publish rules**: sign in at `/admin` with an admin-role Clerk user and publish the seeded drafts — the public API serves published rules only.

## Later deploys

- API: push to `main` → Render auto-deploys. Apply new migrations with the script **before** the deploy that needs them Re-running the seed is safe: it upserts drafts and leaves published rules alone.
- Web: push to `main` → Vercel builds.

## Notes

- Free Render instances sleep when idle: first request after a sleep is slow, and the in-memory rate-limit counters and assessment cleanup timer reset.
- Rate limiting keys on `X-Forwarded-For` (one trusted hop). After the first deploy, confirm in the logs that the logged client IP is the caller's, not a Render proxy address.
