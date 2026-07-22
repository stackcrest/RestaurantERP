using Microsoft.EntityFrameworkCore;
using RestaurantERP.Application.Common;
using RestaurantERP.Application.DTOs.Orders;
using RestaurantERP.Application.Interfaces;
using RestaurantERP.Domain.Enums;

namespace RestaurantERP.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IApplicationDbContext _context;

    public DashboardService(IApplicationDbContext context) => _context = context;

    public async Task<ApiResponse<DashboardSummaryDto>> GetSummaryAsync()
    {
        var today = DateTime.UtcNow.Date;
        var orders = await _context.Orders
            .Include(o => o.Items)
            .Where(o => o.CreatedAt >= today && o.Status != OrderStatus.Cancelled)
            .ToListAsync();

        var topItems = orders.SelectMany(o => o.Items)
            .GroupBy(i => i.MenuItemName)
            .Select(g => new TopItemDto
            {
                Name = g.Key, QuantitySold = g.Sum(i => i.Quantity),
                Revenue = g.Sum(i => i.TotalPrice)
            }).OrderByDescending(t => t.QuantitySold).Take(5).ToList();

        var lowStock = await _context.Ingredients
            .CountAsync(i => i.CurrentStock <= i.LowStockThreshold);

        var tables = await _context.Tables.ToListAsync();

        return ApiResponse<DashboardSummaryDto>.Ok(new DashboardSummaryDto
        {
            TodaySales = orders.Sum(o => o.TotalAmount),
            TodayOrders = orders.Count,
            AverageOrderValue = orders.Any() ? orders.Average(o => o.TotalAmount) : 0,
            PendingOrders = await _context.Orders.CountAsync(o =>
                o.Status == OrderStatus.Placed || o.Status == OrderStatus.Confirmed),
            LowStockItems = lowStock,
            ActiveTables = tables.Count(t => t.Status == TableStatus.Occupied),
            TotalTables = tables.Count,
            TopItems = topItems
        });
    }

    public async Task<ApiResponse<Dictionary<string, object>>> GetSalesChartAsync(int days = 7)
    {
        var start = DateTime.UtcNow.Date.AddDays(-days);
        var data = await _context.Orders
            .Where(o => o.CreatedAt >= start && o.Status != OrderStatus.Cancelled)
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Sales = g.Sum(o => o.TotalAmount), Count = g.Count() })
            .OrderBy(g => g.Date).ToListAsync();

        return ApiResponse<Dictionary<string, object>>.Ok(new Dictionary<string, object>
        {
            ["labels"] = data.Select(d => d.Date.ToString("MMM dd")).ToList(),
            ["sales"] = data.Select(d => d.Sales).ToList(),
            ["orders"] = data.Select(d => d.Count).ToList()
        });
    }
}

public class TableService : ITableService
{
    private readonly IApplicationDbContext _context;
    public TableService(IApplicationDbContext context) => _context = context;

    public async Task<ApiResponse<List<TableDto>>> GetTablesAsync()
    {
        var tables = await _context.Tables.Include(t => t.Section).ToListAsync();
        return ApiResponse<List<TableDto>>.Ok(tables.Select(t => new TableDto
        {
            Id = t.Id, TableNumber = t.TableNumber, Capacity = t.Capacity,
            Status = t.Status.ToString(), SectionName = t.Section.Name
        }).ToList());
    }

    public async Task<ApiResponse<TableDto>> UpdateTableStatusAsync(Guid id, string status)
    {
        var table = await _context.Tables.Include(t => t.Section).FirstOrDefaultAsync(t => t.Id == id);
        if (table == null) return ApiResponse<TableDto>.Fail("Table not found.");
        table.Status = Enum.Parse<TableStatus>(status, true);
        await _context.SaveChangesAsync();
        return ApiResponse<TableDto>.Ok(new TableDto
        {
            Id = table.Id, TableNumber = table.TableNumber, Capacity = table.Capacity,
            Status = table.Status.ToString(), SectionName = table.Section.Name
        });
    }
}

public class ReservationService : IReservationService
{
    private readonly IApplicationDbContext _context;
    public ReservationService(IApplicationDbContext context) => _context = context;

