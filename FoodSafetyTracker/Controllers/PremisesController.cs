using FoodSafetyTracker.Data;
using FoodSafetyTracker.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodSafetyTracker.Controllers;

[Authorize]
public class PremisesController : Controller
{
    private readonly AppDbContext _db;
    private readonly ILogger<PremisesController> _logger;

    public PremisesController(AppDbContext db, ILogger<PremisesController> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var list = await _db.Premises
            .OrderBy(p => p.Town).ThenBy(p => p.Name)
            .ToListAsync();
        return View(list);
    }

    public async Task<IActionResult> Details(int id)
    {
        var premises = await _db.Premises
            .Include(p => p.Inspections)
                .ThenInclude(i => i.FollowUps)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (premises == null)
        {
            _logger.LogWarning("Premises {PremisesId} not found – requested by {User}", id, User.Identity?.Name);
            return NotFound();
        }

        return View(premises);
    }

    [Authorize(Roles = "Admin")]
    public IActionResult Create() => View();

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(Premises model)
    {
        if (!ModelState.IsValid) return View(model);

        _db.Premises.Add(model);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Premises created: {PremisesId} '{Name}' in {Town} by {User}",
            model.Id, model.Name, model.Town, User.Identity?.Name);

        TempData["Success"] = $"Premises '{model.Name}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var premises = await _db.Premises.FindAsync(id);
        if (premises == null) return NotFound();
        return View(premises);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id, Premises model)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View(model);

        _db.Premises.Update(model);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Premises updated: {PremisesId} '{Name}' by {User}",
            model.Id, model.Name, User.Identity?.Name);

        TempData["Success"] = "Premises updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var premises = await _db.Premises.FindAsync(id);
        if (premises == null) return NotFound();
        return View(premises);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var premises = await _db.Premises.FindAsync(id);
        if (premises == null) return NotFound();

        try
        {
            _db.Premises.Remove(premises);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Premises deleted: {PremisesId} '{Name}' by {User}",
                id, premises.Name, User.Identity?.Name);
            TempData["Success"] = "Premises deleted.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting Premises {PremisesId}", id);
            TempData["Error"] = "Failed to delete premises.";
        }

        return RedirectToAction(nameof(Index));
    }
}
