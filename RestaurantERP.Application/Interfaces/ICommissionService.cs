using RestaurantERP.Domain.Entities;

namespace RestaurantERP.Application.Interfaces;

public interface ICommissionService
{
    Task ApplyCommissionAsync(Order order);
    Task<decimal> GetTotalCommissionAsync(DateTime? from = null, DateTime? to = null);
    Task BackfillMissingCommissionsAsync();
}
