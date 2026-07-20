using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantERP.Application.Interfaces;
using RestaurantERP.Domain.Entities;
using RestaurantERP.Domain.Enums;

namespace RestaurantERP.Web.Areas.SuperAdmin.Controllers;

[Area("SuperAdmin")]
[Authorize(Roles = "SuperAdmin")]
public class BlogController : Controller
{
    private readonly IApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _env;

    public BlogController(
        IApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment env)
    {
        _context = context;
        _userManager = userManager;
        _env = env;
    }

    public async Task<IActionResult> Index()
    {
        var posts = await _context.BlogPosts
            .Where(p => !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
        return View(posts);
    }

    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        string title, string slug, string? summary, string content, string? featuredImageUrl,
        IFormFile? featuredImage, string status, string? metaDescription, string? tags)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        slug = await EnsureUniqueSlugAsync(NormalizeSlug(slug, title));

        var imageUrl = await SaveImageAsync(featuredImage) ?? featuredImageUrl;
        var postStatus = Enum.TryParse<PageStatus>(status, true, out var parsed) ? parsed : PageStatus.Draft;

        var post = new BlogPost
        {
            Title = title.Trim(),
            Slug = slug,
            Summary = summary?.Trim(),
            Content = content,
            FeaturedImageUrl = imageUrl,
            Status = postStatus,
            AuthorUserId = user.Id,
            AuthorName = user.FullName,
            MetaDescription = metaDescription?.Trim(),
            Tags = tags?.Trim(),
            PublishedAt = postStatus == PageStatus.Published ? DateTime.UtcNow : null
        };

        _context.BlogPosts.Add(post);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Blog post created!";
        return RedirectToAction(nameof(Edit), new { id = post.Id });
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var post = await _context.BlogPosts.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        if (post == null) return NotFound();
        return View(post);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        Guid id, string title, string slug, string? summary, string content, string? featuredImageUrl,
        IFormFile? featuredImage, string status, string? metaDescription, string? tags)
    {
        var post = await _context.BlogPosts.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        if (post == null) return NotFound();

        var newSlug = NormalizeSlug(slug, title);
        if (!string.Equals(post.Slug, newSlug, StringComparison.OrdinalIgnoreCase))
            newSlug = await EnsureUniqueSlugAsync(newSlug, id);

        var wasPublished = post.Status == PageStatus.Published;
        var postStatus = Enum.TryParse<PageStatus>(status, true, out var parsed) ? parsed : PageStatus.Draft;

        post.Title = title.Trim();
        post.Slug = newSlug;
        post.Summary = summary?.Trim();
        post.Content = content;
        post.MetaDescription = metaDescription?.Trim();
        post.Tags = tags?.Trim();
        post.Status = postStatus;

        var uploaded = await SaveImageAsync(featuredImage);
        if (uploaded != null)
            post.FeaturedImageUrl = uploaded;
        else if (!string.IsNullOrWhiteSpace(featuredImageUrl))
            post.FeaturedImageUrl = featuredImageUrl.Trim();

        if (postStatus == PageStatus.Published && (!wasPublished || post.PublishedAt == null))
            post.PublishedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        TempData["Success"] = "Blog post saved!";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var post = await _context.BlogPosts.FindAsync(id);
        if (post != null)
        {
            post.IsDeleted = true;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Blog post deleted.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePublish(Guid id)
    {
        var post = await _context.BlogPosts.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        if (post == null) return NotFound();

        if (post.Status == PageStatus.Published)
        {
            post.Status = PageStatus.Draft;
        }
        else
        {
            post.Status = PageStatus.Published;
            post.PublishedAt ??= DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = post.Status == PageStatus.Published ? "Post published!" : "Post moved to draft.";
        return RedirectToAction(nameof(Index));
    }

    private static string NormalizeSlug(string slug, string title)
    {
        var value = string.IsNullOrWhiteSpace(slug) ? title : slug;
        value = value.Trim().ToLowerInvariant();
        value = Regex.Replace(value, @"[^a-z0-9\s-]", "");
        value = Regex.Replace(value, @"\s+", "-");
        value = Regex.Replace(value, @"-+", "-").Trim('-');
        return string.IsNullOrEmpty(value) ? $"post-{Guid.NewGuid():N}"[..12] : value;
    }

    private async Task<string> EnsureUniqueSlugAsync(string slug, Guid? excludeId = null)
    {
        var candidate = slug;
        var counter = 1;
        while (await _context.BlogPosts.AnyAsync(p =>
            p.Slug == candidate && !p.IsDeleted && (excludeId == null || p.Id != excludeId)))
        {
            candidate = $"{slug}-{counter++}";
        }
        return candidate;
    }

    private async Task<string?> SaveImageAsync(IFormFile? file)
    {
        if (file == null || file.Length == 0) return null;

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not ".jpg" and not ".jpeg" and not ".png" and not ".webp" and not ".gif")
            return null;

        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "blog");
        Directory.CreateDirectory(uploadsDir);
        var fileName = $"{Guid.NewGuid():N}{ext}";
        var path = Path.Combine(uploadsDir, fileName);

        await using var stream = new FileStream(path, FileMode.Create);
        await file.CopyToAsync(stream);

        return $"/uploads/blog/{fileName}";
    }
}