    public async Task<ApiResponse<ReservationDto>> CreateReservationAsync(Guid userId, CreateReservationDto dto)
    {
        var reservation = new Domain.Entities.Reservation
        {
            UserId = userId, TableId = dto.TableId,
            ReservationDate = dto.ReservationDate.Date,
            ReservationTime = TimeSpan.Parse(dto.ReservationTime),
            PartySize = dto.PartySize, SpecialRequests = dto.SpecialRequests,
            Status = ReservationStatus.Pending
        };
        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync();
        return await GetReservationDto(reservation.Id);
    }

    public async Task<ApiResponse<List<ReservationDto>>> GetReservationsAsync(Guid? userId, bool isAdmin)
    {
        var query = _context.Reservations.Include(r => r.User).Include(r => r.Table).AsQueryable();
        if (!isAdmin && userId.HasValue) query = query.Where(r => r.UserId == userId);
        var list = await query.OrderByDescending(r => r.ReservationDate).ToListAsync();
        return ApiResponse<List<ReservationDto>>.Ok(list.Select(MapReservation).ToList());
    }

    public async Task<ApiResponse<ReservationDto>> UpdateReservationStatusAsync(Guid id, string status)
    {
        var r = await _context.Reservations.FindAsync(id);
        if (r == null) return ApiResponse<ReservationDto>.Fail("Reservation not found.");
        r.Status = Enum.Parse<ReservationStatus>(status, true);
        await _context.SaveChangesAsync();
        return await GetReservationDto(id);
    }

    private async Task<ApiResponse<ReservationDto>> GetReservationDto(Guid id)
    {
        var r = await _context.Reservations.Include(x => x.User).Include(x => x.Table).FirstAsync(x => x.Id == id);
        return ApiResponse<ReservationDto>.Ok(MapReservation(r));
    }

    private static ReservationDto MapReservation(Domain.Entities.Reservation r) => new()
    {
        Id = r.Id, ReservationDate = r.ReservationDate,
        ReservationTime = r.ReservationTime.ToString(@"hh\:mm"),
        PartySize = r.PartySize, Status = r.Status.ToString(),
        TableNumber = r.Table?.TableNumber, CustomerName = r.User.FullName,
        SpecialRequests = r.SpecialRequests
    };
}

public class CouponService : ICouponService
{
    private readonly IApplicationDbContext _context;
    public CouponService(IApplicationDbContext context) => _context = context;

    public async Task<ApiResponse<CouponDto>> ValidateCouponAsync(string code, decimal orderAmount, Guid? userId = null)
    {
        var coupon = await _context.Coupons.FirstOrDefaultAsync(c =>
            c.Code == code.Trim().ToUpper() && !c.IsDeleted && c.IsActive
            && c.ValidFrom <= DateTime.UtcNow && c.ValidTo >= DateTime.UtcNow);

        if (coupon == null)
            return ApiResponse<CouponDto>.Fail("Invalid or expired offer code.");

        if (coupon.UsageLimit.HasValue && coupon.UsedCount >= coupon.UsageLimit.Value)
            return ApiResponse<CouponDto>.Fail("This offer has reached its usage limit.");

        if (coupon.MinimumOrderAmount.HasValue && orderAmount < coupon.MinimumOrderAmount)
            return ApiResponse<CouponDto>.Fail($"Minimum order amount is ₹{coupon.MinimumOrderAmount:N0}.");

        if (coupon.FirstOrderOnly && userId.HasValue)
        {
            var hasPriorOrder = await _context.Orders.AnyAsync(o =>
                o.UserId == userId.Value && o.Status != Domain.Enums.OrderStatus.Cancelled);
            if (hasPriorOrder)
                return ApiResponse<CouponDto>.Fail("This offer is only valid on your first order.");
        }

        var discount = Math.Round(orderAmount * coupon.DiscountPercentage / 100, 2);
        if (coupon.MaxDiscountAmount.HasValue)
            discount = Math.Min(discount, coupon.MaxDiscountAmount.Value);

        return ApiResponse<CouponDto>.Ok(Map(coupon, discount));
    }

