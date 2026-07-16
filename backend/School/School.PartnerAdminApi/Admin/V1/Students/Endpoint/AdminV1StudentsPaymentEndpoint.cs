using Odin.Api.Base.Letters;
using Odin.Api.Base.Payments;
using SharedLibrary.Basics.Opaque.Domains;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;
using SharedLibrary.Basics.Opaque.Domains.Payments;

namespace School.PartnerAdminApi.Admin.V1.Students.Endpoint;

/// <summary>
/// Admission-Office tuition payment plan for an enrolment: read/save the plan +
/// installments (auto-split on the client, editable, marked paid here), and
/// download a generated invoice PDF. Admin-only.
/// </summary>
[Route("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/payment")]
[EndpointTag("Admin.Students")]
public sealed class AdminV1StudentsPaymentEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/payment", GetAsync).RequireAuthorization("AdminOnly");
        app.MapPut("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/payment", SaveAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/payment/invoice", InvoiceAsync).RequireAuthorization("AdminOnly");
        return app;
    }

    public sealed class InstallmentInput
    {
        public int Sequence { get; init; }
        public decimal Amount { get; init; }
        public DateTime? DueDate { get; init; }
        public bool IsPaid { get; init; }
        public DateTime? PaidDate { get; init; }
        public bool PayByCardEnabled { get; init; }
        public string? CardPaymentLink { get; init; }
        public bool PayByBankEnabled { get; init; }
        public string? BankAccountDetails { get; init; }
    }

    public sealed class InvoiceLineInput
    {
        public string? Text { get; init; }
        public decimal Amount { get; init; }
    }

    public sealed class AdditionalInvoiceInput
    {
        public int Sequence { get; init; }
        public DateTime? DueDate { get; init; }
        public bool IsPaid { get; init; }
        public DateTime? PaidDate { get; init; }
        public bool PayByCardEnabled { get; init; }
        public string? CardPaymentLink { get; init; }
        public bool PayByBankEnabled { get; init; }
        public string? BankAccountDetails { get; init; }
        public List<InvoiceLineInput>? Lines { get; init; }
    }

    public sealed class SaveRequest
    {
        public decimal TotalTuitionFee { get; init; }
        public string? Currency { get; init; }
        public List<InstallmentInput>? Installments { get; init; }
        public List<AdditionalInvoiceInput>? AdditionalInvoices { get; init; }
    }

    private static DateTime? Norm(DateTime? d) =>
        d is { } v ? DateTime.SpecifyKind(v.Date, DateTimeKind.Unspecified) : null;

    private static async Task<IResult> GetAsync(
        Guid studentId, Guid enrollmentId, OdinDbContext db, CancellationToken ct)
    {
        var owns = await db.Enrollments.AnyAsync(e =>
            e.StudentEnrollmentId == enrollmentId && e.StudentId == studentId && e.DeletedAt == null, ct);
        if (!owns) return Results.NotFound();

        var plan = await db.EnrollmentPaymentPlans
            .Where(p => p.StudentEnrollmentId == enrollmentId && p.DeletedAt == null)
            .Select(p => new
            {
                p.PaymentPlanId,
                p.TotalTuitionFee,
                p.Currency,
                p.NumberOfPayments,
                Installments = p.Installments
                    .OrderBy(i => i.Sequence)
                    .Select(i => new { i.PaymentInstallmentId, i.Sequence, i.Amount, i.DueDate, i.IsPaid, i.PaidDate,
                        i.PayByCardEnabled, i.CardPaymentLink, i.PayByBankEnabled, i.BankAccountDetails })
                    .ToList(),
                AdditionalInvoices = p.AdditionalInvoices
                    .OrderBy(i => i.Sequence)
                    .Select(i => new { i.AdditionalInvoiceId, i.Sequence, i.DueDate, i.IsPaid, i.PaidDate,
                        i.PayByCardEnabled, i.CardPaymentLink, i.PayByBankEnabled, i.BankAccountDetails, i.LinesJson })
                    .ToList(),
            })
            .FirstOrDefaultAsync(ct);

        if (plan is null)
            return Results.Ok(new { exists = false, totalTuitionFee = 0m, currency = "USD", numberOfPayments = 0, installments = Array.Empty<object>(), additionalInvoices = Array.Empty<object>(), additionalTotal = 0m, totalPaid = 0m, balance = 0m });

        var additionalTotals = plan.AdditionalInvoices.ToDictionary(i => i.Sequence, i => AdditionalInvoiceLines.Total(i.LinesJson));
        var additionalTotal = additionalTotals.Values.Sum();
        var totalPaid = plan.Installments.Where(i => i.IsPaid).Sum(i => i.Amount)
            + plan.AdditionalInvoices.Where(i => i.IsPaid).Sum(i => additionalTotals[i.Sequence]);
        return Results.Ok(new
        {
            exists = true,
            totalTuitionFee = plan.TotalTuitionFee,
            currency = plan.Currency,
            numberOfPayments = plan.NumberOfPayments,
            installments = plan.Installments.Select(i => new
            {
                installmentId = i.PaymentInstallmentId,
                sequence = i.Sequence,
                amount = i.Amount,
                dueDate = i.DueDate,
                isPaid = i.IsPaid,
                paidDate = i.PaidDate,
                payByCardEnabled = i.PayByCardEnabled,
                cardPaymentLink = i.CardPaymentLink,
                payByBankEnabled = i.PayByBankEnabled,
                bankAccountDetails = i.BankAccountDetails,
            }),
            additionalInvoices = plan.AdditionalInvoices.Select(i => new
            {
                additionalInvoiceId = i.AdditionalInvoiceId,
                sequence = i.Sequence,
                dueDate = i.DueDate,
                isPaid = i.IsPaid,
                paidDate = i.PaidDate,
                payByCardEnabled = i.PayByCardEnabled,
                cardPaymentLink = i.CardPaymentLink,
                payByBankEnabled = i.PayByBankEnabled,
                bankAccountDetails = i.BankAccountDetails,
                lines = AdditionalInvoiceLines.Parse(i.LinesJson).Select(l => new { text = l.Text, amount = l.Amount }),
                total = additionalTotals[i.Sequence],
            }),
            additionalTotal,
            totalPaid,
            balance = plan.TotalTuitionFee + additionalTotal - totalPaid,
        });
    }

    private static async Task<IResult> SaveAsync(
        Guid studentId, Guid enrollmentId, [FromBody] SaveRequest body,
        OdinDbContext db, LetterReleaseService letterRelease, CancellationToken ct)
    {
        var owns = await db.Enrollments.AnyAsync(e =>
            e.StudentEnrollmentId == enrollmentId && e.StudentId == studentId && e.DeletedAt == null, ct);
        if (!owns) return Results.NotFound();

        if (body.TotalTuitionFee < 0)
            return Results.BadRequest(new { error = "Fees cannot be negative." });
        if ((body.AdditionalInvoices ?? new()).Any(a => (a.Lines ?? new()).Any(l => l.Amount < 0)))
            return Results.BadRequest(new { error = "Invoice line amounts cannot be negative." });

        var plan = await db.EnrollmentPaymentPlans
            .Include(p => p.Installments)
            .Include(p => p.AdditionalInvoices)
            .FirstOrDefaultAsync(p => p.StudentEnrollmentId == enrollmentId && p.DeletedAt == null, ct);

        var inputs = body.Installments ?? new();
        if (plan is null)
        {
            plan = new EnrollmentPaymentPlan
            {
                PaymentPlanId = Guid.NewGuid(),
                StudentEnrollmentId = enrollmentId,
            };
            db.EnrollmentPaymentPlans.Add(plan);
        }
        plan.TotalTuitionFee = body.TotalTuitionFee;
        plan.Currency = string.IsNullOrWhiteSpace(body.Currency) ? "USD" : body.Currency.Trim().ToUpperInvariant();
        plan.NumberOfPayments = inputs.Count;

        // Replace the installment set wholesale — the client owns the schedule.
        if (plan.Installments.Count > 0)
            db.PaymentInstallments.RemoveRange(plan.Installments);
        var seq = 0;
        foreach (var i in inputs.OrderBy(x => x.Sequence))
        {
            seq++;
            db.PaymentInstallments.Add(new PaymentInstallment
            {
                PaymentInstallmentId = Guid.NewGuid(),
                PaymentPlanId = plan.PaymentPlanId,
                Sequence = seq,
                Amount = i.Amount,
                DueDate = Norm(i.DueDate),
                IsPaid = i.IsPaid,
                PaidDate = i.IsPaid ? (Norm(i.PaidDate) ?? DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Unspecified)) : null,
                PayByCardEnabled = i.PayByCardEnabled,
                CardPaymentLink = string.IsNullOrWhiteSpace(i.CardPaymentLink) ? null : i.CardPaymentLink.Trim(),
                PayByBankEnabled = i.PayByBankEnabled,
                BankAccountDetails = string.IsNullOrWhiteSpace(i.BankAccountDetails) ? null : i.BankAccountDetails.Trim(),
            });
        }

        // Additional invoices are replaced wholesale too — the client owns them.
        if (plan.AdditionalInvoices.Count > 0)
            db.AdditionalInvoices.RemoveRange(plan.AdditionalInvoices);
        var aseq = 0;
        foreach (var a in (body.AdditionalInvoices ?? new()).OrderBy(x => x.Sequence))
        {
            aseq++;
            var lines = (a.Lines ?? new())
                .Select(l => new AdditionalInvoiceLines.Line((l.Text ?? string.Empty).Trim(), l.Amount))
                .ToList();
            if (lines.Count == 0) lines.Add(new AdditionalInvoiceLines.Line(string.Empty, 0m));
            db.AdditionalInvoices.Add(new AdditionalInvoice
            {
                AdditionalInvoiceId = Guid.NewGuid(),
                PaymentPlanId = plan.PaymentPlanId,
                Sequence = aseq,
                DueDate = Norm(a.DueDate),
                IsPaid = a.IsPaid,
                PaidDate = a.IsPaid ? (Norm(a.PaidDate) ?? DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Unspecified)) : null,
                PayByCardEnabled = a.PayByCardEnabled,
                CardPaymentLink = string.IsNullOrWhiteSpace(a.CardPaymentLink) ? null : a.CardPaymentLink.Trim(),
                PayByBankEnabled = a.PayByBankEnabled,
                BankAccountDetails = string.IsNullOrWhiteSpace(a.BankAccountDetails) ? null : a.BankAccountDetails.Trim(),
                LinesJson = AdditionalInvoiceLines.Serialize(lines),
            });
        }

        await db.SaveChangesAsync(ct);

        // The offer/admission letters may embed fee tags ([tuition fee],
        // [payment plan], …); re-render released ones so they stay current.
        // Best-effort: a render failure never fails the plan save.
        foreach (var (docTypeId, type) in new[]
        {
            (SystemDocumentTypeIds.OfferLetter, LetterType.OfferLetter),
            (SystemDocumentTypeIds.AdmissionLetter, LetterType.AdmissionLetter),
        })
        {
            var released = await db.StudentDocuments.AnyAsync(d =>
                d.EnrollmentId == enrollmentId && d.DocumentTypeId == docTypeId && d.DeletedAt == null, ct);
            if (released)
            {
                try { await letterRelease.ReleaseAsync(enrollmentId, type, ct); }
                catch { /* keep the plan save even if re-render fails */ }
            }
        }

        return await GetAsync(studentId, enrollmentId, db, ct);
    }

    private static async Task<IResult> InvoiceAsync(
        Guid studentId, Guid enrollmentId, OdinDbContext db, InvoicePdfService invoicePdf,
        CancellationToken ct, [FromQuery] int? installment = null, [FromQuery] int? additional = null)
    {
        var owns = await db.Enrollments.AnyAsync(e =>
            e.StudentEnrollmentId == enrollmentId && e.StudentId == studentId && e.DeletedAt == null, ct);
        if (!owns) return Results.NotFound();

        var bytes = await invoicePdf.RenderAsync(enrollmentId, ct, installment, additional);
        if (bytes is null)
            return Results.Json(new { error = "No payment plan (or no such invoice) for this enrolment yet." }, statusCode: StatusCodes.Status404NotFound);
        var suffix = installment is { } s ? $"-installment-{s}" : additional is { } a ? $"-additional-{a}" : "";
        return Results.File(bytes, "application/pdf", $"invoice-{enrollmentId.ToString()[..8]}{suffix}.pdf");
    }
}
