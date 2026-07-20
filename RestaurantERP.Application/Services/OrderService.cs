using Microsoft.EntityFrameworkCore;
using RestaurantERP.Application.Common;
using RestaurantERP.Application.DTOs.Orders;
using RestaurantERP.Application.Interfaces;
using RestaurantERP.Domain.Entities;
using RestaurantERP.Domain.Enums;

namespace RestaurantERP.Application.Services;

public class OrderService : IOrderService
{
    private readonly IApplicationDbContext _context;
    private readonly OrderCalculationHelper _calc;
    private readonly ICommissionService _commissionService;

    public OrderService(IApplicationDbContext context, OrderCalculationHelper calc, ICommissionService commissionService)
    {
        _context = context;
        _calc = calc;
        _commissionService = commissionService;
    }

    public async Task<ApiResponse<OrderDetailDto>> PlaceOrderAsync(Guid userId, PlaceOrderDto dto)
    {
        var cartItems = await _context.CartItems
            .Include(c => c.MenuItem).Include(c => c.Variant)
            .Where(c => c.UserId == userId).ToListAsync();

        if (!cartItems.Any())
            return ApiResponse<OrderDetailDto>.Fail("Your cart is empty.");

        decimal subTotal = 0;
        var orderItems = new List<OrderItem>();

        foreach (var cart in cartItems)
        {
            var unitPrice = cart.MenuItem.BasePrice + (cart.Variant?.PriceAdjustment ?? 0);
            var addOns = new List<OrderItemAddOn>();

            if (!string.IsNullOrEmpty(cart.AddOnIds))
            {
                var ids = cart.AddOnIds.Split(',').Select(Guid.Parse).ToList();
                var addOnEntities = await _context.AddOns.Where(a => ids.Contains(a.Id)).ToListAsync();
                foreach (var ao in addOnEntities)
                {
                    unitPrice += ao.Price;
                    addOns.Add(new OrderItemAddOn { AddOnId = ao.Id, AddOnName = ao.Name, Price = ao.Price });
                }
            }

            subTotal += unitPrice * cart.Quantity;
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
            foreach (var ao in addOns) oi.AddOns.Add(ao);
            orderItems.Add(oi);
        }

        decimal couponDiscount = 0;
        if (!string.IsNullOrEmpty(dto.CouponCode))
        {
            var coupon = await _context.Coupons.FirstOrDefaultAsync(c =>
                c.Code == dto.CouponCode && c.IsActive &&
                c.ValidFrom <= DateTime.UtcNow && c.ValidTo >= DateTime.UtcNow);
            if (coupon != null)
            {
                couponDiscount = Math.Round(subTotal * coupon.DiscountPercentage / 100, 2);
                coupon.UsedCount++;
            }
        }

        var orderType = Enum.Parse<OrderType>(dto.OrderType, true);
        var includeDelivery = orderType == OrderType.Delivery;
        var (discount, tax, total) = _calc.Calculate(subTotal, couponDiscount, true, includeDelivery);
        var deliveryFee = includeDelivery ? _calc.GetDeliveryFee() : 0;

        var order = new Order
        {
            OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
            UserId = userId,
            TableId = dto.TableId,
            OrderType = orderType,
            Status = OrderStatus.Placed,
            SubTotal = subTotal,
            DiscountAmount = discount,
            TaxAmount = tax,
            DeliveryFee = deliveryFee,
            TotalAmount = total + deliveryFee - (includeDelivery ? 0 : 0),
            CouponCode = dto.CouponCode,
            DeliveryAddress = dto.DeliveryAddress,
            Notes = dto.Notes,
            PaymentMethod = Enum.Parse<PaymentMethod>(dto.PaymentMethod, true),
            PaymentStatus = PaymentStatus.Paid,
            Items = orderItems
        };
        order.TotalAmount = subTotal - discount + tax + deliveryFee;

        _context.Orders.Add(order);
        _context.CartItems.RemoveRange(cartItems);
        await _context.SaveChangesAsync();

        await _commissionService.ApplyCommissionAsync(order);
        return await GetOrderAsync(order.Id, userId, false);
    }

    public async Task<ApiResponse<List<OrderListDto>>> GetOrdersAsync(Guid? userId, bool isAdmin, PagedRequest paging)
    {
        var query = _context.Orders.Include(o => o.User).Include(o => o.Items).AsQueryable();
        if (!isAdmin && userId.HasValue) query = query.Where(o => o.UserId == userId);

        var total = await query.CountAsync();
        var orders = await query.OrderByDescending(o => o.CreatedAt)
            .Skip((paging.Page - 1) * paging.PageSize).Take(paging.PageSize)
            .Select(o => new OrderListDto
            {
                Id = o.Id, OrderNumber = o.OrderNumber, OrderType = o.OrderType.ToString(),
                Status = o.Status.ToString(), TotalAmount = o.TotalAmount,
                ItemCount = o.Items.Count, CreatedAt = o.CreatedAt,
                CustomerName = o.User.FullName
            }).ToListAsync();

        return ApiResponse<List<OrderListDto>>.Ok(orders, pagination: new PaginationInfo
        {
            Page = paging.Page, PageSize = paging.PageSize, TotalCount = total
        });
    }

    public async Task<ApiResponse<OrderDetailDto>> GetOrderAsync(Guid id, Guid? userId, bool isAdmin)
    {
        var order = await _context.Orders
            .Include(o => o.User).Include(o => o.Items).ThenInclude(i => i.AddOns)
            .Include(o => o.Table)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return ApiResponse<OrderDetailDto>.Fail("Order not found.");
        if (!isAdmin && userId.HasValue && order.UserId != userId)
            return ApiResponse<OrderDetailDto>.Fail("Order not found.");

        return ApiResponse<OrderDetailDto>.Ok(MapDetail(order));
    }

