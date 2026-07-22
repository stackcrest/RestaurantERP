using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RestaurantERP.Application.Interfaces;
using RestaurantERP.Domain.Entities;
using RestaurantERP.Domain.Enums;

namespace RestaurantERP.Web.Controllers;

[Authorize]
public class OrdersController : BaseController
{
    private readonly IConfiguration _config;
    private readonly IDeliveryZoneService _deliveryZoneService;
    private readonly IOrderTrackingService _trackingService;
    private readonly ICommissionService _commissionService;
    private readonly IWhatsAppIntegrationService _whatsApp;
    private readonly ICouponService _offers;

    public OrdersController(
        UserManager<ApplicationUser> userManager,
        IApplicationDbContext context,
        IConfiguration config,
        IDeliveryZoneService deliveryZoneService,
        IOrderTrackingService trackingService,
        ICommissionService commissionService,
        IWhatsAppIntegrationService whatsApp,
        ICouponService offers)
        : base(userManager, context)
    {
        _config = config;
        _deliveryZoneService = deliveryZoneService;
        _trackingService = trackingService;
        _commissionService = commissionService;
        _whatsApp = whatsApp;
        _offers = offers;
    }

    public async Task<IActionResult> Index()
    {
        if (!await CanPlaceCustomerOrdersAsync())
            return StaffOrderBlockedResult();

        var user = await GetCurrentUserAsync();
        if (user == null) return RedirectToAction("Login", "Account");

        var orders = await _context.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == user.Id)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return View(orders);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        if (!await CanPlaceCustomerOrdersAsync())
            return StaffOrderBlockedResult();

        var user = await GetCurrentUserAsync();
        if (user == null) return RedirectToAction("Login", "Account");

