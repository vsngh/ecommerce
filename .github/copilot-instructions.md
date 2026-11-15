# Copilot instructions

## Repository structure

- The frontend is a React/Vite app under `services/frontend`.
- The .NET 8 APIs and gateway are under `services/`.
- APIs use controllers, DTOs, services, EF Core contexts, and entities; keep changes in the existing layer rather than moving logic across layers.
- Each API except the gateway currently has an xUnit test project under its `tests` directory.

## Change expectations

- Inspect the relevant controller, service, DTO, and tests before changing API behavior. Preserve existing route and response shapes unless the task explicitly changes them.
- Keep API and frontend contracts aligned. The frontend calls services through the gateway paths configured in `services/frontend/src/main.jsx` and `services/gateway`.
- Add or update focused tests for behavior changes. Prefer the existing xUnit and EF Core InMemory patterns for API tests.
- Keep configuration environment-driven. Do not add credentials, tokens, or production secrets to source, compose files, workflows, or manifests.
- Avoid adding dependencies unless needed for the requested change. When frontend dependencies change, update `package-lock.json`.
- Preserve each service's Docker build context and validate relevant builds when practical.
- For AI features, use only approved data and models, keep answers grounded in retrievable sources, and include appropriate access controls and human escalation.

## Validation

- Run the affected API test project with `dotnet test <path-to-tests.csproj> --configuration Release`.
- Build the gateway with `dotnet build services/gateway/Gateway.Api.csproj --configuration Release` when it is affected.
- Build the frontend with `npm ci` followed by `npm run build` from `services/frontend`.
- The GitHub Actions workflow in `.github/workflows/ci.yml` runs these checks and builds service container images.
