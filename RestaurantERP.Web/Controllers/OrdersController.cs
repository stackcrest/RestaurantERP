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

    public OrdersController(
        UserManager<ApplicationUser> userManager,
        IApplicationDbContext context,
        IConfiguration config,
        IDeliveryZoneService deliveryZoneService,
        IOrderTrackingService trackingService,
        ICommissionService commissionService,
        IWhatsAppIntegrationService whatsApp)
        : base(userManager, context)
    {
        _config = config;
        _deliveryZoneService = deliveryZoneService;
        _trackingService = trackingService;
        _commissionService = commissionService;
        _whatsApp = whatsApp;
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

        var cartCount = await _context.CartItems.Where(c => c.UserId == user.Id).CountAsync();
        if (cartCount == 0) return RedirectToAction("Index", "Cart");

        var tables = await _context.Tables.Include(t => t.Section)
            .Where(t => t.Status == TableStatus.Available).ToListAsync();

        ViewBag.Tables = tables;
        ViewBag.User = user;
        ViewBag.DeliveryConfig = await _deliveryZoneService.GetConfigAsync();
        return View();
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
        double? latitude, double? longitude, string? notes)
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

        var discount = subTotal >= discountThreshold ? Math.Round(subTotal * discountPercent / 100, 2) : 0;
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
        await _context.SaveChangesAsync();

        await _trackingService.RecordStatusAsync(order.Id, OrderStatus.Placed, user.Id, user.FullName, "Order placed by customer");
        await _commissionService.ApplyCommissionAsync(order);

        TempData["Success"] = "Order placed successfully!";
        return RedirectToAction("Details", new { id = order.Id });
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(Guid id)
    {
        if (!await CanPlaceCustomerOrdersAsync())
            return StaffOrderBlockedResult();

        var user = await GetCurrentUserAsync();
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id && o.UserId == user!.Id);

        if (order != null && order.Status < OrderStatus.Preparing)
        {
            order.Status = OrderStatus.Cancelled;
            await _context.SaveChangesAsync();
            await _trackingService.RecordStatusAsync(order.Id, OrderStatus.Cancelled, user!.Id, user.FullName, "Cancelled by customer");
            TempData["Success"] = "Order cancelled.";
        }
        else
        {
            TempData["Error"] = "Order cannot be cancelled at this stage.";
        }
        return RedirectToAction("Details", new { id });
    }
}

public class DeliveryCheckRequest
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
