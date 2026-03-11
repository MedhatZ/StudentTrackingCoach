<<<<<<< HEAD
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
=======
markdown
# 🎓 Grad Path – Student Success & Early-Alert Platform

[![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-8.0-purple)](https://dotnet.microsoft.com/)
[![Entity Framework Core](https://img.shields.io/badge/EF%20Core-8.0-blue)](https://learn.microsoft.com/ef/core/)
[![SQL Server](https://img.shields.io/badge/SQL%20Server-2019+-red)](https://www.microsoft.com/sql-server)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**Grad Path** is a scalable student success platform designed to track academic performance, identify at-risk students early, and trigger structured interventions. Built for colleges and universities, it provides advisors with the tools to monitor students and deliver personalized study plans.

---

## 📌 **Project Overview**

The platform enables institutions to:
- Track student academic data (courses, grades, enrollment)
- Calculate **student and course-level risk** (High/Medium/Low)
- Surface risk insights in **advisor dashboards**
- Assign **structured interventions** (Study Guides, Tasks)
- Log all actions for **accountability and audit**
- Generate **AI-powered study recommendations** (Phase 2)

---

## ✅ **Phase 1 – Complete**

Phase 1 establishes a **solid, production-ready foundation**:

| Feature | Status |
|--------|--------|
| Centralized Risk Logic | ✅ Consistent High/Medium/Low across all views |
| Study Guide Flow | ✅ Create → Review → Approve → Student View |
| `Interventions` Table | ✅ Canonical intervention tracking |
| Tasks System | ✅ Connected to `Interventions` (Pending/Completed) |
| Search & Pagination | ✅ Fast, scalable lists |
| Role-Based Access | ✅ Student, Advisor, Admin |
| User-Student Linking | ✅ `StudentId` tied to `ApplicationUser` |

---

## 🚀 **Phase 2 – Coming Soon**

- AI-powered Study Guide using structured academic signals
- Personalized recommendations (Focus Areas, Schedule, Techniques)
- Advisor approval workflow
- Outcome tracking (did the student improve?)

---

## 🛠️ **Tech Stack**

- **Backend:** ASP.NET Core 8 (MVC)
- **Database:** SQL Server + Entity Framework Core 8
- **Authentication:** ASP.NET Core Identity
- **Frontend:** Razor Views + Bootstrap 5
- **AI (Phase 2):** Azure OpenAI / Mock Service (pluggable)

---

## 📂 **Database Schema**

### Core Tables
- `Students` – Student demographic data
- `Courses` – Course catalog
- `Enrollments` – Student-course enrollment
- `Grades` – Student grades per assignment/course

### Success Tracking
- `Interventions` – All study guides and actions (canonical table)
- `AdvisorNotes` – Advisor comments and flags
- `Tasks` – Derived from `Interventions` (Pending/Completed)

### Identity
- `AspNetUsers` – ApplicationUser with `StudentId` and `AdvisorId`

---

## ⚙️ **Setup Instructions**

### Prerequisites
- [.NET SDK 8.0+](https://dotnet.microsoft.com/download)
- [SQL Server](https://www.microsoft.com/sql-server) (LocalDB, Express, or Azure SQL)
- [Git](https://git-scm.com/)

### 1. Clone the Repository
```bash
git clone https://github.com/MedhatZ/StudentTrackingCoach.git
cd StudentTrackingCoach
2. Configure Database
Update appsettings.json:

json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=GradPathDb;Trusted_Connection=True;MultipleActiveResultSets=true"
}
3. Apply Migrations
bash
dotnet ef database update
4. Run the Application
bash
dotnet run
Navigate to https://localhost:5001

🔑 Default Roles
Admin – Full system access

Advisor – Monitor students, assign interventions

Student – View personal study guides and progress

📊 Key Features Explained
🔹 Risk Calculation
Centralized IRiskCalculationService ensures consistent logic across:

Student lists

Advisor dashboards

Study Guide triggers

🔹 Study Guide Flow
Advisor creates guide for at-risk student

Saved to Interventions table (Status = "Pending")

Advisor reviews and approves

Student sees approved guide in My Study Guides

🔹 Tasks System
Approved Interventions become Pending Tasks

Completing a task updates status to "Completed"

Tracks advisor follow-ups

🔹 Search + Pagination
Search by Student ID or Name

Pagination with page size selector (10, 20, 50, 100)

Consistent across all list views

🧪 Testing the App
Advisor Flow
Login as Advisor

View Advisor Dashboard → See at-risk students

Click student → Create Study Guide

Go to Pending Reviews → Approve guide

Guide appears in student's My Study Guides

Student Flow
Login as Student

View My Study Guides → See approved guides

View My Grad Path → Track progress

Admin Flow
Login as Admin

Manage users in Admin Panel

View Data Quality dashboard

Access all areas (with appropriate messages)

🧠 AI Integration (Phase 2)
The platform is AI-ready:

Structured signals passed to IAiRecommendationService

AI generates personalized study plans

Output stored in Interventions.PayloadJson (JSON)

Advisor approves before student sees it

🤝 Contributing
Contributions are welcome! Please:

Fork the repository

Create a feature branch

Submit a pull request

📄 License
This project is licensed under the MIT License – see the LICENSE file for details.

📬 Contact
Project Lead: Medhat Zaki
📧 Email: [your-email@example.com]
🔗 GitHub: @MedhatZ

Built with 💪 for student success.
>>>>>>> ac0c2f4d38dbd0f4fcfa6762d54857acdf980c10
