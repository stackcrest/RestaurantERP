using RestaurantERP.Domain.Common;
using RestaurantERP.Domain.Enums;

namespace RestaurantERP.Domain.Entities;

public class Reservation : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid? TableId { get; set; }
    public DateTime ReservationDate { get; set; }
    public TimeSpan ReservationTime { get; set; }
    public int PartySize { get; set; }
    public string? SpecialRequests { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

    public ApplicationUser User { get; set; } = null!;
    public RestaurantTable? Table { get; set; }
}
