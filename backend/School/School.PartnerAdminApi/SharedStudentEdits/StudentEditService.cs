namespace School.PartnerAdminApi.SharedStudentEdits;

/// <summary>
/// Shared write logic for the four student-edit endpoints (admin/partner ×
/// personal/background). Keeps the gate check + tri-table upsert in one
/// place so the endpoints stay thin and the audit text reads consistently.
/// </summary>
public static class StudentEditService
{
    /// <summary>
    /// True once any enrollment has reached
    /// <c>ApplicationApprovedAdmission</c> or later (Level >= 300). Partner
    /// edits are blocked beyond this point; admin edits are never blocked.
    /// </summary>
    public static Task<bool> IsAdmittedAsync(
        OdinDbContext db, Guid studentId, CancellationToken ct)
        => db.Enrollments
            .Where(e => e.StudentId == studentId && e.DeletedAt == null)
            .AnyAsync(e => e.Status.Level >= 300, ct);

    public sealed class PersonalDto
    {
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public DateTime? DateOfBirth { get; init; }
        public string? PassportId { get; init; }
        public int? NationalityId { get; init; }
        public string? AddressLine1 { get; init; }
        public string? AddressLine2 { get; init; }
        public string? City { get; init; }
        public string? StateRegion { get; init; }
        public string? PostalCode { get; init; }
        public string? CountryCode { get; init; }
    }

    public sealed class BackgroundDto
    {
        public string? HighestDegree { get; init; }
        public int? YearsWorkExperience { get; init; }
        public IReadOnlyList<LanguageEntry>? Languages { get; init; }
    }

    public sealed class LanguageEntry
    {
        public int LanguageId { get; init; }
        public int Proficiency { get; init; }
    }

    public static async Task UpdatePersonalAsync(
        OdinDbContext db, Student student, PersonalDto dto, string actorLabel, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        student.DateOfBirth = dto.DateOfBirth;
        student.PassportId = string.IsNullOrWhiteSpace(dto.PassportId) ? null : dto.PassportId;
        student.NationalityId = dto.NationalityId;

        var profile = await db.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == student.UserId, ct);
        if (profile is null)
        {
            profile = new UserProfile
            {
                UserProfileId = Guid.NewGuid(),
                UserId = student.UserId,
            };
            db.UserProfiles.Add(profile);
        }
        profile.FirstName = dto.FirstName;
        profile.LastName = dto.LastName;
        profile.DateOfBirth = dto.DateOfBirth;

        var address = await db.UserAddresses
            .FirstOrDefaultAsync(a => a.UserId == student.UserId && a.IsPrimary, ct);
        if (address is null)
        {
            address = new UserAddress
            {
                UserAddressId = Guid.NewGuid(),
                UserId = student.UserId,
                IsPrimary = true,
            };
            db.UserAddresses.Add(address);
        }
        // addressLine2 is currently dropped: UserAddress has no second-line
        // column. Adding it is a schema change tracked separately.
        address.Street = dto.AddressLine1;
        address.City = dto.City;
        address.State = dto.StateRegion;
        address.ZipCode = dto.PostalCode;
        address.Country = dto.CountryCode;

        db.StudentNotes.Add(new StudentNote
        {
            StudentNoteId = Guid.NewGuid(),
            StudentId = student.StudentId,
            Content = $"Personal details updated by {actorLabel}.",
            CreatedAt = now,
        });

        await db.SaveChangesAsync(ct);
    }

    public static async Task UpdateBackgroundAsync(
        OdinDbContext db, Student student, BackgroundDto dto, string actorLabel, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        student.HighestDegree = string.IsNullOrWhiteSpace(dto.HighestDegree) ? null : dto.HighestDegree;
        student.YearsWorkExperience = dto.YearsWorkExperience ?? 0;

        // UserLanguage.UserId is typed as Guid in the current schema; the
        // detail endpoints filter by `UserId == StudentId`, so we follow the
        // same convention here.
        var existing = await db.UserLanguages
            .Where(ul => ul.UserId == student.StudentId && ul.DeletedAt == null)
            .ToListAsync(ct);

        var incoming = (dto.Languages ?? Array.Empty<LanguageEntry>())
            .Where(l => l.LanguageId > 0)
            .GroupBy(l => l.LanguageId)
            .ToDictionary(g => g.Key, g => g.First());

        foreach (var row in existing)
        {
            if (!incoming.TryGetValue(row.LanguageId, out var match))
            {
                row.DeletedAt = now;
            }
            else
            {
                row.Proficiency = (LanguageProficiency)match.Proficiency;
                incoming.Remove(row.LanguageId);
            }
        }
        foreach (var add in incoming.Values)
        {
            db.UserLanguages.Add(new UserLanguage
            {
                StudentLanguageId = Guid.NewGuid(),
                UserId = student.StudentId,
                LanguageId = add.LanguageId,
                Proficiency = (LanguageProficiency)add.Proficiency,
            });
        }

        db.StudentNotes.Add(new StudentNote
        {
            StudentNoteId = Guid.NewGuid(),
            StudentId = student.StudentId,
            Content = $"Background updated by {actorLabel}.",
            CreatedAt = now,
        });

        await db.SaveChangesAsync(ct);
    }
}
