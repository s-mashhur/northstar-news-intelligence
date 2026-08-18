# v1.2 Backend Runtime Dependency Fix

This patch fixes the Docker startup crash:

`Could not load file or assembly 'Microsoft.EntityFrameworkCore.Relational, Version=9.0.19.0'`

## Changes

- Added an explicit `Microsoft.EntityFrameworkCore.Relational` 9.0.19 reference to `NewsIntelligence.Infrastructure`.
- Added an explicit runtime reference to the API executable project so publish cannot drop the relational assembly.
- Added Docker publish guards for:
  - `Microsoft.EntityFrameworkCore.Relational.dll`
  - `Npgsql.EntityFrameworkCore.PostgreSQL.dll`
  - `NewsIntelligence.Infrastructure.dll`
- Added README troubleshooting instructions for this exact error.

## Clean rebuild

```bash
docker compose down -v
docker compose build --no-cache backend
docker compose up -d
docker compose ps
```
