using RestaurantERP.Domain.Common;

namespace RestaurantERP.Domain.Entities;

public class Supplier : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public decimal OutstandingBalance { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<GRN> GRNs { get; set; } = new List<GRN>();
    public ICollection<SupplierLedger> LedgerEntries { get; set; } = new List<SupplierLedger>();
}

public class SupplierLedger : BaseEntity
{
    public Guid SupplierId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Balance { get; set; }
    public Guid? GRNId { get; set; }

    public Supplier Supplier { get; set; } = null!;
}

public class GRN : BaseEntity
{
    public string GRNNumber { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public DateTime ReceivedDate { get; set; } = DateTime.UtcNow;
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }

    public Supplier Supplier { get; set; } = null!;
    public ICollection<GRNItem> Items { get; set; } = new List<GRNItem>();
}

public class GRNItem : BaseEntity
{
    public Guid GRNId { get; set; }
    public Guid IngredientId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime? ExpiryDate { get; set; }

    public GRN GRN { get; set; } = null!;
    public Ingredient Ingredient { get; set; } = null!;
}
