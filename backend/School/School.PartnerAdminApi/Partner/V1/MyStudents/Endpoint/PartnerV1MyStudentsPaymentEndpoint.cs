using Odin.Api.Base.Payments;
using School.PartnerAdminApi.Partner.V1.MyUsers;

namespace School.PartnerAdminApi.Partner.V1.MyStudents.Endpoint;

/// <summary>
/// Partner READ-ONLY view of their own student's tuition payment plan and
/// Moodle login. The partner sees which installments / additional invoices
/// are due or paid and downloads the invoice PDFs; all editing (amounts,
/// paid flags, payment methods, Moodle credentials) stays with the
/// Admission Office. GET-only — a teacher user can read this too.
/// </summary>
[Route("/v1/partner/my-students/{studentId:guid}/enrollments/{enrollmentId:guid}/payment")]
[EndpointTag("Partner.MyStudents")]
public sealed class PartnerV1MyStudentsPaymentEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/partner/my-students/{studentId:guid}/enrollments/{enrollmentId:guid}/payment", GetAsync)
            .RequireAuthorization("PartnerOnly");
        app.MapGet("/v1/partner/my-students/{studentId:guid}/enrollments/{enrollmentId:guid}/payment/invoice", InvoiceAsync)
            .RequireAuthorization("PartnerOnly");
        app.MapGet("/v1/partner/my-students/{studentId:guid}/moodle", MoodleAsync)
            .RequireAuthorization("PartnerOnly");
        return app;
    }

    private static async Task<(bool Owns, Guid? PartnerId)> OwnsEnrolmentAsync(
        HttpContext http, OdinDbContext db, Guid studentId, Guid enrollmentId, CancellationToken ct)
    {
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(http, db, ct);
        if (fail is not null) return (false, null);
        var owns = await db.Enrollments.AnyAsync(e => e.StudentEnrollmentId == enrollmentId
            && e.StudentId == studentId && e.PartnerId == partnerId && e.DeletedAt == null, ct);
        return (owns, partnerId);
    }

    private static async Task<IResult> GetAsync(
        Guid studentId, Guid enrollmentId, HttpContext http, OdinDbContext db, CancellationToken ct)
    {
        var (owns, _) = await OwnsEnrolmentAsync(http, db, studentId, enrollmentId, ct);
        if (!owns) return Results.NotFound();

        var plan = await db.EnrollmentPaymentPlans
            .Where(p => p.StudentEnrollmentId == enrollmentId && p.DeletedAt == null)
            .Select(p => new
            {
                p.TotalTuitionFee,
                p.Currency,
                Installments = p.Installments.OrderBy(i => i.Sequence)
                    .Select(i => new { i.Sequence, i.Amount, i.DueDate, i.IsPaid, i.PaidDate })
                    .ToList(),
                AdditionalInvoices = p.AdditionalInvoices.OrderBy(i => i.Sequence)
                    .Select(i => new { i.Sequence, i.DueDate, i.IsPaid, i.PaidDate, i.LinesJson })
                    .ToList(),
            })
            .FirstOrDefaultAsync(ct);

        if (plan is null)
            return Results.Ok(new { exists = false, totalTuitionFee = 0m, currency = "USD", installments = Array.Empty<object>(), additionalInvoices = Array.Empty<object>(), totalPaid = 0m, balance = 0m });

        var additional = plan.AdditionalInvoices.Select(a => new
        {
            a.Sequence, a.DueDate, a.IsPaid, a.PaidDate,
            Total = AdditionalInvoiceLines.Total(a.LinesJson),
            Title = AdditionalInvoiceLines.Title(a.LinesJson, a.Sequence),
        }).ToList();
        var additionalTotal = additional.Sum(a => a.Total);
        var totalPaid = plan.Installments.Where(i => i.IsPaid).Sum(i => i.Amount)
            + additional.Where(a => a.IsPaid).Sum(a => a.Total);

        return Results.Ok(new
        {
            exists = true,
            totalTuitionFee = plan.TotalTuitionFee,
            currency = plan.Currency,
            installments = plan.Installments.Select(i => new
            {
                sequence = i.Sequence,
                amount = i.Amount,
                dueDate = i.DueDate,
                isPaid = i.IsPaid,
                paidDate = i.PaidDate,
            }),
            additionalInvoices = additional.Select(a => new
            {
                sequence = a.Sequence,
                title = a.Title,
                total = a.Total,
                dueDate = a.DueDate,
                isPaid = a.IsPaid,
                paidDate = a.PaidDate,
            }),
            additionalTotal,
            totalPaid,
            balance = plan.TotalTuitionFee + additionalTotal - totalPaid,
        });
    }

    private static async Task<IResult> InvoiceAsync(
        Guid studentId, Guid enrollmentId, HttpContext http, OdinDbContext db,
        InvoicePdfService invoicePdf, CancellationToken ct,
        [FromQuery] int? installment = null, [FromQuery] int? additional = null)
    {
        var (owns, _) = await OwnsEnrolmentAsync(http, db, studentId, enrollmentId, ct);
        if (!owns) return Results.NotFound();

        var bytes = await invoicePdf.RenderAsync(enrollmentId, ct, installment, additional);
        if (bytes is null)
            return Results.Json(new { error = "No payment plan (or no such invoice) for this enrolment yet." }, statusCode: StatusCodes.Status404NotFound);
        var suffix = installment is { } s ? $"-installment-{s}" : additional is { } a ? $"-additional-{a}" : "";
        return Results.File(bytes, "application/pdf", $"invoice-{enrollmentId.ToString()[..8]}{suffix}.pdf");
    }

    private static async Task<IResult> MoodleAsync(
        Guid studentId, HttpContext http, OdinDbContext db, CancellationToken ct)
    {
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(http, db, ct);
        if (fail is not null) return fail;

        var student = await db.Students
            .Where(s => s.StudentId == studentId && s.PartnerId == partnerId && s.DeletedAt == null)
            .Select(s => new { s.MoodleEnabled, s.MoodleUsername, s.MoodlePassword })
            .FirstOrDefaultAsync(ct);
        if (student is null) return Results.NotFound();

        return Results.Ok(new
        {
            moodleEnabled = student.MoodleEnabled,
            moodleUsername = student.MoodleUsername,
            moodlePassword = student.MoodlePassword,
        });
    }
}
