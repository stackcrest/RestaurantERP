using Microsoft.EntityFrameworkCore;
using RestaurantERP.Application.Interfaces;
using RestaurantERP.Domain.Entities;
using RestaurantERP.Domain.Enums;

namespace RestaurantERP.Application.Services;

public class ContactInquiryService : IContactInquiryService
{
    private readonly IApplicationDbContext _context;

    public ContactInquiryService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ContactInquiry> SubmitAsync(string name, string email, string? phone, string subject, string message, string? ipAddress, CancellationToken ct = default)
    {
        var inquiry = new ContactInquiry
        {
            Name = name.Trim(),
            Email = email.Trim(),
            Phone = phone?.Trim(),
            Subject = subject.Trim(),
            Message = message.Trim(),
            IpAddress = ipAddress,
            Status = ContactInquiryStatus.New
        };

        _context.ContactInquiries.Add(inquiry);
        await _context.SaveChangesAsync(ct);
        return inquiry;
    }

    public async Task<List<ContactInquiry>> GetInquiriesAsync(ContactInquiryStatus? status = null, CancellationToken ct = default)
    {
        var query = _context.ContactInquiries
            .Include(c => c.HandledBy)
            .Where(c => !c.IsDeleted);

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        return await query.OrderByDescending(c => c.CreatedAt).ToListAsync(ct);
    }

    public async Task<ContactInquiry?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.ContactInquiries
            .Include(c => c.HandledBy)
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, ct);

    public async Task UpdateAsync(Guid id, ContactInquiryStatus status, string? adminNotes, Guid? handledByUserId, CancellationToken ct = default)
    {
        var inquiry = await _context.ContactInquiries.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, ct)
            ?? throw new InvalidOperationException("Contact inquiry not found.");

        inquiry.Status = status;
        inquiry.AdminNotes = adminNotes?.Trim();
        if (status is ContactInquiryStatus.Resolved or ContactInquiryStatus.Closed)
        {
            inquiry.HandledByUserId = handledByUserId;
            inquiry.HandledAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var inquiry = await _context.ContactInquiries.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, ct)
            ?? throw new InvalidOperationException("Contact inquiry not found.");
        inquiry.IsDeleted = true;
        await _context.SaveChangesAsync(ct);
    }

    public async Task<Dictionary<ContactInquiryStatus, int>> GetStatusCountsAsync(CancellationToken ct = default)
    {
        var counts = await _context.ContactInquiries
            .Where(c => !c.IsDeleted)
            .GroupBy(c => c.Status)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return Enum.GetValues<ContactInquiryStatus>()
            .ToDictionary(s => s, s => counts.FirstOrDefault(c => c.Key == s)?.Count ?? 0);
    }
}
