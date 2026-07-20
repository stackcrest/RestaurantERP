using Microsoft.EntityFrameworkCore;
using RestaurantERP.Application.Common;
using RestaurantERP.Application.Interfaces;
using RestaurantERP.Application.Services;
using RestaurantERP.Domain.Entities;
using RestaurantERP.Domain.Enums;

namespace RestaurantERP.Application.Services;

public class PosBillingService : IPosBillingService
{
    private readonly IApplicationDbContext _context;
    private readonly OrderCalculationHelper _calc;
    private readonly IOrderTrackingService _tracking;

    public PosBillingService(IApplicationDbContext context, OrderCalculationHelper calc, IOrderTrackingService tracking)
    {
        _context = context;
        _calc = calc;
        _tracking = tracking;
    }

    public async Task<PosFeatureState> GetFeatureStateAsync()
    {
        var flags = await _context.FeatureFlags
            .Where(f => f.FeatureKey == PosFeatureKeys.PosEnabled
                || f.FeatureKey == PosFeatureKeys.OfflineBilling
                || f.FeatureKey == PosFeatureKeys.OnlineBilling)
            .ToDictionaryAsync(f => f.FeatureKey, f => f.IsEnabled);

        return new PosFeatureState(
            flags.GetValueOrDefault(PosFeatureKeys.PosEnabled, true),
            flags.GetValueOrDefault(PosFeatureKeys.OfflineBilling, true),
            flags.GetValueOrDefault(PosFeatureKeys.OnlineBilling, true));
    }

    public async Task<bool> IsPosEnabledAsync() => (await GetFeatureStateAsync()).PosEnabled;

    public async Task<PosConfig> GetConfigAsync()
    {
        var settings = await _context.ApplicationSettings
            .Where(s => PosSettingKeys.All.Contains(s.Key))
            .ToDictionaryAsync(s => s.Key, s => s.Value);

        return new PosConfig(
            settings.GetValueOrDefault(PosSettingKeys.Gstin),
            settings.GetValueOrDefault(PosSettingKeys.BillPrefix) ?? "BILL",
            settings.GetValueOrDefault(PosSettingKeys.RestaurantPhone),
            settings.GetValueOrDefault(PosSettingKeys.BillFooterNote));
    }

    public async Task SaveConfigAsync(PosConfig config)
    {
        await UpsertSettingAsync(PosSettingKeys.Gstin, config.Gstin ?? "", "POS");
        await UpsertSettingAsync(PosSettingKeys.BillPrefix, config.BillPrefix, "POS");
        await UpsertSettingAsync(PosSettingKeys.RestaurantPhone, config.RestaurantPhone ?? "", "POS");
        await UpsertSettingAsync(PosSettingKeys.BillFooterNote, config.BillFooterNote ?? "", "POS");
        await _context.SaveChangesAsync();
    }

