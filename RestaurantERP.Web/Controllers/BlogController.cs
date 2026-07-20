using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantERP.Application.Interfaces;
using RestaurantERP.Domain.Enums;

namespace RestaurantERP.Web.Controllers;

[AllowAnonymous]
public class BlogController : Controller
{
    private readonly IApplicationDbContext _context;

    public BlogController(IApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index(string? tag)
    {
        var query = _context.BlogPosts
            .Where(p => !p.IsDeleted && p.Status == PageStatus.Published);

        if (!string.IsNullOrWhiteSpace(tag))
            query = query.Where(p => p.Tags != null && p.Tags.Contains(tag));

        var posts = await query
            .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
            .ToListAsync();

        ViewBag.Tag = tag;
        return View(posts);
    }

    public async Task<IActionResult> Post(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return NotFound();

        var post = await _context.BlogPosts
            .FirstOrDefaultAsync(p => p.Slug == id && !p.IsDeleted && p.Status == PageStatus.Published);

        if (post == null) return NotFound();

        ViewData["Title"] = post.Title;
        ViewData["MetaDescription"] = post.MetaDescription ?? post.Summary;

        var related = await _context.BlogPosts
            .Where(p => !p.IsDeleted && p.Status == PageStatus.Published && p.Id != post.Id)
            .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
            .Take(3)
            .ToListAsync();

        ViewBag.RelatedPosts = related;
        return View(post);
    }
}
