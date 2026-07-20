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

    public async Task<ApiResponse<CouponDto>> ValidateCouponAsync(string code, decimal orderAmount)
    {
        var coupon = await _context.Coupons.FirstOrDefaultAsync(c =>
            c.Code == code && c.IsActive && c.ValidFrom <= DateTime.UtcNow && c.ValidTo >= DateTime.UtcNow);
        if (coupon == null) return ApiResponse<CouponDto>.Fail("Invalid or expired coupon.");
        if (coupon.MinimumOrderAmount.HasValue && orderAmount < coupon.MinimumOrderAmount)
            return ApiResponse<CouponDto>.Fail($"Minimum order amount is ₹{coupon.MinimumOrderAmount}.");

        return ApiResponse<CouponDto>.Ok(new CouponDto
        {
            Code = coupon.Code, DiscountPercentage = coupon.DiscountPercentage,
            CalculatedDiscount = Math.Round(orderAmount * coupon.DiscountPercentage / 100, 2)
        });
    }

    public async Task<ApiResponse<List<CouponDto>>> GetCouponsAsync()
    {
        var coupons = await _context.Coupons.ToListAsync();
        return ApiResponse<List<CouponDto>>.Ok(coupons.Select(c => new CouponDto
        {
            Id = c.Id, Code = c.Code, Description = c.Description,
            DiscountPercentage = c.DiscountPercentage, IsActive = c.IsActive
        }).ToList());
    }

    public async Task<ApiResponse<CouponDto>> CreateCouponAsync(CreateCouponDto dto)
    {
        var coupon = new Domain.Entities.Coupon
        {
            Code = dto.Code.ToUpper(), Description = dto.Description,
            DiscountPercentage = dto.DiscountPercentage,
            MinimumOrderAmount = dto.MinimumOrderAmount,
            ValidFrom = dto.ValidFrom, ValidTo = dto.ValidTo, UsageLimit = dto.UsageLimit
        };
        _context.Coupons.Add(coupon);
        await _context.SaveChangesAsync();
        return ApiResponse<CouponDto>.Ok(new CouponDto { Id = coupon.Id, Code = coupon.Code });
    }
}

public class ReviewService : IReviewService
{
    private readonly IApplicationDbContext _context;
    public ReviewService(IApplicationDbContext context) => _context = context;

    public async Task<ApiResponse<ReviewDto>> CreateReviewAsync(Guid userId, CreateReviewDto dto)
    {
        var user = await _context.Orders.Select(o => o.User).FirstOrDefaultAsync();
        var review = new Domain.Entities.Review
        {
            UserId = userId, MenuItemId = dto.MenuItemId, OrderId = dto.OrderId,
            Rating = dto.Rating, Comment = dto.Comment
        };
        _context.Reviews.Add(review);

        var item = await _context.MenuItems.FindAsync(dto.MenuItemId);
        if (item != null)
        {
            var reviews = await _context.Reviews.Where(r => r.MenuItemId == dto.MenuItemId).ToListAsync();
            reviews.Add(review);
            item.AverageRating = (decimal)reviews.Average(r => r.Rating);
            item.ReviewCount = reviews.Count;
        }
        await _context.SaveChangesAsync();

        var reviewer = await _context.Orders.Where(o => o.UserId == userId).Select(o => o.User.FullName).FirstOrDefaultAsync();
        return ApiResponse<ReviewDto>.Ok(new ReviewDto
        {
            Id = review.Id, Rating = review.Rating, Comment = review.Comment,
            UserName = reviewer ?? "Customer", CreatedAt = review.CreatedAt
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
