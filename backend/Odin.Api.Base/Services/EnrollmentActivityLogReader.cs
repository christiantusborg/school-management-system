using Microsoft.EntityFrameworkCore;
using Odin.Api.Base.Data;
using SharedLibrary.Basics.Opaque.Domains;

namespace Odin.Api.Base.Services;

/// <summary>
/// Flattens the two append-only audit tables (<see cref="EnrollmentStatusNote"/>
/// and <see cref="StudentDocumentNote"/>) into a single chronological
/// activity feed for one enrolment. Resolves actor name + role at read time
/// and supports a <c>maskAdmin</c> mode that hides admin user names behind
/// the literal "Admission Office" — used for partner and student views to
/// match the existing convention in the detail endpoints.
///
/// Read-only. Idempotent. No side effects.
/// </summary>
public sealed class EnrollmentActivityLogReader(OdinDbContext db)
{
    public sealed record ActivityEntry(
        string Id,
        string Source,                    // "enrollment" | "document"
        string? DocumentTypeName,         // populated when Source == "document"
        string ActorName,                 // resolved (or "Admission Office" when masked)
        string ActorRole,                 // "Student" | "Partner" | "Admin" | "System"
        string Action,                    // Note text if present, else synthesized from StatusCode
        string? StatusCode,               // drives icon/color in UI
        DateTime AtUtc);

