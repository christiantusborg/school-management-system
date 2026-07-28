using System.Security.Claims;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using School.PartnerAdminApi.Partner.V1.MyUsers;
using SharedLibrary.Basics.Opaque.Domains.Payments;

namespace School.PartnerAdminApi.Partner.V1.Invoices;

/// <summary>
/// Combined invoices: the partner ticks any number of their students' unpaid
/// payment items (plan installments + additional invoices) and generates ONE
/// numbered invoice with a line per item; Admission sees the same list per
/// partner and marks a combined invoice paid, which marks every underlying
/// student item paid. Items sitting in an active combined invoice are locked
/// out of the pick list.
/// </summary>
[Route("/v1/partner/my/invoices")]
[EndpointTag("Partner.Invoices")]
public sealed class PartnerV1InvoicesEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/partner/my/invoices/items", ItemsAsync).RequireAuthorization("PartnerOnly");
        app.MapGet("/v1/partner/my/invoices", ListPartnerAsync).RequireAuthorization("PartnerOnly");
        app.MapPost("/v1/partner/my/invoices", CreateAsync).RequireAuthorization("PartnerOnly");
        app.MapGet("/v1/partner/my/invoices/{invoiceId:guid}/pdf", PdfPartnerAsync).RequireAuthorization("PartnerOnly");

        app.MapGet("/v1/admin/partners/{partnerId:guid}/invoices", ListAdminAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/partners/{partnerId:guid}/invoices/{invoiceId:guid}/pdf", PdfAdminAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/admin/partners/{partnerId:guid}/invoices/{invoiceId:guid}/mark-paid", MarkPaidAsync).RequireAuthorization("AdminOnly");
        return app;
    }

    private sealed record OpenItem(
        Guid? InstallmentId, Guid? InvoiceId, Guid EnrollmentId, string Student,
        string StudentNumber, string ProgrammeCode, string Label, decimal Amount,
        string Currency, DateTime? DueDate);

    private static decimal InvoiceAmount(string linesJson)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(linesJson);
            decimal sum = 0;
            foreach (var line in doc.RootElement.EnumerateArray())
                foreach (var prop in line.EnumerateObject())
                    if (prop.Name.Contains("amount", StringComparison.OrdinalIgnoreCase)
                        && prop.Value.ValueKind == System.Text.Json.JsonValueKind.Number)
                        sum += prop.Value.GetDecimal();
            return sum;
        }
        catch { return 0; }
    }

    /// <summary>Unpaid, not-yet-combined items across every student of the partner.</summary>
    private static async Task<List<OpenItem>> LoadOpenItemsAsync(OdinDbContext db, Guid partnerId, CancellationToken ct)
    {
        // Items already sitting in a live combined invoice are locked.
        var combinedInstallments = await (
            from l in db.CombinedInvoiceLines
            join ci in db.CombinedInvoices on l.CombinedInvoiceId equals ci.CombinedInvoiceId
            where ci.DeletedAt == null && l.PaymentInstallmentId != null
            select l.PaymentInstallmentId!.Value).ToListAsync(ct);
        var combinedInvoices = await (
            from l in db.CombinedInvoiceLines
            join ci in db.CombinedInvoices on l.CombinedInvoiceId equals ci.CombinedInvoiceId
            where ci.DeletedAt == null && l.AdditionalInvoiceId != null
            select l.AdditionalInvoiceId!.Value).ToListAsync(ct);
        var lockedInst = combinedInstallments.ToHashSet();
        var lockedInv = combinedInvoices.ToHashSet();

        var inst = await (
            from i in db.PaymentInstallments
            join pl in db.EnrollmentPaymentPlans on i.PaymentPlanId equals pl.PaymentPlanId
            join e in db.Enrollments on pl.StudentEnrollmentId equals e.StudentEnrollmentId
            join st in db.Students on e.StudentId equals st.StudentId
            where pl.DeletedAt == null && e.DeletedAt == null && st.DeletedAt == null
                && e.PartnerId == partnerId && !i.IsPaid
            select new
            {
                i.PaymentInstallmentId,
                e.StudentEnrollmentId,
                st.StudentNumber,
                Name = db.UserProfiles.Where(p => p.UserId == st.UserId)
                    .Select(p => ((p.FirstName ?? "") + " " + (p.LastName ?? "")).Trim()).FirstOrDefault(),
                ProgrammeCode = e.Specialization.Programmes.Code,
                i.Sequence,
                i.Amount,
                pl.Currency,
                i.DueDate,
            }).ToListAsync(ct);
        var inv = await (
            from a in db.AdditionalInvoices
            join pl in db.EnrollmentPaymentPlans on a.PaymentPlanId equals pl.PaymentPlanId
            join e in db.Enrollments on pl.StudentEnrollmentId equals e.StudentEnrollmentId
            join st in db.Students on e.StudentId equals st.StudentId
            where pl.DeletedAt == null && e.DeletedAt == null && st.DeletedAt == null
                && e.PartnerId == partnerId && !a.IsPaid
            select new
            {
                a.AdditionalInvoiceId,
                e.StudentEnrollmentId,
                st.StudentNumber,
                Name = db.UserProfiles.Where(p => p.UserId == st.UserId)
                    .Select(p => ((p.FirstName ?? "") + " " + (p.LastName ?? "")).Trim()).FirstOrDefault(),
                ProgrammeCode = e.Specialization.Programmes.Code,
                a.Sequence,
                a.LinesJson,
                pl.Currency,
                a.DueDate,
            }).ToListAsync(ct);

        return inst.Where(x => !lockedInst.Contains(x.PaymentInstallmentId))
            .Select(x => new OpenItem(x.PaymentInstallmentId, null, x.StudentEnrollmentId,
                x.Name ?? "?", x.StudentNumber ?? "?", x.ProgrammeCode ?? "", $"Installment {x.Sequence}",
                x.Amount, x.Currency, x.DueDate))
            .Concat(inv.Where(x => !lockedInv.Contains(x.AdditionalInvoiceId))
                .Select(x => new OpenItem(null, x.AdditionalInvoiceId, x.StudentEnrollmentId,
                    x.Name ?? "?", x.StudentNumber ?? "?", x.ProgrammeCode ?? "", $"Additional invoice {x.Sequence}",
                    InvoiceAmount(x.LinesJson), x.Currency, x.DueDate)))
            .OrderBy(x => x.Student).ThenBy(x => x.Label)
            .ToList();
    }

    private static async Task<IResult> ItemsAsync(HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null || partnerId is null) return fail ?? Results.StatusCode(403);
        var items = await LoadOpenItemsAsync(db, partnerId.Value, ct);
        return Results.Ok(new
        {
            items = items.Select(x => new
            {
                installmentId = x.InstallmentId,
                invoiceId = x.InvoiceId,
                enrollmentId = x.EnrollmentId,
                student = x.Student,
                studentNumber = x.StudentNumber,
                programmeCode = x.ProgrammeCode,
                label = x.Label,
                amount = x.Amount,
                currency = x.Currency,
                dueDate = x.DueDate,
            }).ToList(),
        });
    }

    private static async Task<object> ShapeListAsync(OdinDbContext db, Guid partnerId, CancellationToken ct)
    {
        var invoices = await db.CombinedInvoices
            .Where(i => i.PartnerId == partnerId && i.DeletedAt == null)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct);
        var ids = invoices.Select(i => i.CombinedInvoiceId).ToList();
        var lines = await db.CombinedInvoiceLines
            .Where(l => ids.Contains(l.CombinedInvoiceId))
            .ToListAsync(ct);
        return new
        {
            items = invoices.Select(i => new
            {
                id = i.CombinedInvoiceId,
                number = i.Number,
                status = i.Status,
                createdAt = i.CreatedAt,
                paidAt = i.PaidAt,
                totals = lines.Where(l => l.CombinedInvoiceId == i.CombinedInvoiceId)
                    .GroupBy(l => l.Currency)
                    .Select(g => new { currency = g.Key, amount = g.Sum(x => x.Amount) })
                    .ToList(),
                lines = lines.Where(l => l.CombinedInvoiceId == i.CombinedInvoiceId)
                    .Select(l => new
                    {
                        student = l.StudentName,
                        studentNumber = l.StudentNumber,
                        programmeCode = l.ProgrammeCode,
                        label = l.ItemLabel,
                        amount = l.Amount,
                        currency = l.Currency,
                    }).ToList(),
            }).ToList(),
        };
    }

    private static async Task<IResult> ListPartnerAsync(HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null || partnerId is null) return fail ?? Results.StatusCode(403);
        return Results.Ok(await ShapeListAsync(db, partnerId.Value, ct));
    }

    private static async Task<IResult> ListAdminAsync(Guid partnerId, OdinDbContext db, CancellationToken ct) =>
        Results.Ok(await ShapeListAsync(db, partnerId, ct));

    public sealed class CreateBody
    {
        public List<Guid>? InstallmentIds { get; init; }
        public List<Guid>? InvoiceIds { get; init; }
    }

    private static async Task<IResult> CreateAsync(
        HttpContext httpContext, [FromBody] CreateBody body, OdinDbContext db, CancellationToken ct)
    {
        var (callerId, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null || partnerId is null) return fail ?? Results.StatusCode(403);

        var wantedInst = (body.InstallmentIds ?? []).ToHashSet();
        var wantedInv = (body.InvoiceIds ?? []).ToHashSet();
        if (wantedInst.Count + wantedInv.Count == 0)
            return Results.BadRequest(new { error = "Select at least one item." });

        // Validate against the CURRENT open-item set: guarantees partner
        // ownership, unpaid state and not-already-combined in one sweep.
        var open = await LoadOpenItemsAsync(db, partnerId.Value, ct);
        var lines = open.Where(x =>
            (x.InstallmentId is { } ii && wantedInst.Contains(ii))
            || (x.InvoiceId is { } vi && wantedInv.Contains(vi))).ToList();
        if (lines.Count != wantedInst.Count + wantedInv.Count)
            return Results.BadRequest(new { error = "Some selected items are no longer open — refresh and try again." });

        var partnerName = await db.Partners.Where(p => p.PartnerId == partnerId)
            .Select(p => p.Name).FirstOrDefaultAsync(ct) ?? "PARTNER";
        var seq = await db.CombinedInvoices.IgnoreQueryFilters()
            .CountAsync(i => i.PartnerId == partnerId, ct) + 1;
        var condensed = new string(partnerName.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        if (condensed.Length > 12) condensed = condensed[..12];

        var invoice = new CombinedInvoice
        {
            PartnerId = partnerId.Value,
            Number = $"INV-{condensed}-{seq:D3}",
            CreatedByUserId = callerId,
        };
        db.CombinedInvoices.Add(invoice);
        foreach (var l in lines)
            db.CombinedInvoiceLines.Add(new CombinedInvoiceLine
            {
                CombinedInvoiceId = invoice.CombinedInvoiceId,
                PaymentInstallmentId = l.InstallmentId,
                AdditionalInvoiceId = l.InvoiceId,
                StudentEnrollmentId = l.EnrollmentId,
                StudentName = l.Student,
                StudentNumber = l.StudentNumber,
                ProgrammeCode = l.ProgrammeCode,
                ItemLabel = l.Label,
                Amount = l.Amount,
                Currency = l.Currency,
            });
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { id = invoice.CombinedInvoiceId, number = invoice.Number });
    }

    private static async Task<IResult> MarkPaidAsync(
        Guid partnerId, Guid invoiceId, HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var invoice = await db.CombinedInvoices.FirstOrDefaultAsync(i =>
            i.CombinedInvoiceId == invoiceId && i.PartnerId == partnerId && i.DeletedAt == null, ct);
        if (invoice is null) return Results.NotFound();
        if (invoice.Status == CombinedInvoice.StatusPaid)
            return Results.BadRequest(new { error = "This invoice is already marked paid." });

        var lines = await db.CombinedInvoiceLines
            .Where(l => l.CombinedInvoiceId == invoiceId)
            .ToListAsync(ct);
        var now = DateTime.UtcNow;

        var instIds = lines.Where(l => l.PaymentInstallmentId != null).Select(l => l.PaymentInstallmentId!.Value).ToList();
        var installments = await db.PaymentInstallments.Where(i => instIds.Contains(i.PaymentInstallmentId)).ToListAsync(ct);
        foreach (var i in installments) { i.IsPaid = true; i.PaidDate ??= now; }

        var invIds = lines.Where(l => l.AdditionalInvoiceId != null).Select(l => l.AdditionalInvoiceId!.Value).ToList();
        var addInvoices = await db.AdditionalInvoices.Where(a => invIds.Contains(a.AdditionalInvoiceId)).ToListAsync(ct);
        foreach (var a in addInvoices) { a.IsPaid = true; a.PaidDate ??= now; }

        invoice.Status = CombinedInvoice.StatusPaid;
        invoice.PaidAt = now;
        invoice.PaidByUserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { id = invoiceId, status = invoice.Status, itemsMarked = installments.Count + addInvoices.Count });
    }

    // ── PDF ────────────────────────────────────────────────────────────────

    private static async Task<IResult> PdfPartnerAsync(
        Guid invoiceId, HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null || partnerId is null) return fail ?? Results.StatusCode(403);
        return await BuildPdfAsync(db, partnerId.Value, invoiceId, ct);
    }

    private static Task<IResult> PdfAdminAsync(Guid partnerId, Guid invoiceId, OdinDbContext db, CancellationToken ct) =>
        BuildPdfAsync(db, partnerId, invoiceId, ct);

    private static async Task<IResult> BuildPdfAsync(OdinDbContext db, Guid partnerId, Guid invoiceId, CancellationToken ct)
    {
        var invoice = await db.CombinedInvoices.FirstOrDefaultAsync(i =>
            i.CombinedInvoiceId == invoiceId && i.PartnerId == partnerId && i.DeletedAt == null, ct);
        if (invoice is null) return Results.NotFound();
        var lines = await db.CombinedInvoiceLines
            .Where(l => l.CombinedInvoiceId == invoiceId)
            .OrderBy(l => l.StudentName).ThenBy(l => l.ItemLabel)
            .ToListAsync(ct);
        var partnerName = await db.Partners.Where(p => p.PartnerId == partnerId)
            .Select(p => p.Name).FirstOrDefaultAsync(ct) ?? "";

        var pdf = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(col =>
                {
                    col.Item().Text("MGW — Combined Invoice").FontSize(17).Bold().FontColor("#003366");
                    col.Item().Text($"{invoice.Number} · {partnerName}").FontSize(11).FontColor("#333333");
                    col.Item().Text($"Issued {invoice.CreatedAt:yyyy-MM-dd} · Status: {invoice.Status}"
                        + (invoice.PaidAt is { } p ? $" ({p:yyyy-MM-dd})" : ""))
                        .FontSize(9).FontColor("#555555");
                    col.Item().PaddingTop(10);
                });

                page.Content().Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3);   // student
                            c.RelativeColumn(2);   // number
                            c.RelativeColumn(2);   // programme
                            c.RelativeColumn(2.2f); // item
                            c.RelativeColumn(1.6f); // amount
                        });
                        foreach (var h in new[] { "Student", "Student #", "Programme", "Item", "Amount" })
                            table.Cell().Background("#f0f3f7").Padding(4).Text(h).Bold().FontSize(8);
                        var odd = false;
                        foreach (var l in lines)
                        {
                            odd = !odd;
                            var bg = odd ? "#ffffff" : "#f8fafc";
                            table.Cell().Background(bg).Padding(4).Text(l.StudentName);
                            table.Cell().Background(bg).Padding(4).Text(l.StudentNumber);
                            table.Cell().Background(bg).Padding(4).Text(l.ProgrammeCode);
                            table.Cell().Background(bg).Padding(4).Text(l.ItemLabel);
                            table.Cell().Background(bg).Padding(4).AlignRight().Text($"{l.Amount:N2} {l.Currency}");
                        }
                    });

                    col.Item().PaddingTop(12).AlignRight().Column(totals =>
                    {
                        foreach (var g in lines.GroupBy(l => l.Currency))
                            totals.Item().Text($"Total {g.Key}: {g.Sum(x => x.Amount):N2}").FontSize(11).Bold().FontColor("#003366");
                        totals.Item().Text($"{lines.Count} item(s) · {lines.Select(l => l.StudentNumber).Distinct().Count()} student(s)")
                            .FontSize(8).FontColor("#555555");
                    });
                });

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span($"{invoice.Number} · Page ").FontSize(7);
                    x.CurrentPageNumber().FontSize(7);
                    x.Span(" of ").FontSize(7);
                    x.TotalPages().FontSize(7);
                });
            });
        }).GeneratePdf();

        return Results.File(pdf, "application/pdf", $"{invoice.Number}.pdf");
    }
}