    public async Task<ApiResponse<List<CouponDto>>> GetCouponsAsync()
    {
        var coupons = await _context.Coupons
            .Where(c => !c.IsDeleted)
            .OrderByDescending(c => c.Priority)
            .ThenByDescending(c => c.CreatedAt)
            .ToListAsync();
        return ApiResponse<List<CouponDto>>.Ok(coupons.Select(c => Map(c)).ToList());
    }

    public async Task<ApiResponse<CouponDto>> GetByIdAsync(Guid id)
    {
        var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
        if (coupon == null) return ApiResponse<CouponDto>.Fail("Offer not found.");
        return ApiResponse<CouponDto>.Ok(Map(coupon));
    }

    public async Task<ApiResponse<CouponDto>> CreateCouponAsync(CreateCouponDto dto)
    {
        var code = dto.Code.Trim().ToUpperInvariant();
        if (await _context.Coupons.AnyAsync(c => c.Code == code && !c.IsDeleted))
            return ApiResponse<CouponDto>.Fail("An offer with this code already exists.");

        var coupon = ApplyDto(new Domain.Entities.Coupon(), dto, code);
        _context.Coupons.Add(coupon);
        await _context.SaveChangesAsync();
        return ApiResponse<CouponDto>.Ok(Map(coupon), "Offer created.");
    }

    public async Task<ApiResponse<CouponDto>> UpdateCouponAsync(Guid id, CreateCouponDto dto)
    {
        var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
        if (coupon == null) return ApiResponse<CouponDto>.Fail("Offer not found.");

        var code = dto.Code.Trim().ToUpperInvariant();
        if (await _context.Coupons.AnyAsync(c => c.Code == code && c.Id != id && !c.IsDeleted))
            return ApiResponse<CouponDto>.Fail("An offer with this code already exists.");

        ApplyDto(coupon, dto, code);
        coupon.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return ApiResponse<CouponDto>.Ok(Map(coupon), "Offer updated.");
    }

    public async Task<ApiResponse<bool>> ToggleActiveAsync(Guid id)
    {
        var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
        if (coupon == null) return ApiResponse<bool>.Fail("Offer not found.");
        coupon.IsActive = !coupon.IsActive;
        coupon.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return ApiResponse<bool>.Ok(coupon.IsActive, coupon.IsActive ? "Offer activated." : "Offer paused.");
    }

    public async Task<ApiResponse<bool>> DeleteCouponAsync(Guid id)
    {
        var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
        if (coupon == null) return ApiResponse<bool>.Fail("Offer not found.");
        coupon.IsDeleted = true;
        coupon.IsActive = false;
        coupon.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return ApiResponse<bool>.Ok(true, "Offer deleted.");
    }

    public async Task<CouponDto?> GetActiveHomepageOfferAsync()
    {
        var now = DateTime.UtcNow;
        var offer = await _context.Coupons
            .Where(c => !c.IsDeleted && c.IsActive && c.ShowOnHomepage
                && c.ValidFrom <= now && c.ValidTo >= now
                && (!c.UsageLimit.HasValue || c.UsedCount < c.UsageLimit.Value))
            .OrderByDescending(c => c.Priority)
            .ThenByDescending(c => c.DiscountPercentage)
            .FirstOrDefaultAsync();
        return offer == null ? null : Map(offer);
    }

    private static Domain.Entities.Coupon ApplyDto(Domain.Entities.Coupon coupon, CreateCouponDto dto, string code)
    {
        coupon.Code = code;
        coupon.Title = string.IsNullOrWhiteSpace(dto.Title) ? code : dto.Title.Trim();
        coupon.Description = dto.Description?.Trim();
        coupon.Subtitle = dto.Subtitle?.Trim();
        coupon.BadgeText = dto.BadgeText?.Trim();
        coupon.CtaText = dto.CtaText?.Trim();
        coupon.DiscountPercentage = dto.DiscountPercentage;
        coupon.MaxDiscountAmount = dto.MaxDiscountAmount;
        coupon.MinimumOrderAmount = dto.MinimumOrderAmount;
        coupon.ValidFrom = dto.ValidFrom.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(dto.ValidFrom, DateTimeKind.Utc) : dto.ValidFrom.ToUniversalTime();
        coupon.ValidTo = dto.ValidTo.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(dto.ValidTo, DateTimeKind.Utc) : dto.ValidTo.ToUniversalTime();
        coupon.UsageLimit = dto.UsageLimit;
        coupon.ShowOnHomepage = dto.ShowOnHomepage;
        coupon.FirstOrderOnly = dto.FirstOrderOnly;
        coupon.IsActive = dto.IsActive;
        coupon.Priority = dto.Priority;
        return coupon;
    }

