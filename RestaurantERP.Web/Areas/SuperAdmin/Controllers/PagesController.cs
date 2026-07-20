using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantERP.Application.Interfaces;
using RestaurantERP.Domain.Entities;
using RestaurantERP.Domain.Enums;

namespace RestaurantERP.Web.Areas.SuperAdmin.Controllers;

[Area("SuperAdmin")]
[Authorize(Roles = "SuperAdmin")]
public class PagesController : Controller
{
    private readonly IApplicationDbContext _context;

    public PagesController(IApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        var pages = await _context.UiPages.Include(p => p.Blocks).OrderBy(p => p.Title).ToListAsync();
        return View(pages);
    }

    public IActionResult Create() => View();

    [HttpPost]
    public async Task<IActionResult> Create(string title, string slug, string visibility)
    {
        var uiPage = new UiPage
        {
            Title = title,
            Slug = slug.ToLower().Replace(" ", "-"),
            Visibility = Enum.TryParse<PageVisibility>(visibility, true, out var vis) ? vis : PageVisibility.All,
            Status = PageStatus.Draft
        };
        _context.UiPages.Add(uiPage);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Page created!";
        return RedirectToAction("Edit", new { id = uiPage.Id });
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var uiPage = await _context.UiPages.Include(p => p.Blocks.OrderBy(b => b.DisplayOrder)).FirstOrDefaultAsync(p => p.Id == id);
        if (uiPage == null) return NotFound();
        return View(uiPage);
    }

    [HttpPost]
    public async Task<IActionResult> Update(Guid id, string title, string slug, string? metaDescription, string visibility, string status)
    {
        var uiPage = await _context.UiPages.FindAsync(id);
        if (uiPage == null) return NotFound();

        uiPage.Title = title;
        uiPage.Slug = slug;
        uiPage.MetaDescription = metaDescription;
        uiPage.Visibility = Enum.TryParse<PageVisibility>(visibility, true, out var vis) ? vis : PageVisibility.All;
        uiPage.Status = Enum.Parse<PageStatus>(status, true);
        uiPage.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        TempData["Success"] = "Page updated!";
        return RedirectToAction("Edit", new { id });
    }

    [HttpPost]
    public async Task<IActionResult> AddBlock(Guid pageId, string blockType, string content)
    {
        var maxOrder = await _context.UiPageBlocks.Where(b => b.PageId == pageId).Select(b => (int?)b.DisplayOrder).MaxAsync() ?? 0;
        var block = new UiPageBlock
        {
            PageId = pageId,
            BlockType = Enum.Parse<BlockType>(blockType, true),
            Content = content,
            DisplayOrder = maxOrder + 1
        };
        _context.UiPageBlocks.Add(block);
        await _context.SaveChangesAsync();
        return RedirectToAction("Edit", new { id = pageId });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateBlock(Guid id, string content)
    {
        var block = await _context.UiPageBlocks.FindAsync(id);
        if (block != null)
        {
            block.Content = content;
            block.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction("Edit", new { id = block?.PageId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteBlock(Guid id)
    {
        var block = await _context.UiPageBlocks.FindAsync(id);
        if (block != null)
        {
            var pageId = block.PageId;
            _context.UiPageBlocks.Remove(block);
            await _context.SaveChangesAsync();
            return RedirectToAction("Edit", new { id = pageId });
        }
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id)
    {
        var uiPage = await _context.UiPages.FindAsync(id);
        if (uiPage != null)
        {
            uiPage.IsDeleted = true;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Page deleted.";
        }
        return RedirectToAction("Index");
    }
}
