# Azure Deployment Plan

**Status:** Validated

## 1. Deployment summary

Repair the GitHub Actions workflow that publishes the Orchard Core CMS application to the existing Azure App Service named `urbanAgents` in West Europe.

## 2. Application

- Repository: `urbanit/OrchardCore`
- Source branch: `urbanit/main`
- Application project: `src/OrchardCore.Cms.Web/OrchardCore.Cms.Web.csproj`
- Runtime: .NET 10

## 3. Target

- Service: Existing Azure App Service
- App name: `urbanAgents`
- Endpoint: `https://urbanagents-fehbb6f8b6hvdybx.westeurope-01.azurewebsites.net/`
- Region: West Europe

## 4. Deployment approach

Use the existing GitHub Actions OpenID Connect credentials to build, publish, and deploy the CMS application artifact with `azure/webapps-deploy`.

## 5. Planned changes

1. Inspect the failed GitHub Actions run and compare it with the working historic workflow.
2. Correct the project-specific build and publish commands in `.github/workflows/urbanit-main_urbanagents.yml`.
3. Validate the workflow syntax and build command.
4. Commit and push the workflow repair to `urbanit/main`, trigger the deployment, and verify the live endpoint.

## 6. Security and rollback

- Reuse existing GitHub Actions OIDC secrets; do not add credentials to the repository.
- The deployment overwrites the production App Service package. Roll back by redeploying the preceding successful workflow artifact or commit.

## 7. Validation proof

| Check | Command | Result | Timestamp |
|---|---|---|---|
| Failed workflow diagnosis | `gh run view 35067344285 --repo urbanit/OrchardCore --log-failed` | Publish invoked without a project path and failed with `NETSDK1129` | 2026-09-16T10:23:00+03:00 |
| Corrected publish command | `dotnet publish src/OrchardCore.Cms.Web/OrchardCore.Cms.Web.csproj -c Release -f net10.0` | Reaches application compilation; local SDK reports unrelated existing navigation source-generation errors | 2026-09-16T10:26:00+03:00 |
| GitHub runner build | `gh run view 35067344285 --repo urbanit/OrchardCore --log-failed` | The prior `dotnet build --configuration Release` step completed on .NET SDK 10.0.401 before publish began | 2026-09-16T10:23:00+03:00 |
| Workflow structure | PowerShell check of project path, `net10.0` framework, publish output, and App Service target | Pass | 2026-09-16T10:28:00+03:00 |
| Production reachability | `curl https://urbanagents-fehbb6f8b6hvdybx.westeurope-01.azurewebsites.net/` | HTTP 200 | 2026-09-16T10:23:00+03:00 |

No Azure resources will be created or changed outside the existing App Service deployment package. Subscription quota validation is not applicable.
