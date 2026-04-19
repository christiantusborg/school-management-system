using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains;

public class Student : IDeletedAtEntity
{
    public Guid StudentId { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = default!;
    public ApplicationUser User { get; set; } = default!;
    public string StudentNumber { get; set; } = default!;
    public Guid PartnerId { get; set; }
    public Partner Partner { get; set; } = default!;
    public string? PassportId { get; set; }
    public string? Address { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? HighestDegree { get; set; }
    public string? LanguageResult { get; set; }
    public int YearsWorkExperience { get; set; }
    public Guid TenantId { get; set; } = TenantConstants.DefaultTenantId;
    public DateTime? DeletedAt { get; set; }

    public ICollection<StudentDocument> Documents { get; set; } = [];
    public ICollection<StudentEnrollment> Enrollments { get; set; } = [];
    public ICollection<StudentNote> Notes { get; set; } = [];
}
