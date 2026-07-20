using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantERP.Application.Interfaces;
using RestaurantERP.Domain.Entities;
using RestaurantERP.Domain.Enums;

namespace RestaurantERP.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class TablesController : Controller
{
    private readonly IApplicationDbContext _context;

    public TablesController(IApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index(Guid? sectionId)
    {
        var sections = await _context.TableSections.OrderBy(s => s.Name).ToListAsync();
        var query = _context.Tables.Include(t => t.Section).AsQueryable();

        if (sectionId.HasValue)
            query = query.Where(t => t.SectionId == sectionId.Value);

        var tables = await query.OrderBy(t => t.Section.Name).ThenBy(t => t.TableNumber).ToListAsync();

        ViewBag.Sections = sections;
        ViewBag.SelectedSectionId = sectionId;
        return View(tables);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Sections = await _context.TableSections.OrderBy(s => s.Name).ToListAsync();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(string tableNumber, int capacity, Guid sectionId, TableStatus status)
    {
        if (string.IsNullOrWhiteSpace(tableNumber))
        {
            ModelState.AddModelError("", "Table number is required.");
            ViewBag.Sections = await _context.TableSections.OrderBy(s => s.Name).ToListAsync();
            return View();
        }

        if (!await _context.TableSections.AnyAsync(s => s.Id == sectionId))
        {
            TempData["Error"] = "Please select a valid section.";
            return RedirectToAction(nameof(Create));
        }

        var exists = await _context.Tables.AnyAsync(t =>
            t.SectionId == sectionId && t.TableNumber == tableNumber.Trim());

        if (exists)
        {
            TempData["Error"] = "A table with this number already exists in the selected section.";
            return RedirectToAction(nameof(Create));
        }

        _context.Tables.Add(new RestaurantTable
        {
            TableNumber = tableNumber.Trim(),
            Capacity = capacity > 0 ? capacity : 2,
            SectionId = sectionId,
            Status = status
        });

        await _context.SaveChangesAsync();
        TempData["Success"] = "Table created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> CreateSection(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Section name is required.";
            return RedirectToAction(nameof(Index));
        }

        _context.TableSections.Add(new TableSection
        {
            Name = name.Trim(),
            Description = description
        });

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Section \"{name.Trim()}\" created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var table = await _context.Tables.Include(t => t.Section).FirstOrDefaultAsync(t => t.Id == id);
        if (table == null) return NotFound();

        ViewBag.Sections = await _context.TableSections.OrderBy(s => s.Name).ToListAsync();
        return View(table);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, string tableNumber, int capacity, Guid sectionId, TableStatus status)
    {
        var table = await _context.Tables.FindAsync(id);
        if (table == null) return NotFound();

        var duplicate = await _context.Tables.AnyAsync(t =>
            t.Id != id && t.SectionId == sectionId && t.TableNumber == tableNumber.Trim());

        if (duplicate)
        {
            TempData["Error"] = "Another table with this number exists in the selected section.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        table.TableNumber = tableNumber.Trim();
        table.Capacity = capacity > 0 ? capacity : 2;
        table.SectionId = sectionId;
        table.Status = status;
        table.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        TempData["Success"] = "Table updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus(Guid id, TableStatus status)
    {
        var table = await _context.Tables.FindAsync(id);
        if (table != null)
        {
            table.Status = status;
            table.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Table {table.TableNumber} marked as {status}.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id)
    {
        var table = await _context.Tables
            .Include(t => t.Orders)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (table == null) return RedirectToAction(nameof(Index));

        var hasActiveOrders = table.Orders.Any(o =>
            o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled);

        if (hasActiveOrders)
        {
            TempData["Error"] = "Cannot delete table with active orders.";
            return RedirectToAction(nameof(Index));
        }

        _context.Tables.Remove(table);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Table deleted.";
        return RedirectToAction(nameof(Index));
    }
}
