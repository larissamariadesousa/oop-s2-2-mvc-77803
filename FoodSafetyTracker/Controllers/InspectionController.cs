using FoodSafetyTracker.Data;
using FoodSafetyTracker.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FoodSafetyTracker.Controllers;

[Authorize]
public class InspectionController : Controller
{
    private readonly AppDbContext _db;
    private readonly ILogger<InspectionController> _logger;

    public InspectionController(AppDbContext db, ILogger<InspectionController> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var list = await _db.Inspections
            .Include(i => i.Premises)
            .OrderByDescending(i => i.InspectionDate)
            .ToListAsync();
        return View(list);
    }

    public async Task<IActionResult> Details(int id)
    {
        var inspection = await _db.Inspections
            .Include(i => i.Premises)
            .Include(i => i.FollowUps)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (inspection == null)
        {
            _logger.LogWarning("Inspection {InspectionId} not found – requested by {User}", id, User.Identity?.Name);
            return NotFound();
        }

        return View(inspection);
    }

    [Authorize(Roles = "Admin,Inspector")]
    public async Task<IActionResult> Create(int? premisesId)
    {
        ViewBag.PremisesList = new SelectList(
            await _db.Premises.OrderBy(p => p.Name).ToListAsync(), "Id", "Name", premisesId);
        return View(new CreateInspectionViewModel
        {
            PremisesId = premisesId ?? 0,
            InspectionDate = DateTime.Today
        });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,Inspector")]
    public async Task<IActionResult> Create(CreateInspectionViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.PremisesList = new SelectList(
                await _db.Premises.OrderBy(p => p.Name).ToListAsync(), "Id", "Name", vm.PremisesId);
            return View(vm);
        }

        var inspection = new Inspection
        {
            PremisesId    = vm.PremisesId,
            InspectionDate = vm.InspectionDate,
            Score         = vm.Score,
            Outcome       = vm.Outcome,
            Notes         = vm.Notes
        };

        _db.Inspections.Add(inspection);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Inspection created: InspectionId={InspectionId} PremisesId={PremisesId} Outcome={Outcome} Score={Score} by {User}",
            inspection.Id, inspection.PremisesId, inspection.Outcome, inspection.Score, User.Identity?.Name);

        if (inspection.Outcome == InspectionOutcome.Fail)
            _logger.LogWarning("FAILED inspection recorded: InspectionId={InspectionId} PremisesId={PremisesId} Score={Score}",
                inspection.Id, inspection.PremisesId, inspection.Score);

        TempData["Success"] = "Inspection recorded.";
        return RedirectToAction("Details", new { id = inspection.Id });
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var inspection = await _db.Inspections.Include(i => i.Premises).FirstOrDefaultAsync(i => i.Id == id);
        if (inspection == null) return NotFound();
        return View(inspection);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var inspection = await _db.Inspections.FindAsync(id);
        if (inspection == null) return NotFound();

        try
        {
            _db.Inspections.Remove(inspection);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Inspection deleted: {InspectionId} by {User}", id, User.Identity?.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting Inspection {InspectionId}", id);
        }

        return RedirectToAction(nameof(Index));
    }
}
