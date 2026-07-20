using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RestaurantERP.Application.Common;
using RestaurantERP.Application.DTOs.Public;
using RestaurantERP.Application.DTOs.SuperAdmin;
using RestaurantERP.Application.DTOs.Auth;
using RestaurantERP.Application.Interfaces;
using RestaurantERP.Domain.Entities;
using RestaurantERP.Domain.Enums;

namespace RestaurantERP.Application.Services;

public class SuperAdminService : ISuperAdminService
{
    private readonly IApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _audit;

    public SuperAdminService(IApplicationDbContext context, UserManager<ApplicationUser> userManager, IAuditService audit)
    {
        _context = context;
        _userManager = userManager;
        _audit = audit;
    }

    public async Task<ApiResponse<ThemeDto>> GetThemeSettingsAsync()
    {
        var theme = await _context.ThemeSettings.OrderByDescending(t => t.Version).FirstOrDefaultAsync();
        return ApiResponse<ThemeDto>.Ok(theme == null ? new ThemeDto() : MapTheme(theme));
    }

    public async Task<ApiResponse<ThemeDto>> UpdateThemeAsync(UpdateThemeDto dto)
    {
        var current = await _context.ThemeSettings.OrderByDescending(t => t.Version).FirstOrDefaultAsync();
        var version = (current?.Version ?? 0) + 1;

        var theme = new ThemeSetting
        {
            PrimaryColor = dto.PrimaryColor, SecondaryColor = dto.SecondaryColor,
            AccentColor = dto.AccentColor, BackgroundColor = dto.BackgroundColor,
            SurfaceColor = dto.SurfaceColor, TextPrimary = dto.TextPrimary,
            TextSecondary = dto.TextSecondary, FontFamily = dto.FontFamily,
            BorderRadius = dto.BorderRadius, ButtonStyle = dto.ButtonStyle,
            LogoUrl = dto.LogoUrl, FaviconUrl = dto.FaviconUrl, DarkMode = dto.DarkMode,
            RestaurantName = dto.RestaurantName, Tagline = dto.Tagline,
            IsPublished = true, Version = version
        };
        if (current != null) current.IsPublished = false;
        _context.ThemeSettings.Add(theme);
        await _context.SaveChangesAsync();
        await _audit.LogAsync("Update", "ThemeSetting", theme.Id.ToString());
        return ApiResponse<ThemeDto>.Ok(MapTheme(theme), "Theme updated.");
    }

    public async Task<ApiResponse<List<ApplicationSettingDto>>> GetSettingsAsync()
    {
        var settings = await _context.ApplicationSettings.OrderBy(s => s.Group).ToListAsync();
        return ApiResponse<List<ApplicationSettingDto>>.Ok(settings.Select(s => new ApplicationSettingDto
        {
            Key = s.Key, Value = s.Value, Group = s.Group, Description = s.Description, IsPublic = s.IsPublic
        }).ToList());
    }

    public async Task<ApiResponse<bool>> UpdateSettingsAsync(List<ApplicationSettingDto> settings)
    {
        foreach (var dto in settings)
        {
            var existing = await _context.ApplicationSettings.FirstOrDefaultAsync(s => s.Key == dto.Key);
            if (existing != null)
            {
                existing.Value = dto.Value;
                existing.IsPublic = dto.IsPublic;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.ApplicationSettings.Add(new ApplicationSetting
                {
                    Key = dto.Key, Value = dto.Value, Group = dto.Group,
                    Description = dto.Description, IsPublic = dto.IsPublic
                });
            }
        }
        await _context.SaveChangesAsync();
        return ApiResponse<bool>.Ok(true, "Settings updated.");
    }

    public async Task<ApiResponse<List<CommissionRuleDto>>> GetCommissionRulesAsync()
    {
        var rules = await _context.CommissionRules.OrderBy(r => r.Priority).ToListAsync();
        return ApiResponse<List<CommissionRuleDto>>.Ok(rules.Select(r => new CommissionRuleDto
        {
            Id = r.Id, Name = r.Name, Percentage = r.Percentage,
            OrderType = r.OrderType?.ToString(), PaymentMethod = r.PaymentMethod?.ToString(),
            IsActive = r.IsActive, Priority = r.Priority
        }).ToList());
    }

    public async Task<ApiResponse<CommissionRuleDto>> CreateCommissionRuleAsync(CreateCommissionRuleDto dto)
    {
        var rule = new CommissionRule
        {
            Name = dto.Name, Percentage = dto.Percentage, Priority = dto.Priority,
            OrderType = string.IsNullOrEmpty(dto.OrderType) ? null : Enum.Parse<OrderType>(dto.OrderType, true),
            PaymentMethod = string.IsNullOrEmpty(dto.PaymentMethod) ? null : Enum.Parse<PaymentMethod>(dto.PaymentMethod, true)
        };
        _context.CommissionRules.Add(rule);
        await _context.SaveChangesAsync();
        return ApiResponse<CommissionRuleDto>.Ok(new CommissionRuleDto
        {
            Id = rule.Id, Name = rule.Name, Percentage = rule.Percentage, Priority = rule.Priority
        });
    }

    public async Task<ApiResponse<List<UiPageDto>>> GetPagesAsync()
    {
        var pages = await _context.UiPages.Include(p => p.Blocks).ToListAsync();
        return ApiResponse<List<UiPageDto>>.Ok(pages.Select(MapPage).ToList());
    }

    public async Task<ApiResponse<UiPageDto>> CreatePageAsync(CreateUiPageDto dto)
    {
        var page = new UiPage
        {
            Title = dto.Title, Slug = dto.Slug,
            Visibility = Enum.Parse<PageVisibility>(dto.Visibility, true),
            IsHomePage = dto.IsHomePage, Status = PageStatus.Draft
        };
        _context.UiPages.Add(page);
        await _context.SaveChangesAsync();
        return ApiResponse<UiPageDto>.Ok(MapPage(page));
    }