    private static CouponDto Map(Domain.Entities.Coupon c, decimal? calculatedDiscount = null)
    {
        var now = DateTime.UtcNow;
        return new CouponDto
        {
            Id = c.Id,
            Code = c.Code,
            Title = c.Title ?? c.Code,
            Description = c.Description,
            Subtitle = c.Subtitle,
            BadgeText = c.BadgeText,
            CtaText = c.CtaText,
            DiscountPercentage = c.DiscountPercentage,
            MaxDiscountAmount = c.MaxDiscountAmount,
            MinimumOrderAmount = c.MinimumOrderAmount,
            ValidFrom = c.ValidFrom,
            ValidTo = c.ValidTo,
            UsageLimit = c.UsageLimit,
            UsedCount = c.UsedCount,
            IsActive = c.IsActive,
            ShowOnHomepage = c.ShowOnHomepage,
            FirstOrderOnly = c.FirstOrderOnly,
            Priority = c.Priority,
            CalculatedDiscount = calculatedDiscount,
            IsCurrentlyValid = c.IsActive && !c.IsDeleted && c.ValidFrom <= now && c.ValidTo >= now
                && (!c.UsageLimit.HasValue || c.UsedCount < c.UsageLimit.Value)
        };
    }
}

public class ReviewService : IReviewService
{
    private readonly IApplicationDbContext _context;
    public ReviewService(IApplicationDbContext context) => _context = context;

    public async Task<ApiResponse<ReviewDto>> CreateReviewAsync(Guid userId, CreateReviewDto dto)
    {
        var review = new Domain.Entities.Review
        {
            UserId = userId,
            MenuItemId = dto.MenuItemId,
            OrderId = dto.OrderId,
            Rating = Math.Clamp(dto.Rating, 1, 5),
            Comment = dto.Comment
        };
        _context.Reviews.Add(review);

        var item = await _context.MenuItems.FindAsync(dto.MenuItemId);
        if (item != null)
        {
            var reviews = await _context.Reviews.Where(r => r.MenuItemId == dto.MenuItemId && !r.IsDeleted).ToListAsync();
            reviews.Add(review);
            item.AverageRating = (decimal)reviews.Average(r => r.Rating);
            item.ReviewCount = reviews.Count;
            item.UpdatedAt = DateTime.UtcNow;
        }
        await _context.SaveChangesAsync();

        var reviewer = await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync();

        return ApiResponse<ReviewDto>.Ok(new ReviewDto
        {
            Id = review.Id,
            Rating = review.Rating,
            Comment = review.Comment,
            UserName = reviewer ?? "Customer",
            CreatedAt = review.CreatedAt
        });
    }

    public async Task<ApiResponse<List<ReviewDto>>> GetItemReviewsAsync(Guid menuItemId, PagedRequest paging)
    {
        var query = _context.Reviews.Include(r => r.User).Where(r => r.MenuItemId == menuItemId);
        var total = await query.CountAsync();
        var reviews = await query.OrderByDescending(r => r.CreatedAt)
            .Skip((paging.Page - 1) * paging.PageSize).Take(paging.PageSize)
            .Select(r => new ReviewDto
            {
                Id = r.Id, UserName = r.User.FullName, Rating = r.Rating,
                Comment = r.Comment, CreatedAt = r.CreatedAt
            }).ToListAsync();

        return ApiResponse<List<ReviewDto>>.Ok(reviews, pagination: new PaginationInfo
        {
            Page = paging.Page, PageSize = paging.PageSize, TotalCount = total
        });
    }
}
