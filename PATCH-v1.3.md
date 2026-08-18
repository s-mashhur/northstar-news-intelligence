# Patch v1.3 — EF Core 9 migration model synchronization

## Symptom

Backend remained unhealthy and logs contained:

`Microsoft.EntityFrameworkCore.Migrations.PendingModelChangesWarning`

## Root cause

EF Core 9 throws when `Migrate()` / `MigrateAsync()` detects that the runtime model differs from the last migration snapshot. The original portfolio migration had been hand-authored and its snapshot omitted provider-specific Npgsql model metadata; it also lacked the standard migration `Designer.cs` metadata file.

## Changes

- Rebuilt the model snapshot metadata in generated-style form.
- Added Npgsql relational model annotations (`Relational:MaxIdentifierLength` and default identity strategy).
- Added `202608180001_InitialCreate.Designer.cs` migration metadata.
- Added a design-time `IDesignTimeDbContextFactory<AppDbContext>` so EF CLI commands do not execute application startup/seeding/background workers.
- Made table mappings explicit in `AppDbContext`.
- Added a CI guard using `dotnet ef migrations has-pending-model-changes`.

## Clean restart

Because this is still a fresh demo database, reset the volumes once:

```powershell
docker compose down -v
docker compose build --no-cache backend
docker compose up -d
docker compose ps
docker compose logs backend --tail=100
```

Do not use `down -v` against a database containing data you need to keep.
