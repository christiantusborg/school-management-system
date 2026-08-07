using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains.Partners;

/// <summary>
/// A bulk agreement with a partner covering a batch of students, either by
/// COMMENCEMENT date (kind 0: enrolments starting in the period) or by
/// GRADUATION date (kind 1: enrolments graduating in the period), limited to
/// the selected specializations. Coverage is CALCULATED live and exclusive:
/// within one kind, the EARLIEST-created agreement claims an enrolment;
/// later overlapping agreements never double-count it. AgreementNumber is
/// auto-generated (BA-{PARTNERNUMBER}-{seq}) and immutable.
/// </summary>
public class BulkAgreement : IDeletedAtEntity
{
    public Guid BulkAgreementId { get; set; } = Guid.NewGuid();
    public Guid PartnerId { get; set; }
    public string AgreementNumber { get; set; } = default!;
    /// <summary>0 = by commencement date, 1 = by graduation date.</summary>
    public int Kind { get; set; }
    public DateTime PeriodFrom { get; set; }
    public DateTime PeriodTo { get; set; }
    /// <summary>Agreed number of students under this agreement.</summary>
    public int TargetStudents { get; set; }
    public string? Note { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public Partner Partner { get; set; } = default!;
    public ICollection<BulkAgreementSpecialization> Specializations { get; set; } = new List<BulkAgreementSpecialization>();
}

/// <summary>Specialization included in a bulk agreement.</summary>
public class BulkAgreementSpecialization
{
    public Guid BulkAgreementSpecializationId { get; set; } = Guid.NewGuid();
    public Guid BulkAgreementId { get; set; }
    public Guid SpecializationId { get; set; }

    public BulkAgreement Agreement { get; set; } = default!;
}
