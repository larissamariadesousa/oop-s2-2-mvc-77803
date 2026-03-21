# Food Safety Inspection Tracker

Name: Larissa Maria de Sousa - ID: 77803
ASP.NET Core MVC assessment project — Modern Programming Principles & Practice, Semester 2.

## Tech Stack
- ASP.NET Core 8 MVC
- Entity Framework Core + SQLite
- ASP.NET Core Identity (roles)
- Serilog (Console + rolling file sink)
- xUnit tests
- GitHub Actions CI

## Getting Started

### Prerequisites
- .NET 8 SDK
- Git

### Run locally

```bash
git clone <https://github.com/larissamariadesousa/oop-s2-2-mvc-77803.git>
cd oop-s2-2-mvc-77803/FoodSafetyTracker
dotnet restore
dotnet ef database update       
dotnet run
```

Navigate to `https://localhost:5001`

### Seeded accounts

| Role      | Email                          | Password        |
|-----------|-------------------------------|-----------------|
| Admin     | admin@foodsafety.gov          | Admin@123!      |
| Inspector | inspector@foodsafety.gov      | Inspector@123!  |
| Viewer    | viewer@foodsafety.gov         | Viewer@123!     |

### Run tests

```bash
cd ..
dotnet test --verbosity normal
```

## Project Structure

```
FoodSafetyTracker/
├── Controllers/
│   ├── DashboardController.cs   
│   ├── PremisesController.cs    
│   ├── InspectionController.cs  
│   ├── FollowUpController.cs    
│   └── HomeController.cs       
├── Data/
│   ├── AppDbContext.cs           
│   └── DbSeeder.cs              
├── Models/
│   ├── Entities.cs              
│   └── ViewModels.cs            
├── Views/                       
├── Program.cs                  
└── .github/workflows/ci.yml     

FoodSafetyTracker.Tests/
└── FoodSafetyTests.cs           
```

## Serilog Logging

Logs are written to:
- **Console** — with timestamp, level, username, message
- **File** — `logs/app-YYYYMMDD.log` (daily rolling)


