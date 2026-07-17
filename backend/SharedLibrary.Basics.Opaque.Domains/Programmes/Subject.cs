using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

public class Subject : IDeletedAtEntity
{
    public Guid SubjectId { get; set; } = Guid.NewGuid();
    public Guid SpecializationId { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    /// <summary>ECTS credits. Half-point steps allowed (e.g. 7.5).</summary>
    public decimal Ects { get; set; }

    /// <summary>
    /// Marks this module as the thesis / dissertation / final project. A
    /// specialization normally has 0 or 1. When a thesis module is graded, the
    /// grade UI prompts for the project title (stored on the enrolment and
    /// surfaced on the transcript via the [project title] tag).
    /// </summary>
    public bool IsThesis { get; set; }

    /// <summary>Programme-level default module start: commencement + N days.
    /// Applies automatically to every student unless the Admission Office
    /// sets a per-student override. Null = module starts at commencement.</summary>
    public int? DefaultStartOffsetDays { get; set; }

    /// <summary>Programme-level default module end: commencement + N days.
    /// Null = no default end date (shown as TBC until overridden).</summary>
    public int? DefaultEndOffsetDays { get; set; }

    public DateTime? IsActive { get; set; }
    public DateTime? DeletedAt { get; set; }
   
    public Specialization Specializations { get; set; }
}