# Student Tracking Coach

## Azure OpenAI Integration

This project supports both Azure OpenAI and a mock AI fallback service.  
Do not store real API keys in `appsettings.json` for shared repos.

## 🔐 Azure OpenAI Configuration

### 1️⃣ Local Development (User Secrets) - Recommended

```bash
# Initialize user secrets (if not already done)
dotnet user-secrets init

# Set your Azure OpenAI credentials
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://your-resource.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:ApiKey" "your-api-key"
dotnet user-secrets set "AzureOpenAI:DeploymentName" "gpt-4"
dotnet user-secrets set "AzureOpenAI:ApiVersion" "2024-02-15-preview"
```

### 2️⃣ Feature Flags

Configure AI behavior in `appsettings.json`:

- `AiFeatures:Enabled` - master switch for AI generation.
- `AiFeatures:UseRealAi` - when true, use Azure OpenAI if configuration is valid.
- `AiFeatures:UseMockService` - optional compatibility flag.
- `AiFeatures:CacheDurationHours` - recommendation cache TTL.
- `AiFeatures:MaxRecommendationsPerDay` - per-advisor daily soft limit.

### 3️⃣ Fallback Behavior

- If Azure OpenAI is not configured or fails at runtime, the system automatically falls back to `MockAiRecommendationService`.
- Failures are logged and visible in the admin health/data quality views.

## Redis Cache

### Local Development

1. Install Redis locally (or use Docker):

```bash
docker run --name gradpath-redis -p 6379:6379 -d redis:7
```

2. Configure:
- `Redis:Enabled=true`
- `Redis:ConnectionString=localhost:6379`
- `Redis:InstanceName=GradPath`

If Redis is disabled or unavailable, the app automatically uses `MemoryCacheFallbackService`.

### Production

- Use a managed Redis service (Azure Cache for Redis recommended).
- Set connection string via environment variables or secret manager.

## Application Insights

Configure:

- `ApplicationInsights:Enabled=true`
- `ApplicationInsights:ConnectionString=<your-connection-string>`
- `ApplicationInsights:CloudRoleName=GradPath-API`

If disabled or not configured, telemetry falls back to `NullTelemetryService`.

## Testing

![Coverage](https://img.shields.io/badge/coverage-enabled-brightgreen)

Run unit tests:

```bash
dotnet test StudentTrackingCoach.Tests/StudentTrackingCoach.Tests.csproj
```

Run with coverage:

```bash
dotnet test StudentTrackingCoach.Tests/StudentTrackingCoach.Tests.csproj --collect:"XPlat Code Coverage"
```

## Multi-Tenant Support

The platform now includes first-pass multi-tenant support with tenant resolution, tenant-scoped entities, and super-admin tenant management.

- Resolution modes:
  - `Subdomain` (for `tenant1.gradpath.com`)
  - `Header` (`X-Tenant-ID`)
- Toggle in `appsettings.json`:
  - `MultiTenant:Enabled`
  - `MultiTenant:ResolutionMode`
  - `MultiTenant:DefaultTenantId`
- New entities:
  - `Tenant`
  - `TenantFeature`
  - `TenantUserRole`
- Tenant-aware data isolation:
  - Global query filters on `Student`, `AdvisorNotes`, `Interventions`, and `AdminAuditLogs`
  - Tenant indexes for high-cardinality query paths
- Super admin tools:
  - `/SuperAdmin/Tenants`
  - `/SuperAdmin/CreateTenant`
  - `/SuperAdmin/TenantDetails/{id}`
  - tenant switching via session (`SelectedTenantId`)

Apply the migration:

```bash
dotnet ef database update
```

## Real User Monitoring (RUM)

RUM is wired to Application Insights telemetry via a client-side tracker + ingestion endpoint.

- Enable in `appsettings.json` under `RealUserMonitoring`
- Client script: `wwwroot/js/rum-tracker.js`
- Partial include: `Views/Shared/_RUM.cshtml`
- API ingestion:
  - `POST /rum/page-view`
  - `POST /rum/action`
- Server service: `ApplicationInsightsRUMService`

Tracked metrics include:

- TTFB
- First Contentful Paint
- Time to Interactive
- Page load completion
- device/browser/screen footprint
- coarse region (timezone-derived)

Recommended Azure alert rules:

- p95 page load > 3 seconds for 5 minutes
- JS/action tracking failure > 1%

## Load Testing

Load tests are provided with k6 scripts for advisor, student, and mixed workloads.

- Scripts:
  - `LoadTests/k6-scripts/advisor-flow.js`
  - `LoadTests/k6-scripts/student-flow.js`
  - `LoadTests/k6-scripts/mixed-workload.js`
- Config: `LoadTests/config/options.json`
- Results template: `LoadTests/results/README.md`
- CI nightly workflow: `.github/workflows/nightly-load-tests.yml`

Run examples:

```bash
k6 run LoadTests/k6-scripts/mixed-workload.js --env BASE_URL=https://your-env --env TEST_TYPE=normal
k6 run LoadTests/k6-scripts/mixed-workload.js --env BASE_URL=https://your-env --env TEST_TYPE=peak
k6 run LoadTests/k6-scripts/mixed-workload.js --env BASE_URL=https://your-env --env TEST_TYPE=stress
k6 run LoadTests/k6-scripts/mixed-workload.js --env BASE_URL=https://your-env --env TEST_TYPE=soak
```

Target benchmarks:

- p95 response time `< 2s`
- CPU `< 70%` at peak
- memory `< 80%` at peak
- zero functional errors at expected load