    public async Task<IReadOnlyList<ActivityEntry>> ReadAsync(
        Guid enrollmentId, bool maskAdmin, CancellationToken ct)
    {
        // ── Status notes ────────────────────────────────────────────────────
        var statusRows = await db.EnrollmentStatusNotes
            .Where(n => n.EnrollmentId == enrollmentId)
            .Select(n => new
            {
                n.EnrollmentStatusNoteId,
                n.Note,
                n.ByUserId,
                n.CreatedAt,
                StatusCode = n.Status != null ? n.Status.Code : null,
            })
            .ToListAsync(ct);

        // ── Document notes (joined to doc + type) ────────────────────────────
        // Admin-side per-doc statuses (VerifiedByEnrolment / RejectedByEnrolment)
        // are filtered out: the bulk-review endpoint already writes a single
        // EnrollmentStatusNote summary, and showing 4–5 redundant rows per
        // admin review (one per doc + the summary) just adds noise. The
        // per-doc rows still exist in the DB so the student-portal per-doc
        // rejection display keeps working.
        var docRows = await db.StudentDocumentNotes
            .Where(n => db.StudentDocuments
                .Any(d => d.StudentDocumentId == n.StudentDocumentId
                    && d.EnrollmentId == enrollmentId
                    && d.DeletedAt == null))
            .Where(n => n.Status.Code != "VerifiedByEnrolment"
                     && n.Status.Code != "RejectedByEnrolment")
            .Select(n => new
            {
                n.StudentDocumentNoteId,
                n.Note,
                n.ByUserId,
                n.CreatedAt,
                StatusCode = n.Status.Code,
                DocumentTypeName = db.StudentDocuments
                    .Where(d => d.StudentDocumentId == n.StudentDocumentId)
                    .Select(d => d.DocumentType.Name)
                    .FirstOrDefault(),
            })
            .ToListAsync(ct);

        // ── Resolve actor profiles + admin-role membership in one shot ──────
        // EnrollmentStatusNote.ByUserId is Guid; StudentDocumentNote.ByUserId
        // is string. Normalize to string for the lookup.
        var actorIds = statusRows.Select(r => r.ByUserId.ToString())
            .Concat(docRows.Select(r => r.ByUserId))
            .Where(s => !string.IsNullOrEmpty(s) && s != Guid.Empty.ToString())
            .Distinct()
            .ToList();

        var profiles = await db.UserProfiles
            .Where(p => actorIds.Contains(p.UserId))
            .Select(p => new { p.UserId, p.FirstName, p.LastName })
            .ToListAsync(ct);
        var profileByUserId = profiles.ToDictionary(p => p.UserId);

        // Fallback: AspNetUsers.UserName for accounts with no UserProfile
        // (typical for partner-side users created via the admin UI without
        // a personal-info form).
        var userNames = await db.Users
            .Where(u => actorIds.Contains(u.Id))
            .Select(u => new { u.Id, u.UserName })
            .ToListAsync(ct);
        var userNameByUserId = userNames.ToDictionary(u => u.Id, u => u.UserName);

        var adminRoleId = await db.Roles
            .Where(r => r.Name == "Admin")
            .Select(r => r.Id)
            .FirstOrDefaultAsync(ct);
        var partnerRoleId = await db.Roles
            .Where(r => r.Name == "Partner")
            .Select(r => r.Id)
            .FirstOrDefaultAsync(ct);
        var studentRoleId = await db.Roles
            .Where(r => r.Name == "Student")
            .Select(r => r.Id)
            .FirstOrDefaultAsync(ct);

        var roleAssignments = await db.UserRoles
            .Where(ur => actorIds.Contains(ur.UserId))
            .Select(ur => new { ur.UserId, ur.RoleId })
            .ToListAsync(ct);
        var rolesByUserId = roleAssignments
            .GroupBy(r => r.UserId)
            .ToDictionary(g => g.Key, g => g.Select(r => r.RoleId).ToHashSet());

        string ResolveRole(string userId)
        {
            if (string.IsNullOrEmpty(userId) || userId == Guid.Empty.ToString())
                return "System";
            if (!rolesByUserId.TryGetValue(userId, out var roles)) return "System";
            if (adminRoleId   is not null && roles.Contains(adminRoleId))   return "Admin";
            if (partnerRoleId is not null && roles.Contains(partnerRoleId)) return "Partner";
            if (studentRoleId is not null && roles.Contains(studentRoleId)) return "Student";
            return "System";
        }

        string ResolveName(string userId, string role)
        {
            if (role == "Admin" && maskAdmin) return "Admission Office";
            if (role == "System") return "System";
            if (profileByUserId.TryGetValue(userId, out var p))
            {
                var fullName = string.Join(" ", new[] { p.FirstName, p.LastName }
                    .Where(s => !string.IsNullOrWhiteSpace(s)));
                if (!string.IsNullOrWhiteSpace(fullName)) return fullName;
            }
            // No profile (or empty FirstName/LastName) — fall back to username.
            if (userNameByUserId.TryGetValue(userId, out var userName) && !string.IsNullOrWhiteSpace(userName))
                return userName;
            return "Unknown user";
        }

        // ── Synthesize text for empty notes ─────────────────────────────────
        // Document approvals don't write a Note today (Note is only set on
        // rejections). Enrollment notes are always set, but we still safe-guard.
        static string DescribeDocStatus(string? statusCode, string? docName)
        {
            var doc = string.IsNullOrWhiteSpace(docName) ? "document" : docName;
            return statusCode switch
            {
                "Submitted"           => $"Uploaded {doc}",
                "VerifiedByPartner"   => $"Verified {doc}",
                "VerifiedByEnrolment" => $"Verified {doc} (Admission)",
                "RejectedByPartner"   => $"Rejected {doc}",
                "RejectedByEnrolment" => $"Rejected {doc} (Admission)",
                _ => $"Updated {doc}",
            };
        }

        var entries = new List<ActivityEntry>(statusRows.Count + docRows.Count);

        foreach (var r in statusRows)
        {
            var actorId = r.ByUserId.ToString();
            var role = ResolveRole(actorId);
            entries.Add(new ActivityEntry(
                Id: $"esn-{r.EnrollmentStatusNoteId}",
                Source: "enrollment",
                DocumentTypeName: null,
                ActorName: ResolveName(actorId, role),
                ActorRole: role,
                Action: string.IsNullOrWhiteSpace(r.Note) ? (r.StatusCode ?? "Status changed") : r.Note,
                StatusCode: r.StatusCode,
                AtUtc: r.CreatedAt));
        }

        foreach (var r in docRows)
        {
            var role = ResolveRole(r.ByUserId);
            entries.Add(new ActivityEntry(
                Id: $"sdn-{r.StudentDocumentNoteId}",
                Source: "document",
                DocumentTypeName: r.DocumentTypeName,
                ActorName: ResolveName(r.ByUserId, role),
                ActorRole: role,
                Action: string.IsNullOrWhiteSpace(r.Note)
                    ? DescribeDocStatus(r.StatusCode, r.DocumentTypeName)
                    : r.Note,
                StatusCode: r.StatusCode,
                AtUtc: r.CreatedAt));
        }

        // Primary sort: chronological. Secondary sort: when two entries share
        // the same millisecond (which happens whenever a single endpoint writes
        // multiple notes — e.g. partner-review writes 4 per-doc + 1 summary
        // with `now`), put document notes before enrolment notes so the
        // narrative reads "verified each doc … then summary" instead of the
        // other way round.
        return entries
            .OrderBy(e => e.AtUtc)
            .ThenBy(e => e.Source == "document" ? 0 : 1)
            .ToList();
    }
}