    public async Task<(bool Success, string Message, Guid? BillId)> CreateOfflineBillAsync(
        PosCheckoutRequest request, Guid userId, string userName)
    {
        var features = await GetFeatureStateAsync();
        if (!features.PosEnabled || !features.OfflineBilling)
            return (false, "Offline POS billing is disabled by SuperAdmin.", null);

        if (!request.Items.Any())
            return (false, "Add at least one item to the bill.", null);

        var addOns = await _context.AddOns.ToListAsync();
        decimal subTotal = 0;
        var orderItems = new List<OrderItem>();

        foreach (var item in request.Items)
        {
            var menuItem = await _context.MenuItems.FindAsync(item.MenuItemId);
            if (menuItem == null || !menuItem.IsAvailable)
                return (false, $"Item not found or unavailable.", null);

            var variant = item.VariantId.HasValue
                ? await _context.MenuItemVariants.FindAsync(item.VariantId.Value)
                : null;

            var unitPrice = menuItem.BasePrice + (variant?.PriceAdjustment ?? 0);
            var itemAddOns = new List<OrderItemAddOn>();

            if (item.AddOnIds?.Any() == true)
            {
                foreach (var ao in addOns.Where(a => item.AddOnIds.Contains(a.Id)))
                {
                    unitPrice += ao.Price;
                    itemAddOns.Add(new OrderItemAddOn { AddOnId = ao.Id, AddOnName = ao.Name, Price = ao.Price });
                }
            }

            var oi = new OrderItem
            {
                MenuItemId = item.MenuItemId,
                MenuItemName = menuItem.Name,
                VariantId = item.VariantId,
                VariantName = variant?.Name,
                Quantity = item.Quantity,
                UnitPrice = unitPrice,
                TotalPrice = unitPrice * item.Quantity
            };
            foreach (var ao in itemAddOns) oi.AddOns.Add(ao);
            orderItems.Add(oi);
            subTotal += oi.TotalPrice;
        }

        var includeDelivery = request.OrderType == OrderType.Delivery;
        var (discount, tax, total) = _calc.Calculate(subTotal, includeDelivery: includeDelivery);
        var taxable = subTotal - discount;
        var cgstRate = _calc.GetCgstRate();
        var sgstRate = _calc.GetSgstRate();
        var cgst = Math.Round(taxable * cgstRate / 100, 2);
        var sgst = Math.Round(taxable * sgstRate / 100, 2);
        var deliveryFee = includeDelivery ? _calc.GetDeliveryFee() : 0;

        var order = new Order
        {
            OrderNumber = await GenerateOrderNumberAsync(),
            UserId = userId,
            TableId = request.TableId,
            OrderType = request.OrderType,
            OrderSource = OrderSource.POS,
            Status = OrderStatus.Completed,
            SubTotal = subTotal,
            DiscountAmount = discount,
            TaxAmount = tax,
            DeliveryFee = deliveryFee,
            TotalAmount = total,
            PaymentMethod = request.PaymentMethod,
            PaymentStatus = PaymentStatus.Paid,
            Notes = request.Notes,
            Items = orderItems
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        await _tracking.RecordStatusAsync(order.Id, OrderStatus.Placed, userId, userName, "POS order placed");
        await _tracking.RecordStatusAsync(order.Id, OrderStatus.Completed, userId, userName, "POS bill settled");

        var bill = await CreateBillForOrderAsync(order, BillSource.Offline, request.CustomerName, request.CustomerPhone, userId, userName, cgst, sgst, request.Notes);
        return (true, "Bill generated successfully.", bill.Id);
    }

    public async Task<(bool Success, string Message, Guid? BillId)> GenerateOnlineBillAsync(
        Guid orderId, Guid userId, string userName)
    {
        var features = await GetFeatureStateAsync();
        if (!features.PosEnabled || !features.OnlineBilling)
            return (false, "Online bill generation is disabled by SuperAdmin.", null);

        var order = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.Items).ThenInclude(i => i.AddOns)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null) return (false, "Order not found.", null);

        var existing = await _context.Bills.FirstOrDefaultAsync(b => b.OrderId == orderId);
        if (existing != null)
            return (true, "Bill already exists.", existing.Id);

        if (order.OrderSource == OrderSource.POS)
            return (false, "This order already has a POS bill.", null);

        var taxable = order.SubTotal - order.DiscountAmount;
        var cgstRate = _calc.GetCgstRate();
        var sgstRate = _calc.GetSgstRate();
        var cgst = Math.Round(taxable * cgstRate / 100, 2);
        var sgst = Math.Round(taxable * sgstRate / 100, 2);

        var bill = await CreateBillForOrderAsync(
            order, BillSource.Online, order.User.FullName, order.User.PhoneNumber, userId, userName, cgst, sgst, order.Notes);