        var order = await _context.Orders
            .Include(o => o.Items).ThenInclude(i => i.AddOns)
            .Include(o => o.Table)
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == user.Id);

        if (order == null) return NotFound();
        ViewBag.Timeline = await _trackingService.GetTimelineAsync(id);
        ViewBag.WhatsAppUrl = _whatsApp.GetOrderChatUrl(order.OrderNumber, user.FullName, user.Email, null);
        return View(order);
    }

    public async Task<IActionResult> Checkout()
    {
        if (!await CanPlaceCustomerOrdersAsync())
            return StaffOrderBlockedResult();

        var user = await GetCurrentUserAsync();
        if (user == null) return RedirectToAction("Login", "Account");

        var cartItems = await _context.CartItems
            .Include(c => c.MenuItem)
            .Include(c => c.Variant)
            .Where(c => c.UserId == user.Id)
            .ToListAsync();

        if (!cartItems.Any()) return RedirectToAction("Index", "Cart");

        var addOns = await _context.AddOns.ToListAsync();
        decimal subTotal = 0;
        foreach (var item in cartItems)
        {
            var unitPrice = item.MenuItem.BasePrice + (item.Variant?.PriceAdjustment ?? 0);
            if (!string.IsNullOrEmpty(item.AddOnIds))
            {
                var ids = item.AddOnIds.Split(',').Select(Guid.Parse).ToList();
                unitPrice += addOns.Where(a => ids.Contains(a.Id)).Sum(a => a.Price);
            }
            subTotal += unitPrice * item.Quantity;
        }

        var discountThreshold = _config.GetValue<decimal>("BusinessRules:DiscountThresholdAmount", 500);
        var discountPercent = _config.GetValue<decimal>("BusinessRules:DiscountPercentage", 5);
        var cgst = _config.GetValue<decimal>("BusinessRules:CGST", 2.5m);
        var sgst = _config.GetValue<decimal>("BusinessRules:SGST", 2.5m);
        var deliveryFee = _config.GetValue<decimal>("BusinessRules:DeliveryFee", 40);

        var autoDiscount = subTotal >= discountThreshold ? Math.Round(subTotal * discountPercent / 100, 2) : 0;
        var taxable = subTotal - autoDiscount;
        var tax = Math.Round(taxable * (cgst + sgst) / 100, 2);

        var tables = await _context.Tables.Include(t => t.Section)
            .Where(t => t.Status == TableStatus.Available).ToListAsync();

        ViewBag.Tables = tables;
        ViewBag.User = user;
        ViewBag.DeliveryConfig = await _deliveryZoneService.GetConfigAsync();
        ViewBag.SubTotal = subTotal;
        ViewBag.AutoDiscount = autoDiscount;
        ViewBag.Tax = tax;
        ViewBag.DeliveryFee = deliveryFee;
        ViewBag.DiscountThreshold = discountThreshold;
        ViewBag.DiscountPercent = discountPercent;
        ViewBag.TakeawayTotal = taxable + tax;
        ViewBag.DeliveryTotal = taxable + tax + deliveryFee;
        return View();
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> PreviewTotals([FromBody] PreviewTotalsRequest request)
    {
        if (!await CanPlaceCustomerOrdersAsync())
            return StaffOrderBlockedResult(json: true);

        var user = await GetCurrentUserAsync();
        if (user == null) return Unauthorized();

        var cartItems = await _context.CartItems
            .Include(c => c.MenuItem)
            .Include(c => c.Variant)
            .Where(c => c.UserId == user.Id)
            .ToListAsync();

        var addOns = await _context.AddOns.ToListAsync();
        decimal subTotal = 0;
        foreach (var item in cartItems)
        {
            var unitPrice = item.MenuItem.BasePrice + (item.Variant?.PriceAdjustment ?? 0);
            if (!string.IsNullOrEmpty(item.AddOnIds))
            {
                var ids = item.AddOnIds.Split(',').Select(Guid.Parse).ToList();
                unitPrice += addOns.Where(a => ids.Contains(a.Id)).Sum(a => a.Price);
            }
            subTotal += unitPrice * item.Quantity;
        }

        var discountThreshold = _config.GetValue<decimal>("BusinessRules:DiscountThresholdAmount", 500);
        var discountPercent = _config.GetValue<decimal>("BusinessRules:DiscountPercentage", 5);
        var cgst = _config.GetValue<decimal>("BusinessRules:CGST", 2.5m);
        var sgst = _config.GetValue<decimal>("BusinessRules:SGST", 2.5m);
        var deliveryFee = _config.GetValue<decimal>("BusinessRules:DeliveryFee", 40);

        var autoDiscount = subTotal >= discountThreshold ? Math.Round(subTotal * discountPercent / 100, 2) : 0;
        decimal couponDiscount = 0;
        string? couponMessage = null;
        string? appliedCode = null;

        if (!string.IsNullOrWhiteSpace(request.CouponCode))
        {
            var offerResult = await _offers.ValidateCouponAsync(request.CouponCode, subTotal, user.Id);
            if (offerResult.Success)
            {
                couponDiscount = offerResult.Data?.CalculatedDiscount ?? 0;
                appliedCode = offerResult.Data?.Code;
                couponMessage = $"{appliedCode} applied — you save ₹{couponDiscount:N0}";
            }
            else
            {
                couponMessage = offerResult.Message;
            }
        }

        var discount = Math.Max(autoDiscount, couponDiscount);
        var discountLabel = couponDiscount > autoDiscount && appliedCode != null
            ? $"Coupon ({appliedCode})"
            : autoDiscount > 0 ? $"Auto discount ({discountPercent:0}%)" : "Discount";

        var isDelivery = string.Equals(request.OrderType, "Delivery", StringComparison.OrdinalIgnoreCase);
        var delivery = isDelivery ? deliveryFee : 0;
        var taxable = subTotal - discount;
        var tax = Math.Round(taxable * (cgst + sgst) / 100, 2);
        var total = taxable + tax + delivery;

        return Json(new
        {
            success = true,
            subTotal,
            autoDiscount,
            couponDiscount,
            discount,
            discountLabel,
            couponApplied = couponDiscount > 0,
            couponValid = string.IsNullOrWhiteSpace(request.CouponCode) || couponDiscount > 0,
            couponMessage,
            tax,
            deliveryFee = delivery,
            total
        });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> CheckDelivery([FromBody] DeliveryCheckRequest request)
    {
        if (request.Latitude == 0 && request.Longitude == 0)
            return Json(new { success = false, message = "Please enable location access." });

        var result = await _deliveryZoneService.ValidateDeliveryAsync(request.Latitude, request.Longitude);
        var config = await _deliveryZoneService.GetConfigAsync();
        return Json(new
        {
            success = result.CanDeliver,
            canDeliver = result.CanDeliver,
            distanceKm = result.DistanceKm,
            radiusKm = config.RadiusKm,
            message = result.Message
        });
    }

    [HttpPost]
    public async Task<IActionResult> PlaceOrder(
        string orderType, string paymentMethod, Guid? tableId,
        string? address, string? city, string? pinCode, string? landmark,
        double? latitude, double? longitude, string? notes, string? couponCode)
    {
        if (!await CanPlaceCustomerOrdersAsync())
            return StaffOrderBlockedResult();

        var user = await GetCurrentUserAsync();
        if (user == null) return RedirectToAction("Login", "Account");

        var parsedOrderType = Enum.Parse<OrderType>(orderType, true);

        if (parsedOrderType == OrderType.Delivery)
        {
            if (string.IsNullOrWhiteSpace(address) || !latitude.HasValue || !longitude.HasValue)
            {
                TempData["Error"] = "Delivery address and location are required.";
                return RedirectToAction(nameof(Checkout));
            }

            var deliveryCheck = await _deliveryZoneService.ValidateDeliveryAsync(latitude.Value, longitude.Value);
            if (!deliveryCheck.CanDeliver)
            {
                TempData["Error"] = deliveryCheck.Message;
                return RedirectToAction(nameof(Checkout));
            }
        }

        var cartItems = await _context.CartItems
            .Include(c => c.MenuItem).Include(c => c.Variant)
            .Where(c => c.UserId == user.Id).ToListAsync();

        if (!cartItems.Any())
        {
            TempData["Error"] = "Your cart is empty.";
            return RedirectToAction("Index", "Cart");
        }

        var addOns = await _context.AddOns.ToListAsync();
        decimal subTotal = 0;
        var orderItems = new List<OrderItem>();

        foreach (var cart in cartItems)
        {
            var unitPrice = cart.MenuItem.BasePrice + (cart.Variant?.PriceAdjustment ?? 0);
            var itemAddOns = new List<OrderItemAddOn>();

            if (!string.IsNullOrEmpty(cart.AddOnIds))
            {
                var ids = cart.AddOnIds.Split(',').Select(Guid.Parse).ToList();
                foreach (var ao in addOns.Where(a => ids.Contains(a.Id)))
                {
                    unitPrice += ao.Price;
                    itemAddOns.Add(new OrderItemAddOn { AddOnId = ao.Id, AddOnName = ao.Name, Price = ao.Price });
                }
            }

            var oi = new OrderItem
            {
                MenuItemId = cart.MenuItemId,
                MenuItemName = cart.MenuItem.Name,
                VariantId = cart.VariantId,
                VariantName = cart.Variant?.Name,
                Quantity = cart.Quantity,
                UnitPrice = unitPrice,
                TotalPrice = unitPrice * cart.Quantity
            };
            foreach (var ao in itemAddOns) oi.AddOns.Add(ao);
            orderItems.Add(oi);
            subTotal += oi.TotalPrice;
        }

        var discountThreshold = _config.GetValue<decimal>("BusinessRules:DiscountThresholdAmount", 500);
        var discountPercent = _config.GetValue<decimal>("BusinessRules:DiscountPercentage", 5);
        var cgst = _config.GetValue<decimal>("BusinessRules:CGST", 2.5m);
        var sgst = _config.GetValue<decimal>("BusinessRules:SGST", 2.5m);
        var deliveryFee = _config.GetValue<decimal>("BusinessRules:DeliveryFee", 40);

        var autoDiscount = subTotal >= discountThreshold ? Math.Round(subTotal * discountPercent / 100, 2) : 0;
        decimal couponDiscount = 0;
        string? appliedCoupon = null;

        if (!string.IsNullOrWhiteSpace(couponCode))
        {
            var offerResult = await _offers.ValidateCouponAsync(couponCode, subTotal, user.Id);
            if (!offerResult.Success)
            {
                TempData["Error"] = offerResult.Message;
                return RedirectToAction(nameof(Checkout));
            }
            couponDiscount = offerResult.Data?.CalculatedDiscount ?? 0;
            appliedCoupon = offerResult.Data?.Code;
        }

        var discount = Math.Max(autoDiscount, couponDiscount);
        var taxable = subTotal - discount;
        var tax = Math.Round(taxable * (cgst + sgst) / 100, 2);
        var delivery = parsedOrderType == OrderType.Delivery ? deliveryFee : 0;
        var total = taxable + tax + delivery;

        double? distanceKm = null;
        if (parsedOrderType == OrderType.Delivery && latitude.HasValue && longitude.HasValue)
        {
            var check = await _deliveryZoneService.ValidateDeliveryAsync(latitude.Value, longitude.Value);
            distanceKm = check.DistanceKm;
        }

        var fullAddress = parsedOrderType == OrderType.Delivery
            ? string.Join(", ", new[] { address, landmark, city, pinCode }.Where(s => !string.IsNullOrWhiteSpace(s)))
            : address;

        var order = new Order
        {
            OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
            UserId = user.Id,
            TableId = tableId,
            OrderType = parsedOrderType,
            Status = OrderStatus.Placed,
            SubTotal = subTotal,
            DiscountAmount = discount,
            TaxAmount = tax,
            DeliveryFee = delivery,
            TotalAmount = total,
            CouponCode = appliedCoupon,
            DeliveryAddress = fullAddress,
            DeliveryCity = city,
            DeliveryPinCode = pinCode,
            DeliveryLandmark = landmark,
            DeliveryLatitude = latitude,
            DeliveryLongitude = longitude,
            DeliveryDistanceKm = distanceKm,
            Notes = notes,
            PaymentMethod = Enum.Parse<PaymentMethod>(paymentMethod, true),
            PaymentStatus = PaymentStatus.Paid,
            Items = orderItems
        };

        user.Address = address ?? user.Address;
        user.City = city ?? user.City;
        user.PinCode = pinCode ?? user.PinCode;
        await _userManager.UpdateAsync(user);

        _context.Orders.Add(order);
        _context.CartItems.RemoveRange(cartItems);

        if (!string.IsNullOrEmpty(appliedCoupon))
        {
            var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == appliedCoupon && !c.IsDeleted);
            if (coupon != null)
            {
                coupon.UsedCount += 1;
                coupon.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();

        await _trackingService.RecordStatusAsync(order.Id, OrderStatus.Placed, user.Id, user.FullName, "Order placed by customer");
        await _commissionService.ApplyCommissionAsync(order);

        TempData["Success"] = "Order placed successfully!";
        return RedirectToAction("Details", new { id = order.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id, string? returnTo = null)
    {
        if (!await CanPlaceCustomerOrdersAsync())
            return StaffOrderBlockedResult();

        var user = await GetCurrentUserAsync();
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id && o.UserId == user!.Id);

        if (order != null && order.Status == OrderStatus.Placed)
        {
            order.Status = OrderStatus.Cancelled;
            await _context.SaveChangesAsync();
            await _trackingService.RecordStatusAsync(order.Id, OrderStatus.Cancelled, user!.Id, user.FullName, "Cancelled by customer");
            TempData["Success"] = "Order cancelled.";
        }
        else
        {
            TempData["Error"] = "Order can only be cancelled while status is Placed.";
        }

        if (string.Equals(returnTo, "index", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction(nameof(Index));

        return RedirectToAction("Details", new { id });
    }
}

public class DeliveryCheckRequest
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class PreviewTotalsRequest
{
    public string? CouponCode { get; set; }
    public string? OrderType { get; set; }
}
