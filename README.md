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
- Generate **AI-powered study recommendations**

---

## ✅ **Phase 1 – Foundation (Complete)**

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

## 🚀 **Phase 2 – AI Integration (Complete)**

| Feature | Status |
|--------|--------|
| AI-Powered Study Guide | ✅ Personalized recommendations based on risk level |
| Mock AI Service | ✅ Rule-based, works out-of-the-box |
| Real AI (Azure OpenAI) | ✅ Ready – just add your API key |
| AI Usage Tracking | ✅ Monitor calls and fallbacks |
| Caching | ✅ Redis + Memory fallback |
| Telemetry | ✅ Application Insights integration |
| Advisor Modification | ✅ Track changes to AI recommendations |

---

## 🛠️ **Tech Stack**

- **Backend:** ASP.NET Core 8 (MVC)
- **Database:** SQL Server + Entity Framework Core 8
- **Authentication:** ASP.NET Core Identity
- **Frontend:** Razor Views + Bootstrap 5
- **AI:** Azure OpenAI / Mock Service (pluggable)
- **Cache:** Redis / Memory Cache
- **Monitoring:** Application Insights
- **Load Testing:** k6
- **CI/CD:** GitHub Actions

---

## 🔐 **Azure OpenAI Configuration**

### Local Development (User Secrets) - Recommended
```bash
dotnet user-secrets init
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://your-resource.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:ApiKey" "your-api-key"
dotnet user-secrets set "AzureOpenAI:DeploymentName" "gpt-4"
dotnet user-secrets set "AzureOpenAI:ApiVersion" "2024-02-15-preview"
Feature Flags
Configure in appsettings.json:

AiFeatures:Enabled – master switch

AiFeatures:UseRealAi – use Azure OpenAI when configured

AiFeatures:CacheDurationHours – recommendation cache TTL (default 24h)

AiFeatures:MaxRecommendationsPerDay – per-advisor daily limit

Fallback Behavior
If Azure OpenAI is not configured or fails, system automatically falls back to Mock AI

Failures are logged and visible in Admin dashboard

🟢 Redis Cache
Local Development
bash
docker run --name gradpath-redis -p 6379:6379 -d redis:7
Configure:

Redis:Enabled=true

Redis:ConnectionString=localhost:6379

Redis:InstanceName=GradPath

If Redis is disabled/unavailable, app uses MemoryCacheFallbackService.

📊 Application Insights
Configure:

ApplicationInsights:Enabled=true

ApplicationInsights:ConnectionString=<your-connection-string>

ApplicationInsights:CloudRoleName=GradPath-API

If disabled, telemetry falls back to NullTelemetryService.

👥 Multi-Tenant Support
First-pass multi-tenant support with tenant resolution and super-admin management.

Resolution modes: Subdomain or Header (X-Tenant-ID)

Toggle in appsettings.json: MultiTenant:Enabled

New entities: Tenant, TenantFeature, TenantUserRole

Data isolation via global query filters

Super admin tools: /SuperAdmin/Tenants

Apply migration:

bash
dotnet ef database update
📈 Real User Monitoring (RUM)
Client-side tracking wired to Application Insights:

TTFB, First Contentful Paint, Time to Interactive

Device/browser/screen footprint

Region (timezone-derived)

Enable in appsettings.json under RealUserMonitoring.

🧪 Load Testing
k6 scripts for advisor, student, and mixed workloads:

LoadTests/k6-scripts/advisor-flow.js

LoadTests/k6-scripts/student-flow.js

LoadTests/k6-scripts/mixed-workload.js

Run example:

bash
k6 run LoadTests/k6-scripts/mixed-workload.js --env BASE_URL=https://your-env
⚙️ Setup Instructions
Prerequisites
.NET SDK 8.0+

SQL Server

Git

1. Clone Repository
bash
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
4. Run Application
bash
dotnet run
Navigate to https://localhost:5001

🔑 Default Roles
Admin – Full system access

SuperAdmin – Multi-tenant management

Advisor – Monitor students, assign interventions

Student – View personal study guides and progress

🧪 Testing the App
Advisor Flow
Login as Advisor

View Advisor Dashboard → See at-risk students

Click student → Create Study Guide (AI pre-populates)

Modify if needed → Save → Go to Pending Reviews

Approve guide → Student sees it in My Study Guides

Student Flow
Login as Student

View My Study Guides → See approved guides

View My Grad Path → Track progress

Admin Flow
Login as Admin

Manage users in Admin Panel

View Data Quality dashboard with AI metrics

Check Health Check page for service status

SuperAdmin Flow
Login as SuperAdmin

Manage tenants in SuperAdmin Panel

Switch between tenants to test isolation

📂 Project Structure
text
StudentTrackingCoach/
├── Controllers/
├── Services/
│   ├── Interfaces/
│   └── Implementations/
├── Models/
├── Data/
├── Views/
├── Migrations/
├── LoadTests/
└── StudentTrackingCoach.Tests/
🤝 Contributing
Contributions are welcome! Please:

Fork the repository

Create a feature branch

Submit a pull request

📄 License
This project is licensed under the MIT License.

📬 Contact
Project Lead: Medhat Ali
🔗 GitHub: @MedhatZ
