# Security

## Reporting

This repository is a portfolio/reference implementation. If you discover a security issue, open a private security advisory in the repository where you host this project rather than publishing secrets or exploit details in a public issue.

## Deployment checklist

- Replace all example JWT/database/demo credentials.
- Disable demo seeding for public environments.
- Keep `WEBZ_API_TOKEN` server-side and in a managed secret store.
- Terminate TLS before the application and redirect HTTP to HTTPS.
- Restrict CORS to your production origins.
- Use managed PostgreSQL/Redis backups, monitoring and network controls.
- Keep .NET, Next.js, React and base container images on serviced security patches.
