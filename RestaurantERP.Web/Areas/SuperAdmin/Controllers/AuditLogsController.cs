using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantERP.Application.Interfaces;

namespace RestaurantERP.Web.Areas.SuperAdmin.Controllers;

[Area("SuperAdmin")]
[Authorize(Roles = "SuperAdmin")]
public class AuditLogsController : Controller
{
    private readonly IApplicationDbContext _context;

    public AuditLogsController(IApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index(string? action, string? entity, int pageNum = 1)
    {
        var query = _context.AuditLogs.AsQueryable();
        
        if (!string.IsNullOrEmpty(action))
            query = query.Where(a => a.Action == action);
        
        if (!string.IsNullOrEmpty(entity))
            query = query.Where(a => a.EntityType == entity);

        var pageSize = 50;
        var total = await query.CountAsync();
        var logs = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((pageNum - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.CurrentPage = pageNum;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
        ViewBag.CurrentAction = action;
        ViewBag.CurrentEntity = entity;
        ViewBag.Actions = await _context.AuditLogs.Select(a => a.Action).Distinct().ToListAsync();
        ViewBag.Entities = await _context.AuditLogs.Select(a => a.EntityType).Distinct().ToListAsync();

        return View(logs);
    }
}