    public async Task<ApiResponse<UiPageDto>> UpdatePageAsync(Guid id, UpdateUiPageDto dto)
    {
        var page = await _context.UiPages.Include(p => p.Blocks).FirstOrDefaultAsync(p => p.Id == id);
        if (page == null) return ApiResponse<UiPageDto>.Fail("Page not found.");

        page.Title = dto.Title; page.Slug = dto.Slug;
        page.Visibility = Enum.Parse<PageVisibility>(dto.Visibility, true);
        page.IsHomePage = dto.IsHomePage;
        page.UpdatedAt = DateTime.UtcNow;

        if (dto.Blocks != null)
        {
            _context.UiPageBlocks.RemoveRange(page.Blocks);
            foreach (var b in dto.Blocks)
            {
                page.Blocks.Add(new UiPageBlock
                {
                    BlockType = Enum.Parse<BlockType>(b.BlockType, true),
                    Title = b.Title, Content = b.Content, ImageUrl = b.ImageUrl,
                    LinkUrl = b.LinkUrl, LinkText = b.LinkText, ConfigJson = b.ConfigJson,
                    DisplayOrder = b.DisplayOrder
                });
            }
        }
        await _context.SaveChangesAsync();
        return ApiResponse<UiPageDto>.Ok(MapPage(page));
    }

    public async Task<ApiResponse<bool>> PublishPageAsync(Guid id)
    {
        var page = await _context.UiPages.FindAsync(id);
        if (page == null) return ApiResponse<bool>.Fail("Page not found.");
        page.Status = PageStatus.Published;
        await _context.SaveChangesAsync();
        return ApiResponse<bool>.Ok(true, "Page published.");
    }

    public async Task<ApiResponse<List<FeatureFlagDto>>> GetFeatureFlagsAsync()
    {
        var flags = await _context.FeatureFlags.ToListAsync();
        return ApiResponse<List<FeatureFlagDto>>.Ok(flags.Select(f => new FeatureFlagDto
        {
            Id = f.Id, FeatureKey = f.FeatureKey, Name = f.Name,
            Description = f.Description, IsEnabled = f.IsEnabled
        }).ToList());
    }

    public async Task<ApiResponse<FeatureFlagDto>> UpdateFeatureFlagAsync(string key, bool enabled)
    {
        var flag = await _context.FeatureFlags.FirstOrDefaultAsync(f => f.FeatureKey == key);
        if (flag == null) return ApiResponse<FeatureFlagDto>.Fail("Feature not found.");
        flag.IsEnabled = enabled;
        await _context.SaveChangesAsync();
        return ApiResponse<FeatureFlagDto>.Ok(new FeatureFlagDto
        {
            Id = flag.Id, FeatureKey = flag.FeatureKey, Name = flag.Name, IsEnabled = flag.IsEnabled
        });
    }

    public async Task<ApiResponse<List<UserListDto>>> GetAllUsersAsync()
    {
        var users = await _userManager.Users.ToListAsync();
        var result = new List<UserListDto>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            result.Add(new UserListDto
            {
                Id = u.Id, Email = u.Email ?? "", FullName = u.FullName,
                PhoneNumber = u.PhoneNumber, IsActive = u.IsActive,
                Roles = roles.ToList(), CreatedAt = u.CreatedAt
            });
        }
        return ApiResponse<List<UserListDto>>.Ok(result);
    }

    public async Task<ApiResponse<List<AuditLogDto>>> GetAuditLogsAsync(PagedRequest paging)
    {
        var query = _context.AuditLogs.OrderByDescending(a => a.CreatedAt);
        var total = await query.CountAsync();
        var logs = await query.Skip((paging.Page - 1) * paging.PageSize).Take(paging.PageSize)
            .Select(a => new AuditLogDto
            {
                Id = a.Id, UserEmail = a.UserEmail, Action = a.Action,
                EntityType = a.EntityType, EntityId = a.EntityId, CreatedAt = a.CreatedAt
            }).ToListAsync();

        return ApiResponse<List<AuditLogDto>>.Ok(logs, pagination: new PaginationInfo
        {
            Page = paging.Page, PageSize = paging.PageSize, TotalCount = total
        });
    }

    private static ThemeDto MapTheme(ThemeSetting t) => new()
    {
        PrimaryColor = t.PrimaryColor, SecondaryColor = t.SecondaryColor, AccentColor = t.AccentColor,
        BackgroundColor = t.BackgroundColor, SurfaceColor = t.SurfaceColor,
        TextPrimary = t.TextPrimary, TextSecondary = t.TextSecondary,
        FontFamily = t.FontFamily, BorderRadius = t.BorderRadius, ButtonStyle = t.ButtonStyle,
        LogoUrl = t.LogoUrl, FaviconUrl = t.FaviconUrl, DarkMode = t.DarkMode,
        RestaurantName = t.RestaurantName, Tagline = t.Tagline
    };

    private static UiPageDto MapPage(UiPage p) => new()
    {
        Id = p.Id, Title = p.Title, Slug = p.Slug, Status = p.Status.ToString(),
        Visibility = p.Visibility.ToString(), IsHomePage = p.IsHomePage,
        Blocks = p.Blocks.OrderBy(b => b.DisplayOrder).Select(b => new PageBlockDto
        {
            Id = b.Id, BlockType = b.BlockType.ToString(), Title = b.Title,
            Content = b.Content, ImageUrl = b.ImageUrl, DisplayOrder = b.DisplayOrder
        }).ToList()
    };
}
