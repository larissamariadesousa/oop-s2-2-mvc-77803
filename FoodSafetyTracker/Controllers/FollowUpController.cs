using FoodSafetyTracker.Data;
using FoodSafetyTracker.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodSafetyTracker.Controllers;

[Authorize]
public class FollowUpController : Controller
{
    private readonly AppDbContext _db;
    private readonly ILogger<FollowUpController> _logger;

    public FollowUpController(AppDbContext db, ILogger<FollowUpController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [Authorize(Roles = "Admin,Inspector")]
    [Route("FollowUp/Create/{inspectionId:int}")]
    public async Task<IActionResult> Create(int inspectionId)
    {
        var inspection = await _db.Inspections.Include(i => i.Premises).FirstOrDefaultAsync(i => i.Id == inspectionId);
        if (inspection == null) return NotFound();

        ViewBag.Inspection = inspection;
        return View(new CreateFollowUpViewModel
        {
            InspectionId = inspectionId,
            DueDate = DateTime.Today.AddDays(14)
        });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,Inspector")]
    public async Task<IActionResult> Create(CreateFollowUpViewModel vm)
    {
        var inspection = await _db.Inspections.Include(i => i.Premises).FirstOrDefaultAsync(i => i.Id == vm.InspectionId);
        if (inspection == null) return NotFound();
        
        if (vm.DueDate < inspection.InspectionDate)
        {
            _logger.LogWarning("FollowUp creation failed: DueDate {DueDate} is before InspectionDate {InspectionDate} for InspectionId={InspectionId} by {User}",
                vm.DueDate, inspection.InspectionDate, vm.InspectionId, User.Identity?.Name);
            ModelState.AddModelError("DueDate", "Due date cannot be before the inspection date.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Inspection = inspection;
            return View(vm);
        }

        var followUp = new FollowUp
        {
            InspectionId = vm.InspectionId,
            DueDate      = vm.DueDate,
            Status       = FollowUpStatus.Open,
            Notes        = vm.Notes
        };

        _db.FollowUps.Add(followUp);
        await _db.SaveChangesAsync();

        _logger.LogInformation("FollowUp created: FollowUpId={FollowUpId} InspectionId={InspectionId} DueDate={DueDate} by {User}",
            followUp.Id, followUp.InspectionId, followUp.DueDate, User.Identity?.Name);

        TempData["Success"] = "Follow-up added.";
        return RedirectToAction("Details", "Inspection", new { id = vm.InspectionId });
    }

    [Authorize(Roles = "Admin,Inspector")]
    public async Task<IActionResult> Close(int id)
    {
        var followUp = await _db.FollowUps.Include(f => f.Inspection).FirstOrDefaultAsync(f => f.Id == id);
        if (followUp == null) return NotFound();
        return View(followUp);
    }

    [HttpPost, ActionName("Close"), ValidateAntiForgeryToken, Authorize(Roles = "Admin,Inspector")]
    public async Task<IActionResult> CloseConfirmed(int id, [FromForm] DateTime? closedDate)
    {
        var followUp = await _db.FollowUps.Include(f => f.Inspection).FirstOrDefaultAsync(f => f.Id == id);
        if (followUp == null) return NotFound();

        
        if (!closedDate.HasValue)
        {
            _logger.LogWarning("FollowUp close failed: No ClosedDate provided for FollowUpId={FollowUpId} by {User}",
                id, User.Identity?.Name);
            ModelState.AddModelError("", "A closed date is required.");
            return View(followUp);
        }

        followUp.Status     = FollowUpStatus.Closed;
        followUp.ClosedDate = closedDate;
        await _db.SaveChangesAsync();

        _logger.LogInformation("FollowUp closed: FollowUpId={FollowUpId} ClosedDate={ClosedDate} by {User}",
            followUp.Id, closedDate, User.Identity?.Name);

        TempData["Success"] = "Follow-up closed.";
        return RedirectToAction("Details", "Inspection", new { id = followUp.InspectionId });
    }
}
