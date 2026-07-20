using RestaurantERP.Domain.Entities;
using RestaurantERP.Domain.Enums;

namespace RestaurantERP.Application.Interfaces;

public interface IContactInquiryService
{
    Task<ContactInquiry> SubmitAsync(string name, string email, string? phone, string subject, string message, string? ipAddress, CancellationToken ct = default);
    Task<List<ContactInquiry>> GetInquiriesAsync(ContactInquiryStatus? status = null, CancellationToken ct = default);
    Task<ContactInquiry?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task UpdateAsync(Guid id, ContactInquiryStatus status, string? adminNotes, Guid? handledByUserId, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Dictionary<ContactInquiryStatus, int>> GetStatusCountsAsync(CancellationToken ct = default);
}
