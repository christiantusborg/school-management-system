using Odin.Api.Base.Authorization;
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
        app.MapPost("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/payment/records", AddRecordAsync).RequireAuthorization("AdminOnly");
        app.MapDelete("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/payment/records/{recordId:guid}", DeleteRecordAsync).RequireAuthorization("AdminOnly");
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
                        i.PayByCardEnabled, i.CardPaymentLink, i.PayByBankEnabled, i.BankAccountDetails,
                        Records = db.PaymentRecords
                            .Where(r => r.PaymentInstallmentId == i.PaymentInstallmentId)
                            .OrderBy(r => r.PaidDate).ThenBy(r => r.CreatedAt)
                            .Select(r => new { r.PaymentRecordId, r.Amount, r.PaidDate, r.Note })
                            .ToList() })
                    .ToList(),
                AdditionalInvoices = p.AdditionalInvoices
                    .OrderBy(i => i.Sequence)
                    .Select(i => new { i.AdditionalInvoiceId, i.Sequence, i.DueDate, i.IsPaid, i.PaidDate,
                        i.PayByCardEnabled, i.CardPaymentLink, i.PayByBankEnabled, i.BankAccountDetails, i.LinesJson,
                        Records = db.PaymentRecords
                            .Where(r => r.AdditionalInvoiceId == i.AdditionalInvoiceId)
                            .OrderBy(r => r.PaidDate).ThenBy(r => r.CreatedAt)
                            .Select(r => new { r.PaymentRecordId, r.Amount, r.PaidDate, r.Note })
                            .ToList() })
                    .ToList(),
            })
            .FirstOrDefaultAsync(ct);

        if (plan is null)
            return Results.Ok(new { exists = false, totalTuitionFee = 0m, currency = "USD", numberOfPayments = 0, installments = Array.Empty<object>(), additionalInvoices = Array.Empty<object>(), additionalTotal = 0m, totalPaid = 0m, balance = 0m });

        var additionalTotals = plan.AdditionalInvoices.ToDictionary(i => i.Sequence, i => AdditionalInvoiceLines.Total(i.LinesJson));
        var additionalTotal = additionalTotals.Values.Sum();
        // Paid amount per item: sum of its part-payment records; a legacy item
        // marked paid with no records counts as fully paid.
        decimal PaidOf(bool isPaid, decimal amount, decimal recordsSum, int recordCount) =>
            recordCount > 0 ? Math.Min(recordsSum, amount) : (isPaid ? amount : 0m);
        var instPaid = plan.Installments.ToDictionary(i => i.Sequence,
            i => PaidOf(i.IsPaid, i.Amount, i.Records.Sum(r => r.Amount), i.Records.Count));
        var addPaid = plan.AdditionalInvoices.ToDictionary(i => i.Sequence,
            i => PaidOf(i.IsPaid, additionalTotals[i.Sequence], i.Records.Sum(r => r.Amount), i.Records.Count));
        var totalPaid = instPaid.Values.Sum() + addPaid.Values.Sum();
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
                amountPaid = instPaid[i.Sequence],
                payments = i.Records.Select(r => new { recordId = r.PaymentRecordId, amount = r.Amount, paidDate = r.PaidDate, note = r.Note }),
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
                amountPaid = addPaid[i.Sequence],
                payments = i.Records.Select(r => new { recordId = r.PaymentRecordId, amount = r.Amount, paidDate = r.PaidDate, note = r.Note }),
            }),
            additionalTotal,
            totalPaid,
            balance = plan.TotalTuitionFee + additionalTotal - totalPaid,
        });
    }

    private static async Task<IResult> SaveAsync(
        Guid studentId, Guid enrollmentId, [FromBody] SaveRequest body,
        HttpContext httpContext, IPermissionService perms,
        OdinDbContext db, LetterReleaseService letterRelease, CancellationToken ct)
    {
        if (await perms.AccessAsync(httpContext.User, "student.payment", ct) != AccessLevel.Edit) return Results.Forbid();

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

        // Upsert installments by sequence so surviving rows KEEP their IDs:
        // payment records and combined-invoice lines reference them. Paid
        // state is derived from payment records (plus the combined-invoice
        // flow) and is never overwritten by a plan save.
        var bySeq = plan.Installments.ToDictionary(x => x.Sequence);
        var seq = 0;
        var keptSeqs = new HashSet<int>();
        foreach (var i in inputs.OrderBy(x => x.Sequence))
        {
            seq++;
            keptSeqs.Add(seq);
            if (!bySeq.TryGetValue(seq, out var row))
            {
                row = new PaymentInstallment
                {
                    PaymentInstallmentId = Guid.NewGuid(),
                    PaymentPlanId = plan.PaymentPlanId,
                    Sequence = seq,
                };
                db.PaymentInstallments.Add(row);
            }
            row.Amount = i.Amount;
            row.DueDate = Norm(i.DueDate);
            row.PayByCardEnabled = i.PayByCardEnabled;
            row.CardPaymentLink = string.IsNullOrWhiteSpace(i.CardPaymentLink) ? null : i.CardPaymentLink.Trim();
            row.PayByBankEnabled = i.PayByBankEnabled;
            row.BankAccountDetails = string.IsNullOrWhiteSpace(i.BankAccountDetails) ? null : i.BankAccountDetails.Trim();
        }
        foreach (var dropped in plan.Installments.Where(x => !keptSeqs.Contains(x.Sequence)).ToList())
            db.PaymentInstallments.Remove(dropped);

        // Additional invoices upsert by sequence for the same reason.
        var addBySeq = plan.AdditionalInvoices.ToDictionary(x => x.Sequence);
        var aseq = 0;
        var keptAddSeqs = new HashSet<int>();
        foreach (var a in (body.AdditionalInvoices ?? new()).OrderBy(x => x.Sequence))
        {
            aseq++;
            keptAddSeqs.Add(aseq);
            var lines = (a.Lines ?? new())
                .Select(l => new AdditionalInvoiceLines.Line((l.Text ?? string.Empty).Trim(), l.Amount))
                .ToList();
            if (lines.Count == 0) lines.Add(new AdditionalInvoiceLines.Line(string.Empty, 0m));
            if (!addBySeq.TryGetValue(aseq, out var row))
            {
                row = new AdditionalInvoice
                {
                    AdditionalInvoiceId = Guid.NewGuid(),
                    PaymentPlanId = plan.PaymentPlanId,
                    Sequence = aseq,
                };
                db.AdditionalInvoices.Add(row);
            }
            row.DueDate = Norm(a.DueDate);
            row.PayByCardEnabled = a.PayByCardEnabled;
            row.CardPaymentLink = string.IsNullOrWhiteSpace(a.CardPaymentLink) ? null : a.CardPaymentLink.Trim();
            row.PayByBankEnabled = a.PayByBankEnabled;
            row.BankAccountDetails = string.IsNullOrWhiteSpace(a.BankAccountDetails) ? null : a.BankAccountDetails.Trim();
            row.LinesJson = AdditionalInvoiceLines.Serialize(lines);
        }
        foreach (var dropped in plan.AdditionalInvoices.Where(x => !keptAddSeqs.Contains(x.Sequence)).ToList())
            db.AdditionalInvoices.Remove(dropped);

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

    public sealed class RecordBody
    {
        public Guid? InstallmentId { get; init; }
        public Guid? AdditionalInvoiceId { get; init; }
        public decimal Amount { get; init; }
        public DateTime? PaidDate { get; init; }
        public string? Note { get; init; }
    }

    /// <summary>Record a (part-)payment against an installment or an
    /// additional invoice. The parent's IsPaid flips automatically when its
    /// records cover the full amount.</summary>
    private static async Task<IResult> AddRecordAsync(
        Guid studentId, Guid enrollmentId, [FromBody] RecordBody body,
        HttpContext httpContext, IPermissionService perms, OdinDbContext db, LetterReleaseService letterRelease, CancellationToken ct)
    {
        if (await perms.AccessAsync(httpContext.User, "student.payment.records", ct) != AccessLevel.Edit) return Results.Forbid();

        var owns = await db.Enrollments.AnyAsync(e =>
            e.StudentEnrollmentId == enrollmentId && e.StudentId == studentId && e.DeletedAt == null, ct);
        if (!owns) return Results.NotFound();

        if (body.Amount <= 0)
            return Results.BadRequest(new { error = "Payment amount must be above zero." });
        if ((body.InstallmentId is null) == (body.AdditionalInvoiceId is null))
            return Results.BadRequest(new { error = "Pick exactly one of installmentId / additionalInvoiceId." });

        var record = new PaymentRecord
        {
            Amount = body.Amount,
            PaidDate = Norm(body.PaidDate) ?? DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Unspecified),
            Note = string.IsNullOrWhiteSpace(body.Note) ? null : body.Note.Trim(),
            RecordedByUserId = Guid.TryParse(
                httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var uid)
                ? uid : null,
            CreatedAt = DateTime.UtcNow,
        };

        if (body.InstallmentId is { } instId)
        {
            var inst = await db.PaymentInstallments.FirstOrDefaultAsync(i =>
                i.PaymentInstallmentId == instId && i.Plan.StudentEnrollmentId == enrollmentId, ct);
            if (inst is null) return Results.NotFound(new { error = "Installment not found." });
            record.PaymentInstallmentId = instId;
        }
        else
        {
            var inv = await db.AdditionalInvoices.FirstOrDefaultAsync(i =>
                i.AdditionalInvoiceId == body.AdditionalInvoiceId && i.Plan.StudentEnrollmentId == enrollmentId, ct);
            if (inv is null) return Results.NotFound(new { error = "Additional invoice not found." });
            record.AdditionalInvoiceId = inv.AdditionalInvoiceId;
        }

        db.PaymentRecords.Add(record);
        await db.SaveChangesAsync(ct);
        await RecomputePaidAsync(db, record.PaymentInstallmentId, record.AdditionalInvoiceId, ct);
        await RerenderReleasedLettersAsync(db, letterRelease, enrollmentId, ct);
        return await GetAsync(studentId, enrollmentId, db, ct);
    }

    private static async Task<IResult> DeleteRecordAsync(
        Guid studentId, Guid enrollmentId, Guid recordId,
        HttpContext httpContext, IPermissionService perms,
        OdinDbContext db, LetterReleaseService letterRelease, CancellationToken ct)
    {
        if (await perms.AccessAsync(httpContext.User, "student.payment.records", ct) != AccessLevel.Edit) return Results.Forbid();

        var owns = await db.Enrollments.AnyAsync(e =>
            e.StudentEnrollmentId == enrollmentId && e.StudentId == studentId && e.DeletedAt == null, ct);
        if (!owns) return Results.NotFound();

        var record = await db.PaymentRecords.FirstOrDefaultAsync(r => r.PaymentRecordId == recordId
            && (r.Installment!.Plan.StudentEnrollmentId == enrollmentId
                || r.AdditionalInvoice!.Plan.StudentEnrollmentId == enrollmentId), ct);
        if (record is null) return Results.NotFound();

        var (instId, addId) = (record.PaymentInstallmentId, record.AdditionalInvoiceId);
        db.PaymentRecords.Remove(record);
        await db.SaveChangesAsync(ct);
        await RecomputePaidAsync(db, instId, addId, ct);
        await RerenderReleasedLettersAsync(db, letterRelease, enrollmentId, ct);
        return await GetAsync(studentId, enrollmentId, db, ct);
    }

    /// <summary>Derive IsPaid/PaidDate of the record's parent from its
    /// records: paid once covered (date = latest record), unpaid otherwise.
    /// An item with NO records is left alone so the combined-invoice
    /// mark-paid flow (which sets IsPaid directly) keeps working.</summary>
    private static async Task RecomputePaidAsync(
        OdinDbContext db, Guid? installmentId, Guid? additionalInvoiceId, CancellationToken ct)
    {
        if (installmentId is { } instId)
        {
            var inst = await db.PaymentInstallments.FirstAsync(i => i.PaymentInstallmentId == instId, ct);
            var records = await db.PaymentRecords
                .Where(r => r.PaymentInstallmentId == instId)
                .Select(r => new { r.Amount, r.PaidDate })
                .ToListAsync(ct);
            if (records.Count > 0)
            {
                inst.IsPaid = inst.Amount > 0 && records.Sum(r => r.Amount) >= inst.Amount;
                inst.PaidDate = inst.IsPaid ? records.Max(r => r.PaidDate) : null;
            }
            await db.SaveChangesAsync(ct);
        }
        if (additionalInvoiceId is { } addId)
        {
            var inv = await db.AdditionalInvoices.FirstAsync(i => i.AdditionalInvoiceId == addId, ct);
            var total = AdditionalInvoiceLines.Total(inv.LinesJson);
            var records = await db.PaymentRecords
                .Where(r => r.AdditionalInvoiceId == addId)
                .Select(r => new { r.Amount, r.PaidDate })
                .ToListAsync(ct);
            if (records.Count > 0)
            {
                inv.IsPaid = total > 0 && records.Sum(r => r.Amount) >= total;
                inv.PaidDate = inv.IsPaid ? records.Max(r => r.PaidDate) : null;
            }
            await db.SaveChangesAsync(ct);
        }
    }

    /// <summary>Same best-effort re-render as a plan save: released offer /
    /// admission letters embed fee and payment tags, so they stay current.</summary>
    private static async Task RerenderReleasedLettersAsync(
        OdinDbContext db, LetterReleaseService letterRelease, Guid enrollmentId, CancellationToken ct)
    {
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
                catch { /* payment record stands even if re-render fails */ }
            }
        }
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
