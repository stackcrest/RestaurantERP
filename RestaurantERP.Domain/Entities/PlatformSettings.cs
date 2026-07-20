using RestaurantERP.Domain.Common;
using RestaurantERP.Domain.Enums;

namespace RestaurantERP.Domain.Entities;

public class ApplicationSetting : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Group { get; set; }
    public string? Description { get; set; }
    public bool IsPublic { get; set; }
}

public class ThemeSetting : BaseEntity
{
    public string PrimaryColor { get; set; } = "#E63946";
    public string SecondaryColor { get; set; } = "#1D3557";
    public string AccentColor { get; set; } = "#F4A261";
    public string BackgroundColor { get; set; } = "#FFFFFF";
    public string SurfaceColor { get; set; } = "#F8F9FA";
    public string TextPrimary { get; set; } = "#212529";
    public string TextSecondary { get; set; } = "#6C757D";
    public string FontFamily { get; set; } = "Inter, system-ui, sans-serif";
    public string BorderRadius { get; set; } = "12px";
    public string ButtonStyle { get; set; } = "rounded";
    public string? LogoUrl { get; set; }
    public string? FaviconUrl { get; set; }
    public bool DarkMode { get; set; }
    public bool IsPublished { get; set; } = true;
    public int Version { get; set; } = 1;
    public string? RestaurantName { get; set; }
    public string? Tagline { get; set; }
}

public class CommissionRule : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
    public OrderType? OrderType { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public bool IsActive { get; set; } = true;
    public int Priority { get; set; }
}

public class CommissionLedger : BaseEntity
{
    public Guid OrderId { get; set; }
    public Guid CommissionRuleId { get; set; }
    public decimal OrderAmount { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal CommissionPercentage { get; set; }
    public bool IsSettled { get; set; }
    public Guid? SettlementId { get; set; }

    public Order Order { get; set; } = null!;
    public CommissionRule CommissionRule { get; set; } = null!;
    public Settlement? Settlement { get; set; }
}

public class Settlement : BaseEntity
{
    public string SettlementNumber { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal TotalCommission { get; set; }
    public SettlementStatus Status { get; set; } = SettlementStatus.Pending;
    public SettlementCycle Cycle { get; set; }

    public ICollection<CommissionLedger> CommissionEntries { get; set; } = new List<CommissionLedger>();
}
