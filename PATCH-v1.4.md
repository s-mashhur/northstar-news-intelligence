# Patch v1.4

## What actually happened in v1.3

The v1.3 backend image never finished building. The initial migration class and its EF-generated-style Designer partial both declared `[DbContext]` and `[Migration]` attributes. Since the partial declarations compile into one type, this causes duplicate-attribute compiler errors.

Running `docker compose up -d` after that failed build reused the previously available backend image, so the logs still showed the older `PendingModelChangesWarning` behavior.

## Fixes

- Removed duplicate EF migration attributes from `202608180001_InitialCreate.cs`; metadata now lives only in `202608180001_InitialCreate.Designer.cs`, matching the standard EF migration layout.
- Added an explicit `dotnet build` step before publish so compile failures are easy to see.
- Publish now uses `--no-build` after a successful Release build.
- Added an OCI image version label (`1.4.0`).
- `/health` and `/api/meta` now report version `1.4.0`, making it easy to prove the new image is running.

## Clean rebuild

```powershell
docker compose down -v --remove-orphans
docker image rm news-intelligence-platform-backend -f 2>$null
docker compose build --no-cache backend
# Only continue if the previous command ends successfully.
docker compose up -d
docker compose ps
curl http://localhost:8080/health
```

Expected health payload includes `"version":"1.4.0"`.
