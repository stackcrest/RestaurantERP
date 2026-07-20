using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantERP.Application.Interfaces;
using RestaurantERP.Domain.Entities;

namespace RestaurantERP.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class InventoryController : Controller
{
    private readonly IApplicationDbContext _context;

    public InventoryController(IApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index(string? tab)
    {
        ViewBag.Tab = tab ?? "ingredients";
        ViewBag.Ingredients = await _context.Ingredients.OrderBy(i => i.Name).ToListAsync();
        ViewBag.Suppliers = await _context.Suppliers.OrderBy(s => s.Name).ToListAsync();
        ViewBag.Grns = await _context.GRNs
            .Include(g => g.Supplier)
            .Include(g => g.Items)
            .OrderByDescending(g => g.ReceivedDate)
            .Take(20)
            .ToListAsync();
        ViewBag.LowStockCount = await _context.Ingredients.CountAsync(i => i.CurrentStock <= i.LowStockThreshold);
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> CreateIngredient(string name, string unit, decimal lowStockThreshold, decimal unitCost, bool isPerishable, decimal initialStock)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Ingredient name is required.";
            return RedirectToAction(nameof(Index));
        }

        _context.Ingredients.Add(new Ingredient
        {
            Name = name.Trim(),
            Unit = string.IsNullOrWhiteSpace(unit) ? "kg" : unit.Trim(),
            LowStockThreshold = lowStockThreshold,
            UnitCost = unitCost,
            IsPerishable = isPerishable,
            CurrentStock = initialStock >= 0 ? initialStock : 0
        });

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Ingredient \"{name.Trim()}\" added.";
        return RedirectToAction(nameof(Index), new { tab = "ingredients" });
    }

    public async Task<IActionResult> EditIngredient(Guid id)
    {
        var ingredient = await _context.Ingredients.FindAsync(id);
        if (ingredient == null) return NotFound();
        return View(ingredient);
    }

    [HttpPost]
    public async Task<IActionResult> EditIngredient(Guid id, string name, string unit, decimal lowStockThreshold, decimal unitCost, bool isPerishable, DateTime? expiryDate)
    {
        var ingredient = await _context.Ingredients.FindAsync(id);
        if (ingredient == null) return NotFound();

        ingredient.Name = name.Trim();
        ingredient.Unit = string.IsNullOrWhiteSpace(unit) ? "kg" : unit.Trim();
        ingredient.LowStockThreshold = lowStockThreshold;
        ingredient.UnitCost = unitCost;
        ingredient.IsPerishable = isPerishable;
        ingredient.ExpiryDate = expiryDate;
        ingredient.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        TempData["Success"] = "Ingredient updated.";
        return RedirectToAction(nameof(Index), new { tab = "ingredients" });
    }

    [HttpPost]
    public async Task<IActionResult> AdjustStock(Guid id, decimal quantity, string? note)
    {
        var ingredient = await _context.Ingredients.FindAsync(id);
        if (ingredient == null) return RedirectToAction(nameof(Index));

        ingredient.CurrentStock += quantity;
        if (ingredient.CurrentStock < 0) ingredient.CurrentStock = 0;
        ingredient.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Stock adjusted for {ingredient.Name} ({(quantity >= 0 ? "+" : "")}{quantity} {ingredient.Unit}).";
        return RedirectToAction(nameof(Index), new { tab = "ingredients" });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteIngredient(Guid id)
    {
        var ingredient = await _context.Ingredients.FindAsync(id);
        if (ingredient == null) return RedirectToAction(nameof(Index));

        var inRecipes = await _context.RecipeIngredients.AnyAsync(r => r.IngredientId == id);
        if (inRecipes)
        {
            TempData["Error"] = "Cannot delete ingredient used in recipes.";
            return RedirectToAction(nameof(Index), new { tab = "ingredients" });
        }

        _context.Ingredients.Remove(ingredient);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Ingredient deleted.";
        return RedirectToAction(nameof(Index), new { tab = "ingredients" });
    }

    [HttpPost]
    public async Task<IActionResult> CreateSupplier(string name, string? contactPerson, string? phone, string? email)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Supplier name is required.";
            return RedirectToAction(nameof(Index), new { tab = "suppliers" });
        }

        _context.Suppliers.Add(new Supplier
        {
            Name = name.Trim(),
            ContactPerson = contactPerson,
            Phone = phone,
            Email = email,
            IsActive = true
        });

        await _context.SaveChangesAsync();
        TempData["Success"] = $"Supplier \"{name.Trim()}\" added.";
        return RedirectToAction(nameof(Index), new { tab = "suppliers" });
    }

    public async Task<IActionResult> CreateGrn()
    {
        ViewBag.Suppliers = await _context.Suppliers.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();
        ViewBag.Ingredients = await _context.Ingredients.OrderBy(i => i.Name).ToListAsync();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> CreateGrn(Guid supplierId, string? notes, List<Guid>? ingredientIds, List<decimal>? quantities, List<decimal>? unitPrices)
    {
        if (ingredientIds == null || !ingredientIds.Any())
        {
            TempData["Error"] = "Add at least one item to the stock receipt.";
            return RedirectToAction(nameof(CreateGrn));
        }

        var supplier = await _context.Suppliers.FindAsync(supplierId);
        if (supplier == null)
        {
            TempData["Error"] = "Select a valid supplier.";
            return RedirectToAction(nameof(CreateGrn));
        }

        var grn = new GRN
        {
            GRNNumber = $"GRN-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
            SupplierId = supplierId,
            ReceivedDate = DateTime.UtcNow,
            Notes = notes,
            Items = new List<GRNItem>()
        };

        decimal total = 0;
        for (var i = 0; i < ingredientIds.Count; i++)
        {
            var ingId = ingredientIds[i];
            var qty = quantities != null && i < quantities.Count ? quantities[i] : 0;
            var price = unitPrices != null && i < unitPrices.Count ? unitPrices[i] : 0;

            if (qty <= 0) continue;

            var ingredient = await _context.Ingredients.FindAsync(ingId);
            if (ingredient == null) continue;

            var lineTotal = qty * price;
            grn.Items.Add(new GRNItem
            {
                IngredientId = ingId,
                Quantity = qty,
                UnitPrice = price,
                TotalPrice = lineTotal
            });

            ingredient.CurrentStock += qty;
            ingredient.UnitCost = price > 0 ? price : ingredient.UnitCost;
            ingredient.UpdatedAt = DateTime.UtcNow;
            total += lineTotal;
        }

        if (!grn.Items.Any())
        {
            TempData["Error"] = "No valid items in stock receipt.";
            return RedirectToAction(nameof(CreateGrn));
        }

        grn.TotalAmount = total;
        _context.GRNs.Add(grn);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Stock receipt {grn.GRNNumber} recorded. Inventory updated.";
        return RedirectToAction(nameof(Index), new { tab = "grn" });
    }
}
