using RestaurantERP.Application.DTOs.Public;

namespace RestaurantERP.Application.DTOs.SuperAdmin;

public class UpdateThemeDto : ThemeDto;

public class ApplicationSettingDto
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Group { get; set; }
    public string? Description { get; set; }
    public bool IsPublic { get; set; }
}

public class CommissionRuleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
    public string? OrderType { get; set; }
    public string? PaymentMethod { get; set; }
    public bool IsActive { get; set; }
    public int Priority { get; set; }
}

public class CreateCommissionRuleDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
    public string? OrderType { get; set; }
    public string? PaymentMethod { get; set; }
    public int Priority { get; set; }
}

public class UiPageDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Visibility { get; set; } = string.Empty;
    public bool IsHomePage { get; set; }
    public List<PageBlockDto> Blocks { get; set; } = new();
}

public class CreateUiPageDto
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Visibility { get; set; } = "All";
    public bool IsHomePage { get; set; }
}

public class UpdateUiPageDto : CreateUiPageDto
{
    public List<PageBlockDto>? Blocks { get; set; }
}

public class FeatureFlagDto
{
    public Guid Id { get; set; }
    public string FeatureKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsEnabled { get; set; }
}

public class AuditLogDto
{
    public Guid Id { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public DateTime CreatedAt { get; set; }
}