        return (true, "Online bill generated.", bill.Id);
    }

    public async Task<BillPrintDto?> GetBillPrintAsync(Guid billId, bool incrementPrintCount = false)
    {
        var bill = await _context.Bills
            .Include(b => b.Order).ThenInclude(o => o.Items).ThenInclude(i => i.AddOns)
            .Include(b => b.Order).ThenInclude(o => o.Table)
            .Include(b => b.Order).ThenInclude(o => o.User)
            .FirstOrDefaultAsync(b => b.Id == billId);

        if (bill == null) return null;

        if (incrementPrintCount)
        {
            bill.PrintCount++;
            await _context.SaveChangesAsync();
        }

        var config = await GetConfigAsync();
        var theme = await _context.ThemeSettings.Where(t => t.IsPublished).OrderByDescending(t => t.Version).FirstOrDefaultAsync();
        var addressSetting = await _context.ApplicationSettings.FirstOrDefaultAsync(s => s.Key == DeliverySettingKeys.RestaurantAddress);

        return new BillPrintDto
        {
            Id = bill.Id,
            BillNumber = bill.BillNumber,
            OrderNumber = bill.Order.OrderNumber,
            Source = bill.Source,
            OrderType = bill.Order.OrderType,
            RestaurantName = theme?.RestaurantName ?? "Restaurant ERP",
            RestaurantAddress = addressSetting?.Value,
            RestaurantPhone = config.RestaurantPhone,
            Gstin = config.Gstin,
            BillFooterNote = config.BillFooterNote,
            CustomerName = bill.CustomerName ?? bill.Order.User?.FullName,
            CustomerPhone = bill.CustomerPhone ?? bill.Order.User?.PhoneNumber,
            CashierName = bill.GeneratedByName,
            TableNumber = bill.Order.Table?.TableNumber,
            SubTotal = bill.SubTotal,
            DiscountAmount = bill.DiscountAmount,
            CgstAmount = bill.CgstAmount,
            SgstAmount = bill.SgstAmount,
            TaxAmount = bill.TaxAmount,
            DeliveryFee = bill.DeliveryFee,
            TotalAmount = bill.TotalAmount,
            PaymentMethod = bill.PaymentMethod,
            BillDate = bill.CreatedAt,
            Notes = bill.Notes,
            Items = bill.Order.Items.Select(i => new BillLineItemDto
            {
                Name = i.MenuItemName,
                VariantName = i.VariantName,
                AddOns = i.AddOns.Select(a => a.AddOnName).ToList(),
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                TotalPrice = i.TotalPrice
            }).ToList()
        };
    }

    private async Task<Bill> CreateBillForOrderAsync(
        Order order, BillSource source, string? customerName, string? customerPhone,
        Guid userId, string userName, decimal cgst, decimal sgst, string? notes)
    {
        var bill = new Bill
        {
            BillNumber = await GenerateBillNumberAsync(),
            OrderId = order.Id,
            Source = source,
            SubTotal = order.SubTotal,
            DiscountAmount = order.DiscountAmount,
            CgstAmount = cgst,
            SgstAmount = sgst,
            TaxAmount = order.TaxAmount,
            DeliveryFee = order.DeliveryFee,
            TotalAmount = order.TotalAmount,
            PaymentMethod = order.PaymentMethod,
            PaymentStatus = order.PaymentStatus,
            CustomerName = customerName,
            CustomerPhone = customerPhone,
            GeneratedByUserId = userId,
            GeneratedByName = userName,
            Notes = notes
        };

        _context.Bills.Add(bill);

        _context.Payments.Add(new Payment
        {
            OrderId = order.Id,
            Amount = order.TotalAmount,
            Method = order.PaymentMethod,
            Status = order.PaymentStatus,
            TransactionReference = bill.BillNumber
        });

        await _context.SaveChangesAsync();
        return bill;
    }

    private async Task<string> GenerateBillNumberAsync()
    {
        var config = await GetConfigAsync();
        var prefix = config.BillPrefix;
        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var count = await _context.Bills.CountAsync(b => b.CreatedAt.Date == DateTime.UtcNow.Date);
        return $"{prefix}-{today}-{(count + 1):D4}";
    }

    private async Task<string> GenerateOrderNumberAsync()
    {
        var count = await _context.Orders.CountAsync(o => o.CreatedAt.Date == DateTime.UtcNow.Date);
        return $"POS-{DateTime.UtcNow:yyyyMMdd}-{(count + 1):D4}";
    }

    private async Task UpsertSettingAsync(string key, string value, string group)
    {
        var setting = await _context.ApplicationSettings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting == null)
        {
            _context.ApplicationSettings.Add(new ApplicationSetting
            {
                Key = key,
                Value = value,
                Group = group,
                IsPublic = false
            });
        }
        else
        {
            setting.Value = value;
            setting.UpdatedAt = DateTime.UtcNow;
        }
    }
}
