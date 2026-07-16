using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains.Payments;

/// <summary>
/// A configurable currency for tuition payments/invoices (System Config →
/// Currencies). The payment plan stores the <see cref="Code"/> as a string, so
/// currencies can be added/removed without migrations. Soft-deletable.
/// </summary>
public class Currency : IDeletedAtEntity
{
    public Guid CurrencyId { get; set; } = Guid.NewGuid();
    /// <summary>Short code shown in dropdowns / on invoices, e.g. "USD", "CHF", "RM".</summary>
    public string Code { get; set; } = default!;
    /// <summary>Optional human name, e.g. "Swiss Franc".</summary>
    public string? Name { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime? DeletedAt { get; set; }
}
