namespace RestaurantERP.Application.Common;

public static class PosSettingKeys
{
    public const string Gstin = "Pos.Gstin";
    public const string BillPrefix = "Pos.BillPrefix";
    public const string RestaurantPhone = "Pos.RestaurantPhone";
    public const string BillFooterNote = "Pos.BillFooterNote";

    public static readonly string[] All =
    [
        Gstin,
        BillPrefix,
        RestaurantPhone,
        BillFooterNote
    ];
}

public static class PosFeatureKeys
{
    public const string PosEnabled = "pos_enabled";
    public const string OfflineBilling = "pos_offline_billing";
    public const string OnlineBilling = "pos_online_billing";
}

public record PosConfig(string? Gstin, string BillPrefix, string? RestaurantPhone, string? BillFooterNote);

public record PosFeatureState(bool PosEnabled, bool OfflineBilling, bool OnlineBilling);