    public async Task<ApiResponse<OrderDetailDto>> GetOrderByNumberAsync(string orderNumber, Guid? userId)
    {
        var order = await _context.Orders
            .Include(o => o.User).Include(o => o.Items).ThenInclude(i => i.AddOns)
            .Include(o => o.Table)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);

        if (order == null) return ApiResponse<OrderDetailDto>.Fail("Order not found.");
        if (userId.HasValue && order.UserId != userId)
            return ApiResponse<OrderDetailDto>.Fail("Order not found.");

        return ApiResponse<OrderDetailDto>.Ok(MapDetail(order));
    }

    public async Task<ApiResponse<OrderDetailDto>> UpdateOrderStatusAsync(Guid id, string status)
    {
        var order = await _context.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return ApiResponse<OrderDetailDto>.Fail("Order not found.");

        order.Status = Enum.Parse<OrderStatus>(status, true);
        order.UpdatedAt = DateTime.UtcNow;

        if (order.Status == OrderStatus.Confirmed)
            await DeductStockAsync(order);

        await _context.SaveChangesAsync();
        return await GetOrderAsync(id, null, true);
    }

    private async Task DeductStockAsync(Order order)
    {
        foreach (var item in order.Items)
        {
            var recipes = await _context.Recipes
                .Include(r => r.Ingredients).ThenInclude(ri => ri.Ingredient)
                .Where(r => r.MenuItemId == item.MenuItemId).ToListAsync();

            foreach (var recipe in recipes)
            foreach (var ri in recipe.Ingredients)
                ri.Ingredient.CurrentStock -= ri.QuantityRequired * item.Quantity;
        }
    }

    public async Task<ApiResponse<bool>> CancelOrderAsync(Guid id, Guid userId)
    {
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);
        if (order == null) return ApiResponse<bool>.Fail("Order not found.");
        if (order.Status >= OrderStatus.Preparing)
            return ApiResponse<bool>.Fail("Order cannot be cancelled at this stage.");

        order.Status = OrderStatus.Cancelled;
        await _context.SaveChangesAsync();
        return ApiResponse<bool>.Ok(true, "Order cancelled.");
    }

    public async Task<ApiResponse<OrderDetailDto>> ReorderAsync(Guid orderId, Guid userId)
    {
        var order = await _context.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);
        if (order == null) return ApiResponse<OrderDetailDto>.Fail("Order not found.");

        foreach (var item in order.Items)
        {
            _context.CartItems.Add(new CartItem
            {
                UserId = userId, MenuItemId = item.MenuItemId,
                VariantId = item.VariantId, Quantity = item.Quantity
            });
        }
        await _context.SaveChangesAsync();
        return ApiResponse<OrderDetailDto>.Ok(MapDetail(order), "Items added to cart for reorder.");
    }

    private static OrderDetailDto MapDetail(Order o)
    {
        var statuses = o.OrderType == OrderType.Delivery
            ? new[] { OrderStatus.Placed, OrderStatus.Confirmed, OrderStatus.Preparing, OrderStatus.Ready, OrderStatus.OutForDelivery, OrderStatus.Delivered, OrderStatus.Completed }
            : new[] { OrderStatus.Placed, OrderStatus.Confirmed, OrderStatus.Preparing, OrderStatus.Ready, OrderStatus.Served, OrderStatus.Completed };
        var labels = new Dictionary<OrderStatus, string>
        {
            [OrderStatus.Placed] = "Order Placed", [OrderStatus.Confirmed] = "Confirmed",
            [OrderStatus.Preparing] = "Preparing", [OrderStatus.Ready] = "Ready",
            [OrderStatus.OutForDelivery] = "Out for Delivery",
            [OrderStatus.Served] = "Served", [OrderStatus.Delivered] = "Delivered",
            [OrderStatus.Completed] = "Completed"
        };
        var currentIdx = Array.IndexOf(statuses, o.Status);

        return new OrderDetailDto
        {
            Id = o.Id, OrderNumber = o.OrderNumber, OrderType = o.OrderType.ToString(),
            Status = o.Status.ToString(), TotalAmount = o.TotalAmount,
            SubTotal = o.SubTotal, DiscountAmount = o.DiscountAmount, TaxAmount = o.TaxAmount,
            DeliveryFee = o.DeliveryFee, PaymentMethod = o.PaymentMethod.ToString(),
            PaymentStatus = o.PaymentStatus.ToString(), DeliveryAddress = o.DeliveryAddress,
            Notes = o.Notes, TableNumber = o.Table?.TableNumber,
            ItemCount = o.Items.Count, CreatedAt = o.CreatedAt, CustomerName = o.User.FullName,
            Items = o.Items.Select(i => new OrderItemDto
            {
                Id = i.Id, MenuItemName = i.MenuItemName, VariantName = i.VariantName,
                Quantity = i.Quantity, UnitPrice = i.UnitPrice, TotalPrice = i.TotalPrice,
                KitchenStatus = i.KitchenStatus.ToString(),
                AddOns = i.AddOns.Select(a => a.AddOnName).ToList()
            }).ToList(),
            StatusTimeline = statuses.Select((s, idx) => new OrderStatusStepDto
            {
                Status = s.ToString(), Label = labels[s],
                IsCompleted = idx <= currentIdx, IsCurrent = idx == currentIdx
            }).ToList()
        };
    }
}
