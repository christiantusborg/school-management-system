using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains;

public class Student
    : IDeletedAtEntity
{
    public Guid StudentId { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = default!;

    public string StudentNumber { get; set; } = default!;
    
    public Guid PartnerId { get; set; }
    public string? PassportId { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? HighestDegree { get; set; }
    public string? LanguageResult { get; set; }
    public int YearsWorkExperience { get; set; }
    /// <summary>Highest signup-wizard step the applicant has completed (0 = not started, 6 = submitted).</summary>
    public int WizardStep { get; set; }

    // Identity / address (filled by the wizard)
    public int? NationalityId { get; set; }
    


    public DateTime? DeletedAt { get; set; }

    
    public ICollection<StudentDocument> Documents { get; set; } = [];
    public ICollection<Enrollment> Enrollments { get; set; } = [];
    public ICollection<StudentNote> Notes { get; set; } = [];
    public ICollection<UserLanguage> Languages { get; set; } = [];
    
    public Nationality? Nationality { get; set; }
    public ApplicationUser User { get; set; } = default!;
}


