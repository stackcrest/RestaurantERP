using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantERP.Application.Common;
using RestaurantERP.Application.Interfaces;
using RestaurantERP.Domain.Entities;
using RestaurantERP.Domain.Enums;

namespace RestaurantERP.Web.Areas.SuperAdmin.Controllers;

[Area("SuperAdmin")]
[Authorize(Roles = "SuperAdmin")]
public class ApplicationController : Controller
{
    private readonly IApplicationContentService _content;
    private readonly IWebHostEnvironment _env;

    public ApplicationController(IApplicationContentService content, IWebHostEnvironment env)
    {
        _content = content;
        _env = env;
    }

    public async Task<IActionResult> Index(string? section)
    {
        ViewBag.Sections = AppContentSections.All;
        ViewBag.ActiveSection = section ?? AppContentSections.Site;
        var items = await _content.GetSectionItemsAsync(ViewBag.ActiveSection);
        return View(items);
    }

    public IActionResult Create(string? section)
    {
        ViewBag.Sections = AppContentSections.All;
        ViewBag.DefaultSection = section ?? AppContentSections.Site;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        string contentKey, string value, AppContentType contentType, string section,
        string label, string? description, IFormFile? upload)
    {
        try
        {
            if (contentType is AppContentType.Image or AppContentType.File)
            {
                var uploaded = await SaveUploadAsync(upload);
                if (!string.IsNullOrEmpty(uploaded))
                    value = uploaded;
            }

            await _content.CreateItemAsync(contentKey, value, contentType, section, label, description);
            TempData["Success"] = "Content item created.";
            return RedirectToAction(nameof(Index), new { section });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Create), new { section });
        }
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var items = await _content.GetAllItemsAsync();
        var item = items.FirstOrDefault(i => i.Id == id);
        if (item == null) return NotFound();
        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        Guid id, string value, bool isPublished, IFormFile? upload, string? section)
    {
        try
        {
            var items = await _content.GetAllItemsAsync();
            var item = items.FirstOrDefault(i => i.Id == id);
            if (item == null) return NotFound();

            if (item.ContentType is AppContentType.Image or AppContentType.File)
            {
                var uploaded = await SaveUploadAsync(upload);
                if (!string.IsNullOrEmpty(uploaded))
                    value = uploaded;
            }

            await _content.UpdateItemAsync(id, value, isPublished);
            TempData["Success"] = $"'{item.Label}' updated successfully.";
            return RedirectToAction(nameof(Index), new { section = section ?? item.Section });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Edit), new { id });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, string? section)
    {
        try
        {
            await _content.DeleteItemAsync(id);
            TempData["Success"] = "Custom content item deleted.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { section });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkSave(string section, IFormCollection form)
    {
        try
        {
            var items = await _content.GetSectionItemsAsync(section);
            foreach (var item in items)
            {
                var fieldKey = $"item_{item.Id}";
                if (!form.ContainsKey(fieldKey)) continue;

                var value = form[fieldKey].ToString() ?? "";
                var publishedKey = $"published_{item.Id}";
                var isPublished = form.ContainsKey(publishedKey);

                if (item.ContentType is AppContentType.Image or AppContentType.File)
                {
                    var file = Request.Form.Files[$"upload_{item.Id}"];
                    var uploaded = await SaveUploadAsync(file);
                    if (!string.IsNullOrEmpty(uploaded))
                        value = uploaded;
                }

                await _content.UpdateItemAsync(item.Id, value, isPublished);
            }

            TempData["Success"] = $"{section} section saved successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { section });
    }

    public async Task<IActionResult> Navigation()
    {
        var menus = await _content.GetNavigationMenusAsync();
        return View(menus);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveNavItem(
        Guid? id, Guid menuId, string label, string url, string? icon,
        int displayOrder, bool isActive, bool openInNewTab)
    {
        try
        {
            await _content.SaveNavigationItemAsync(id, menuId, label, url, icon, displayOrder, isActive, openInNewTab);
            TempData["Success"] = id.HasValue ? "Navigation link updated." : "Navigation link added.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Navigation));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteNavItem(Guid id)
    {
        try
        {
            await _content.DeleteNavigationItemAsync(id);
            TempData["Success"] = "Navigation link removed.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Navigation));
    }

    private async Task<string?> SaveUploadAsync(IFormFile? file)
    {
        if (file == null || file.Length == 0) return null;

        var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg", ".pdf", ".ico" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowed.Contains(ext)) return null;

        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "content");
        Directory.CreateDirectory(uploadsDir);

        var fileName = $"{Guid.NewGuid():N}{ext}";
        var path = Path.Combine(uploadsDir, fileName);
        await using var stream = new FileStream(path, FileMode.Create);
        await file.CopyToAsync(stream);

        return $"/uploads/content/{fileName}";
    }
}
