#!/usr/bin/env bash
# One-off: apply EF migrations, then load api/seed/*.json as drafts (publish them in /admin).
# Usage (from anywhere; keeps the secret out of argv/history if you export it first):
#   export ConnectionStrings__Db='Host=...;Database=...;Username=...;Password=...;SSL Mode=Require'
#   api/scripts/migrate-and-seed.sh
# Requires the dotnet-ef tool: dotnet tool install --global dotnet-ef
set -euo pipefail

if [[ -z "${ConnectionStrings__Db:-}" ]]; then
  echo "Set ConnectionStrings__Db to the target database's Npgsql connection string." >&2
  exit 1
fi

cd "$(dirname "$0")/.."

echo "==> Applying migrations"
dotnet ef database update \
  --project src/NextShore.Infrastructure \
  --startup-project src/NextShore.Api \
  --connection "$ConnectionStrings__Db"

echo "==> Loading seed data"
DOTNET_ENVIRONMENT=Production ASPNETCORE_ENVIRONMENT=Production \
  dotnet run --project src/NextShore.Api -- seed

echo "==> Done. Publish the seeded drafts in /admin."
