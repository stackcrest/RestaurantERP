using RestaurantERP.Domain.Common;
using RestaurantERP.Domain.Enums;

namespace RestaurantERP.Domain.Entities;

public class ContactInquiry : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public ContactInquiryStatus Status { get; set; } = ContactInquiryStatus.New;
    public string? AdminNotes { get; set; }
    public string? IpAddress { get; set; }
    public Guid? HandledByUserId { get; set; }
    public DateTime? HandledAt { get; set; }

    public ApplicationUser? HandledBy { get; set; }
}
