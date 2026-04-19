using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains;

public class FinalProject : IDeletedAtEntity
{
    public Guid FinalProjectId { get; set; } = Guid.NewGuid();
    public Guid StudentEnrollmentId { get; set; }
    public StudentEnrollment StudentEnrollment { get; set; } = default!;
    public string? Title { get; set; }
    public string? Supervisor { get; set; }
    public DateTime? SubmissionDate { get; set; }
    public int FinalProjectStatusId { get; set; }
    public FinalProjectStatus FinalProjectStatus { get; set; } = default!;
    public decimal? Score { get; set; }
    public DateTime? DeletedAt { get; set; }
}
