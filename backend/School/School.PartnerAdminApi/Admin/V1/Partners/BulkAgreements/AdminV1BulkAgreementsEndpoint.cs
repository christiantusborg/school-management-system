using System.Security.Claims;
using Odin.Api.Base.Authorization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using School.PartnerAdminApi.Partner.V1.MyUsers;
using SharedLibrary.Basics.Opaque.Domains.Partners;

namespace School.PartnerAdminApi.Admin.V1.Partners.BulkAgreements;

/// <summary>
/// Bulk agreements per partner: an auto-numbered agreement
/// (BA-{PARTNERNUMBER}-{seq}) covering students by COMMENCEMENT date
/// (kind 0) or GRADUATION date (kind 1) within a period, limited to the
/// selected specializations.
///
/// Membership is PERSISTED on the enrolment (Enrollment.BulkAgreementId):
/// unassigned enrolments are auto-assigned to the OLDEST-created matching
/// agreement (never double-counted); no match = stays null. Admission can
/// MOVE an enrolment to any other agreement of the same partner (or detach
/// it) — manual placements are respected and never overwritten by the
/// auto-assign, which only ever fills nulls. Deleting an agreement releases
/// its enrolments back to auto-assignment.
/// </summary>
[Route("/v1/admin/partners/{partnerId:guid}/bulk-agreements")]
[EndpointTag("Admin.Partners.BulkAgreements")]
public sealed class AdminV1BulkAgreementsEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/admin/partners/{partnerId:guid}/bulk-agreements", ListAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/admin/partners/{partnerId:guid}/bulk-agreements", CreateAsync).RequireAuthorization("AdminOnly");
        app.MapPatch("/v1/admin/partners/{partnerId:guid}/bulk-agreements/{id:guid}", UpdateAsync).RequireAuthorization("AdminOnly");
        app.MapDelete("/v1/admin/partners/{partnerId:guid}/bulk-agreements/{id:guid}", DeleteAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/partners/{partnerId:guid}/bulk-agreements/{id:guid}/students", StudentsAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/partners/{partnerId:guid}/bulk-agreements/{id:guid}/export", ExportAsync).RequireAuthorization("AdminOnly");
        app.MapPatch("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/bulk-agreement", MoveAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/partner/bulk-agreements", PartnerListAsync).RequireAuthorization("PartnerOnly");
        return app;
    }

    public sealed class WriteBody
    {
        public int Kind { get; init; }
        public DateTime? PeriodFrom { get; init; }
        public DateTime? PeriodTo { get; init; }
        public int TargetStudents { get; init; }
        public string? Note { get; init; }
        public List<Guid>? SpecializationIds { get; init; }
    }

    public sealed class MoveBody { public Guid? BulkAgreementId { get; init; } }

    /// <summary>Auto-assign every UNASSIGNED enrolment of the partner to the
    /// oldest-created agreement whose kind-date, period and specialization
    /// list match. Fills nulls only — manual placements are never touched.</summary>
    public static async Task EnsureAssignmentsAsync(OdinDbContext db, Guid partnerId, CancellationToken ct)
    {
        var agreements = await db.BulkAgreements
            .Where(a => a.PartnerId == partnerId && a.DeletedAt == null)
            .OrderBy(a => a.CreatedAt)
            .Select(a => new
            {
                a.BulkAgreementId,
                a.Kind,
                a.PeriodFrom,
                a.PeriodTo,
                SpecIds = a.Specializations.Select(s => s.SpecializationId).ToList(),
            })
            .ToListAsync(ct);
        if (agreements.Count == 0) return;

        var unassigned = await db.Enrollments
            .Where(e => e.PartnerId == partnerId && e.DeletedAt == null && e.BulkAgreementId == null)
            .Select(e => new
            {
                Enrollment = e,
                e.SpecializationId,
                e.CommencementDate,
                e.GraduationDate,
                GradesApprovedAt = db.EnrollmentStatusNotes
                    .Where(n => n.EnrollmentId == e.StudentEnrollmentId
                        && n.StatusId == SharedLibrary.Basics.Opaque.Domains.EnrollmentStatusIds.GradesApproved)
                    .OrderByDescending(n => n.CreatedAt)
                    .Select(n => (DateTime?)n.CreatedAt)
                    .FirstOrDefault(),
            })
            .ToListAsync(ct);
        if (unassigned.Count == 0) return;

        var changed = false;
        foreach (var e in unassigned)
        {
            foreach (var a in agreements)
            {
                if (!a.SpecIds.Contains(e.SpecializationId)) continue;
                var date = a.Kind == 0 ? e.CommencementDate : (e.GraduationDate ?? e.GradesApprovedAt);
                if (date is not { } d || d.Date < a.PeriodFrom.Date || d.Date > a.PeriodTo.Date) continue;
                e.Enrollment.BulkAgreementId = a.BulkAgreementId;
                changed = true;
                break;
            }
        }
        if (changed) await db.SaveChangesAsync(ct);
    }

    private sealed record Covered(
        Guid StudentEnrollmentId, Guid StudentId, string StudentNumber, string Name,
        string ProgrammeCode, string SpecializationName, DateTime? Date);

    private static async Task<List<Covered>> CoveredAsync(OdinDbContext db, Guid agreementId, int kind, CancellationToken ct)
    {
        var rows = await db.Enrollments
            .Where(e => e.BulkAgreementId == agreementId && e.DeletedAt == null)
            .Select(e => new
            {
                e.StudentEnrollmentId,
                e.StudentId,
                e.CommencementDate,
                e.GraduationDate,
                GradesApprovedAt = db.EnrollmentStatusNotes
                    .Where(n => n.EnrollmentId == e.StudentEnrollmentId
                        && n.StatusId == SharedLibrary.Basics.Opaque.Domains.EnrollmentStatusIds.GradesApproved)
                    .OrderByDescending(n => n.CreatedAt)
                    .Select(n => (DateTime?)n.CreatedAt)
                    .FirstOrDefault(),
                StudentNumber = db.Students.Where(s => s.StudentId == e.StudentId)
                    .Select(s => s.StudentNumber).FirstOrDefault(),
                Name = db.Students.Where(s => s.StudentId == e.StudentId)
                    .Select(s => db.UserProfiles.Where(p => p.UserId == s.UserId)
                        .Select(p => (p.FirstName + " " + p.LastName).Trim()).FirstOrDefault())
                    .FirstOrDefault(),
                ProgrammeCode = e.Specialization.Programmes.Code,
                SpecializationName = e.Specialization.Name,
            })
            .ToListAsync(ct);
        return rows.Select(e => new Covered(
                e.StudentEnrollmentId, e.StudentId, e.StudentNumber ?? "?", e.Name ?? "?",
                e.ProgrammeCode, e.SpecializationName,
                kind == 0 ? e.CommencementDate : (e.GraduationDate ?? e.GradesApprovedAt)))
            .OrderBy(c => c.Date)
            .ToList();
    }

    private static async Task<object> BuildListAsync(OdinDbContext db, Guid partnerId, CancellationToken ct)
    {
        await EnsureAssignmentsAsync(db, partnerId, ct);
        var items = await db.BulkAgreements
            .Where(a => a.PartnerId == partnerId && a.DeletedAt == null)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new
            {
                bulkAgreementId = a.BulkAgreementId,
                agreementNumber = a.AgreementNumber,
                kind = a.Kind,
                periodFrom = a.PeriodFrom,
                periodTo = a.PeriodTo,
                targetStudents = a.TargetStudents,
                note = a.Note,
                createdAt = a.CreatedAt,
                specializationIds = a.Specializations.Select(s => s.SpecializationId).ToList(),
                specializationNames = a.Specializations
                    .Select(s => db.Specializations.Where(sp => sp.SpecializationId == s.SpecializationId)
                        .Select(sp => sp.Programmes.Code + " · " + sp.Name).FirstOrDefault())
                    .ToList(),
                coveredCount = db.Enrollments.Count(e => e.BulkAgreementId == a.BulkAgreementId && e.DeletedAt == null),
            })
            .ToListAsync(ct);
        return new { items };
    }

    private static async Task<IResult> ListAsync(Guid partnerId, OdinDbContext db, CancellationToken ct)
    {
        if (!await db.Partners.AnyAsync(p => p.PartnerId == partnerId && p.DeletedAt == null, ct))
            return Results.NotFound();
        return Results.Ok(await BuildListAsync(db, partnerId, ct));
    }

    private static async Task<IResult> CreateAsync(
        Guid partnerId, [FromBody] WriteBody body, HttpContext ctx, IPermissionService perms, OdinDbContext db, CancellationToken ct)
    {
        if (await perms.AccessAsync(ctx.User, "bulk_agreements.manage", ct) != AccessLevel.Edit) return Results.Forbid();

        var partner = await db.Partners.FirstOrDefaultAsync(p => p.PartnerId == partnerId && p.DeletedAt == null, ct);
        if (partner is null) return Results.NotFound();
        if (body.PeriodFrom is not { } from || body.PeriodTo is not { } to || to < from)
            return Results.BadRequest(new { error = "A valid period (from ≤ to) is required." });
        if ((body.SpecializationIds ?? new()).Count == 0)
            return Results.BadRequest(new { error = "Select at least one specialization." });
        if (body.Kind is not (0 or 1))
            return Results.BadRequest(new { error = "Kind must be 0 (commencement) or 1 (graduation)." });

        var seq = await db.BulkAgreements.CountAsync(a => a.PartnerId == partnerId, ct) + 1;
        var agreement = new BulkAgreement
        {
            PartnerId = partnerId,
            AgreementNumber = $"BA-{partner.PartnerNumber}-{seq:000}",
            Kind = body.Kind,
            PeriodFrom = DateTime.SpecifyKind(from.Date, DateTimeKind.Unspecified),
            PeriodTo = DateTime.SpecifyKind(to.Date, DateTimeKind.Unspecified),
            TargetStudents = Math.Max(0, body.TargetStudents),
            Note = body.Note?.Trim(),
            CreatedByUserId = Guid.TryParse(ctx.User.FindFirstValue(ClaimTypes.NameIdentifier), out var g) ? g : Guid.Empty,
            CreatedAt = DateTime.UtcNow,
        };
        db.BulkAgreements.Add(agreement);
        foreach (var specId in body.SpecializationIds!.Distinct())
            db.BulkAgreementSpecializations.Add(new BulkAgreementSpecialization
            {
                BulkAgreementId = agreement.BulkAgreementId,
                SpecializationId = specId,
            });
        await db.SaveChangesAsync(ct);
        await EnsureAssignmentsAsync(db, partnerId, ct);
        return Results.Ok(new { bulkAgreementId = agreement.BulkAgreementId, agreementNumber = agreement.AgreementNumber });
    }

    private static async Task<IResult> UpdateAsync(
        Guid partnerId, Guid id, [FromBody] WriteBody body, HttpContext ctx, IPermissionService perms, OdinDbContext db, CancellationToken ct)
    {
        if (await perms.AccessAsync(ctx.User, "bulk_agreements.manage", ct) != AccessLevel.Edit) return Results.Forbid();

        var agreement = await db.BulkAgreements
            .Include(a => a.Specializations)
            .FirstOrDefaultAsync(a => a.BulkAgreementId == id && a.PartnerId == partnerId && a.DeletedAt == null, ct);
        if (agreement is null) return Results.NotFound();
        if (body.PeriodFrom is { } from && body.PeriodTo is { } to && to >= from)
        {
            agreement.PeriodFrom = DateTime.SpecifyKind(from.Date, DateTimeKind.Unspecified);
            agreement.PeriodTo = DateTime.SpecifyKind(to.Date, DateTimeKind.Unspecified);
        }
        agreement.TargetStudents = Math.Max(0, body.TargetStudents);
        agreement.Note = body.Note?.Trim();
        if (body.SpecializationIds is { Count: > 0 } specIds)
        {
            db.BulkAgreementSpecializations.RemoveRange(agreement.Specializations);
            foreach (var specId in specIds.Distinct())
                db.BulkAgreementSpecializations.Add(new BulkAgreementSpecialization
                {
                    BulkAgreementId = agreement.BulkAgreementId,
                    SpecializationId = specId,
                });
        }
        await db.SaveChangesAsync(ct);
        await EnsureAssignmentsAsync(db, partnerId, ct);
        return Results.Ok(new { bulkAgreementId = id });
    }

    private static async Task<IResult> DeleteAsync(
        Guid partnerId, Guid id, HttpContext ctx, IPermissionService perms, OdinDbContext db, CancellationToken ct)
    {
        if (await perms.AccessAsync(ctx.User, "bulk_agreements.manage", ct) != AccessLevel.Edit) return Results.Forbid();

        var agreement = await db.BulkAgreements
            .FirstOrDefaultAsync(a => a.BulkAgreementId == id && a.PartnerId == partnerId && a.DeletedAt == null, ct);
        if (agreement is null) return Results.NotFound();
        agreement.DeletedAt = DateTime.UtcNow;
        // Release the enrolments; they refill from other matching agreements.
        await db.Enrollments.Where(e => e.BulkAgreementId == id)
            .ExecuteUpdateAsync(u => u.SetProperty(e => e.BulkAgreementId, (Guid?)null), ct);
        await db.SaveChangesAsync(ct);
        await EnsureAssignmentsAsync(db, partnerId, ct);
        return Results.Ok(new { bulkAgreementId = id });
    }

    /// <summary>Admission moves an enrolment to another agreement of the SAME
    /// partner (or detaches it with null). Manual placements stick.</summary>
    private static async Task<IResult> MoveAsync(
        Guid studentId, Guid enrollmentId, [FromBody] MoveBody body,
        HttpContext httpContext, IPermissionService perms, OdinDbContext db, CancellationToken ct)
    {
        if (await perms.AccessAsync(httpContext.User, "student.enrolment.bulk_agreement", ct) != AccessLevel.Edit) return Results.Forbid();

        var enrolment = await db.Enrollments.FirstOrDefaultAsync(e =>
            e.StudentEnrollmentId == enrollmentId && e.StudentId == studentId && e.DeletedAt == null, ct);
        if (enrolment is null) return Results.NotFound();
        if (body.BulkAgreementId is { } targetId)
        {
            var ok = await db.BulkAgreements.AnyAsync(a => a.BulkAgreementId == targetId
                && a.PartnerId == enrolment.PartnerId && a.DeletedAt == null, ct);
            if (!ok) return Results.BadRequest(new { error = "That agreement doesn't belong to this enrolment's partner." });
        }
        enrolment.BulkAgreementId = body.BulkAgreementId;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { enrollmentId, bulkAgreementId = enrolment.BulkAgreementId });
    }

    private static async Task<IResult> StudentsAsync(
        Guid partnerId, Guid id, OdinDbContext db, CancellationToken ct)
    {
        var agreement = await db.BulkAgreements
            .FirstOrDefaultAsync(a => a.BulkAgreementId == id && a.PartnerId == partnerId && a.DeletedAt == null, ct);
        if (agreement is null) return Results.NotFound();
        await EnsureAssignmentsAsync(db, partnerId, ct);
        var covered = await CoveredAsync(db, id, agreement.Kind, ct);
        return Results.Ok(new
        {
            items = covered.Select(c => new
            {
                studentEnrollmentId = c.StudentEnrollmentId,
                studentId = c.StudentId,
                studentNumber = c.StudentNumber,
                name = c.Name,
                programmeCode = c.ProgrammeCode,
                specializationName = c.SpecializationName,
                date = c.Date,
            }),
        });
    }

    private static async Task<IResult> ExportAsync(
        Guid partnerId, Guid id, OdinDbContext db, CancellationToken ct, [FromQuery] string format = "csv")
    {
        var agreement = await db.BulkAgreements
            .FirstOrDefaultAsync(a => a.BulkAgreementId == id && a.PartnerId == partnerId && a.DeletedAt == null, ct);
        if (agreement is null) return Results.NotFound();
        await EnsureAssignmentsAsync(db, partnerId, ct);
        var partnerName = await db.Partners.Where(p => p.PartnerId == partnerId).Select(p => p.Name).FirstAsync(ct);
        var covered = await CoveredAsync(db, id, agreement.Kind, ct);
        var dateLabel = agreement.Kind == 0 ? "Commencement" : "Graduation";

        if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Agreement;{agreement.AgreementNumber};Partner;{partnerName};Kind;{dateLabel};Period;{agreement.PeriodFrom:yyyy-MM-dd} - {agreement.PeriodTo:yyyy-MM-dd};Covered;{covered.Count};Target;{agreement.TargetStudents}");
            sb.AppendLine($"Student #;Name;Programme;Specialization;{dateLabel} date");
            foreach (var c in covered)
                sb.AppendLine($"{c.StudentNumber};{c.Name};{c.ProgrammeCode};{c.SpecializationName};{c.Date:yyyy-MM-dd}");
            return Results.File(System.Text.Encoding.UTF8.GetBytes(sb.ToString()),
                "text/csv", $"{agreement.AgreementNumber}.csv");
        }

        var pdf = Document.Create(doc => doc.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(36);
            page.Header().Column(col =>
            {
                col.Item().Text($"Bulk Agreement {agreement.AgreementNumber}").FontSize(16).Bold();
                col.Item().Text($"{partnerName} · by {dateLabel.ToLowerInvariant()} date · {agreement.PeriodFrom:dd MMM yyyy} – {agreement.PeriodTo:dd MMM yyyy}")
                    .FontSize(10).FontColor(Colors.Grey.Darken1);
                col.Item().PaddingTop(4).Text($"Covered students: {covered.Count} of {agreement.TargetStudents} agreed")
                    .FontSize(11).Bold();
            });
            page.Content().PaddingTop(12).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(2.2f); c.RelativeColumn(3); c.RelativeColumn(1.4f);
                    c.RelativeColumn(2.4f); c.RelativeColumn(1.6f);
                });
                table.Header(h =>
                {
                    foreach (var t in new[] { "Student #", "Name", "Programme", "Specialization", $"{dateLabel} date" })
                        h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text(t).FontSize(9).Bold();
                });
                foreach (var c in covered)
                {
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(c.StudentNumber).FontSize(9);
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(c.Name).FontSize(9);
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(c.ProgrammeCode).FontSize(9);
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(c.SpecializationName).FontSize(9);
                    table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text($"{c.Date:yyyy-MM-dd}").FontSize(9);
                }
            });
            page.Footer().AlignRight().Text(t => { t.CurrentPageNumber().FontSize(8); t.Span(" / ").FontSize(8); t.TotalPages().FontSize(8); });
        })).GeneratePdf();
        return Results.File(pdf, "application/pdf", $"{agreement.AgreementNumber}.pdf");
    }

    /// <summary>Partner portal: read-only list of their agreements with the
    /// covered students (no admin links).</summary>
    private static async Task<IResult> PartnerListAsync(
        HttpContext ctx, OdinDbContext db, CancellationToken ct)
    {
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(ctx, db, ct);
        if (fail is not null || partnerId is null) return fail ?? Results.StatusCode(403);
        var list = (dynamic)await BuildListAsync(db, partnerId.Value, ct);
        var agreements = await db.BulkAgreements
            .Where(a => a.PartnerId == partnerId && a.DeletedAt == null)
            .Select(a => new { a.BulkAgreementId, a.Kind })
            .ToListAsync(ct);
        var students = new Dictionary<Guid, object>();
        foreach (var a in agreements)
        {
            var covered = await CoveredAsync(db, a.BulkAgreementId, a.Kind, ct);
            students[a.BulkAgreementId] = covered.Select(c => new
            {
                studentNumber = c.StudentNumber,
                name = c.Name,
                programmeCode = c.ProgrammeCode,
                specializationName = c.SpecializationName,
                date = c.Date,
            }).ToList();
        }
        return Results.Ok(new { list.items, students });
    }
}
