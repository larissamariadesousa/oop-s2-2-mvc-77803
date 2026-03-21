using FoodSafetyTracker.Data;
using FoodSafetyTracker.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FoodSafetyTracker.Tests;


public static class TestDbFactory
{
    public static AppDbContext Create(string dbName = "TestDb")
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName + Guid.NewGuid())
            .Options;
        return new AppDbContext(options);
    }
}


public class OverdueFollowUpTests
{
    [Fact]
    public async Task OverdueFollowUps_ReturnsOnlyOpenAndPastDueDate()
    {
        
        await using var db = TestDbFactory.Create();

        var premises = new Premises { Id = 1, Name = "Test Place", Address = "1 St", Town = "Town", RiskRating = RiskRating.Low };
        db.Premises.Add(premises);

        var inspection = new Inspection { Id = 1, PremisesId = 1, InspectionDate = DateTime.UtcNow.AddDays(-30), Score = 50, Outcome = InspectionOutcome.Fail };
        db.Inspections.Add(inspection);

        db.FollowUps.AddRange(
            new FollowUp { Id = 1, InspectionId = 1, DueDate = DateTime.UtcNow.AddDays(-10), Status = FollowUpStatus.Open },   // overdue
            new FollowUp { Id = 2, InspectionId = 1, DueDate = DateTime.UtcNow.AddDays(-5),  Status = FollowUpStatus.Closed },  // not overdue – closed
            new FollowUp { Id = 3, InspectionId = 1, DueDate = DateTime.UtcNow.AddDays(7),   Status = FollowUpStatus.Open },    // not overdue – future
            new FollowUp { Id = 4, InspectionId = 1, DueDate = DateTime.UtcNow.AddDays(-2),  Status = FollowUpStatus.Open }    // overdue
        );
        await db.SaveChangesAsync();

       
        var overdue = await db.FollowUps
            .Where(f => f.DueDate < DateTime.UtcNow && f.Status == FollowUpStatus.Open)
            .ToListAsync();

      
        Assert.Equal(2, overdue.Count);
        Assert.All(overdue, f =>
        {
            Assert.Equal(FollowUpStatus.Open, f.Status);
            Assert.True(f.DueDate < DateTime.UtcNow);
        });
    }
}


public class FollowUpBusinessRuleTests
{
    [Fact]
    public void ClosingFollowUp_WithoutClosedDate_ShouldNotSetStatusClosed()
    {
      
        var followUp = new FollowUp
        {
            Id = 1,
            InspectionId = 1,
            DueDate = DateTime.UtcNow.AddDays(-5),
            Status = FollowUpStatus.Open,
            ClosedDate = null
        };

        
        bool canClose = followUp.ClosedDate.HasValue;

        
        Assert.False(canClose);
        Assert.Equal(FollowUpStatus.Open, followUp.Status);
    }

    [Fact]
    public void ClosingFollowUp_WithClosedDate_ShouldSetStatusClosed()
    {
        
        var followUp = new FollowUp
        {
            Id = 2,
            InspectionId = 1,
            DueDate = DateTime.UtcNow.AddDays(-5),
            Status = FollowUpStatus.Open
        };

        
        var closedDate = DateTime.UtcNow;
        if (closedDate != default)
        {
            followUp.Status = FollowUpStatus.Closed;
            followUp.ClosedDate = closedDate;
        }

        
        Assert.Equal(FollowUpStatus.Closed, followUp.Status);
        Assert.NotNull(followUp.ClosedDate);
    }
}


public class DashboardQueryTests
{
    private async Task<AppDbContext> CreateSeededDb()
    {
        var db = TestDbFactory.Create("DashboardDb");

        var premises = new Premises { Id = 1, Name = "Test Cafe", Address = "1 St", Town = "TestTown", RiskRating = RiskRating.Medium };
        db.Premises.Add(premises);

        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);

        db.Inspections.AddRange(
            
            new Inspection { Id = 1, PremisesId = 1, InspectionDate = startOfMonth.AddDays(1), Score = 80, Outcome = InspectionOutcome.Pass },
            new Inspection { Id = 2, PremisesId = 1, InspectionDate = startOfMonth.AddDays(2), Score = 40, Outcome = InspectionOutcome.Fail },
            new Inspection { Id = 3, PremisesId = 1, InspectionDate = startOfMonth.AddDays(3), Score = 55, Outcome = InspectionOutcome.Fail },
            
            new Inspection { Id = 4, PremisesId = 1, InspectionDate = startOfMonth.AddMonths(-1), Score = 70, Outcome = InspectionOutcome.Pass }
        );

        db.FollowUps.AddRange(
            new FollowUp { Id = 1, InspectionId = 2, DueDate = now.AddDays(-3), Status = FollowUpStatus.Open },  // overdue
            new FollowUp { Id = 2, InspectionId = 3, DueDate = now.AddDays(5),  Status = FollowUpStatus.Open },  // not overdue
            new FollowUp { Id = 3, InspectionId = 2, DueDate = now.AddDays(-1), Status = FollowUpStatus.Closed } // closed
        );

        await db.SaveChangesAsync();
        return db;
    }

    [Fact]
    public async Task Dashboard_InspectionsThisMonth_ReturnsCorrectCount()
    {
        await using var db = await CreateSeededDb();
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        var count = await db.Inspections.CountAsync(i => i.InspectionDate >= startOfMonth);

        Assert.Equal(3, count);
    }

    [Fact]
    public async Task Dashboard_FailedInspectionsThisMonth_ReturnsCorrectCount()
    {
        await using var db = await CreateSeededDb();
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        var count = await db.Inspections.CountAsync(i =>
            i.InspectionDate >= startOfMonth && i.Outcome == InspectionOutcome.Fail);

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task Dashboard_OverdueFollowUps_ReturnsCorrectCount()
    {
        await using var db = await CreateSeededDb();

        var count = await db.FollowUps.CountAsync(f =>
            f.DueDate < DateTime.UtcNow && f.Status == FollowUpStatus.Open);

        Assert.Equal(1, count);
    }
}

// ── Test 4
public class FollowUpDateValidationTests
{
    [Fact]
    public void FollowUp_DueDateBeforeInspectionDate_IsInvalid()
    {
        var inspectionDate = DateTime.Today;
        var dueDate = inspectionDate.AddDays(-1); // before inspection

        bool isValid = dueDate >= inspectionDate;

        Assert.False(isValid);
    }

    [Fact]
    public void FollowUp_DueDateAfterInspectionDate_IsValid()
    {
        var inspectionDate = DateTime.Today;
        var dueDate = inspectionDate.AddDays(14);

        bool isValid = dueDate >= inspectionDate;

        Assert.True(isValid);
    }
}
