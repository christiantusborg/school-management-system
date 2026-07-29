using System.Security.Claims;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Odin.Api.Base.Letters;
using Odin.Api.Base.Storage;
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
        // Partner: read-only — sees and downloads what Admission combined.
        app.MapGet("/v1/partner/my/invoices", ListPartnerAsync).RequireAuthorization("PartnerOnly");
        app.MapGet("/v1/partner/my/invoices/{invoiceId:guid}/pdf", PdfPartnerAsync).RequireAuthorization("PartnerOnly");

        // Admission: picks the items, combines, deletes (1h window; SuperAdmin
        // always, never while Paid), marks paid, and SuperAdmin may revert.
        app.MapGet("/v1/admin/partners/{partnerId:guid}/invoices/items", ItemsAdminAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/admin/partners/{partnerId:guid}/invoices", CreateAdminAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/partners/{partnerId:guid}/invoices", ListAdminAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/partners/{partnerId:guid}/invoices/{invoiceId:guid}/pdf", PdfAdminAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/admin/partners/{partnerId:guid}/invoices/{invoiceId:guid}/mark-paid", MarkPaidAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/admin/partners/{partnerId:guid}/invoices/{invoiceId:guid}/unmark-paid", UnmarkPaidAsync).RequireAuthorization("AdminOnly");
        app.MapDelete("/v1/admin/partners/{partnerId:guid}/invoices/{invoiceId:guid}", DeleteAsync).RequireAuthorization("AdminOnly");

        // Per-partner invoice design template (letter-designer layout).
        app.MapGet("/v1/admin/partners/{partnerId:guid}/invoice-template", GetTemplateAsync).RequireAuthorization("AdminOnly");
        app.MapPut("/v1/admin/partners/{partnerId:guid}/invoice-template", SaveTemplateAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/admin/partners/{partnerId:guid}/invoice-template/preview", PreviewTemplateAsync).RequireAuthorization("AdminOnly");
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

    private static async Task<IResult> ItemsAdminAsync(Guid partnerId, OdinDbContext db, CancellationToken ct)
    {
        var items = await LoadOpenItemsAsync(db, partnerId, ct);
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

    private static async Task<IResult> CreateAdminAsync(
        Guid partnerId, HttpContext httpContext, [FromBody] CreateBody body, OdinDbContext db, CancellationToken ct)
    {
        if (!await db.Partners.AnyAsync(p => p.PartnerId == partnerId && p.DeletedAt == null, ct))
            return Results.NotFound();
        var callerId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var wantedInst = (body.InstallmentIds ?? []).ToHashSet();
        var wantedInv = (body.InvoiceIds ?? []).ToHashSet();
        if (wantedInst.Count + wantedInv.Count == 0)
            return Results.BadRequest(new { error = "Select at least one item." });

        // Validate against the CURRENT open-item set: guarantees partner
        // ownership, unpaid state and not-already-combined in one sweep.
        var open = await LoadOpenItemsAsync(db, partnerId, ct);
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
            PartnerId = partnerId,
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

    /// <summary>Delete rules: never while Paid (SuperAdmin must revert
    /// first); SuperAdministrator always; every other admission user only
    /// within 1 hour of creation. Soft delete unlocks the underlying items
    /// back into the pick list.</summary>
    private static async Task<IResult> DeleteAsync(
        Guid partnerId, Guid invoiceId, HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var invoice = await db.CombinedInvoices.FirstOrDefaultAsync(i =>
            i.CombinedInvoiceId == invoiceId && i.PartnerId == partnerId && i.DeletedAt == null, ct);
        if (invoice is null) return Results.NotFound();
        if (invoice.Status == CombinedInvoice.StatusPaid)
            return Results.BadRequest(new { error = "A paid invoice cannot be deleted — mark it unpaid first (SuperAdministrator only)." });
        var isSuper = httpContext.User.IsInRole("SuperAdministrator");
        if (!isSuper && DateTime.UtcNow - invoice.CreatedAt > TimeSpan.FromHours(1))
            return Results.Json(new { error = "Only a SuperAdministrator can delete an invoice older than 1 hour." },
                statusCode: StatusCodes.Status403Forbidden);
        invoice.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { id = invoiceId, deleted = true });
    }

    /// <summary>SuperAdministrator only: reverts Paid → Open and un-pays the
    /// underlying items that this invoice settled.</summary>
    private static async Task<IResult> UnmarkPaidAsync(
        Guid partnerId, Guid invoiceId, HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        if (!httpContext.User.IsInRole("SuperAdministrator"))
            return Results.Json(new { error = "Only a SuperAdministrator can mark an invoice unpaid." },
                statusCode: StatusCodes.Status403Forbidden);
        var invoice = await db.CombinedInvoices.FirstOrDefaultAsync(i =>
            i.CombinedInvoiceId == invoiceId && i.PartnerId == partnerId && i.DeletedAt == null, ct);
        if (invoice is null) return Results.NotFound();
        if (invoice.Status != CombinedInvoice.StatusPaid)
            return Results.BadRequest(new { error = "This invoice is not marked paid." });

        var lines = await db.CombinedInvoiceLines
            .Where(l => l.CombinedInvoiceId == invoiceId)
            .ToListAsync(ct);
        var instIds = lines.Where(l => l.PaymentInstallmentId != null).Select(l => l.PaymentInstallmentId!.Value).ToList();
        foreach (var i in await db.PaymentInstallments.Where(x => instIds.Contains(x.PaymentInstallmentId)).ToListAsync(ct))
        { i.IsPaid = false; i.PaidDate = null; }
        var invIds = lines.Where(l => l.AdditionalInvoiceId != null).Select(l => l.AdditionalInvoiceId!.Value).ToList();
        foreach (var a in await db.AdditionalInvoices.Where(x => invIds.Contains(x.AdditionalInvoiceId)).ToListAsync(ct))
        { a.IsPaid = false; a.PaidDate = null; }

        invoice.Status = CombinedInvoice.StatusOpen;
        invoice.PaidAt = null;
        invoice.PaidByUserId = null;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { id = invoiceId, status = invoice.Status });
    }

    // ── Per-partner invoice template (designer layout) ─────────────────────

    private static readonly object[] InvoiceTags =
    [
        new { tag = "[partner name]", description = "The partner's name" },
        new { tag = "[invoice number]", description = "Combined invoice number (INV-…)" },
        new { tag = "[invoice date]", description = "Date the invoice was created" },
        new { tag = "[invoice status]", description = "Open or Paid (with paid date)" },
        new { tag = "[invoice total]", description = "Grand total per currency" },
        new { tag = "[invoice item count]", description = "Number of bundled items" },
        new { tag = "[invoice student count]", description = "Number of distinct students" },
        new { tag = "[date]", description = "Today's date" },
    ];

    private static async Task<IResult> GetTemplateAsync(Guid partnerId, OdinDbContext db, CancellationToken ct)
    {
        var template = await db.CombinedInvoiceTemplates
            .FirstOrDefaultAsync(t => t.PartnerId == partnerId, ct);
        return Results.Ok(new
        {
            certificateLayoutJson = template?.CertificateLayoutJson,
            tags = InvoiceTags,
        });
    }

    public sealed class TemplateBody { public string? CertificateLayoutJson { get; init; } }

    private static async Task<IResult> SaveTemplateAsync(
        Guid partnerId, [FromBody] TemplateBody body, OdinDbContext db, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.CertificateLayoutJson))
            return Results.BadRequest(new { error = "Layout missing." });
        var template = await db.CombinedInvoiceTemplates
            .FirstOrDefaultAsync(t => t.PartnerId == partnerId, ct);
        if (template is null)
        {
            template = new CombinedInvoiceTemplate { PartnerId = partnerId };
            db.CombinedInvoiceTemplates.Add(template);
        }
        template.CertificateLayoutJson = body.CertificateLayoutJson;
        template.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { saved = true });
    }

    private static Dictionary<string, string> InvoiceTagValues(
        CombinedInvoice invoice, IReadOnlyList<CombinedInvoiceLine> lines, string partnerName)
    {
        var totals = string.Join(" · ", lines.GroupBy(l => l.Currency)
            .Select(g => $"{g.Sum(x => x.Amount):N2} {g.Key}"));
        return new Dictionary<string, string>
        {
            ["[partner name]"] = partnerName,
            ["[invoice number]"] = invoice.Number,
            ["[invoice date]"] = invoice.CreatedAt.ToString("yyyy-MM-dd"),
            ["[invoice status]"] = invoice.Status + (invoice.PaidAt is { } p ? $" ({p:yyyy-MM-dd})" : ""),
            ["[invoice total]"] = totals,
            ["[invoice item count]"] = lines.Count.ToString(),
            ["[invoice student count]"] = lines.Select(l => l.StudentNumber).Distinct().Count().ToString(),
            ["[date]"] = DateTime.UtcNow.ToString("yyyy-MM-dd"),
        };
    }

    private static async Task<Dictionary<Guid, byte[]>> LoadLayoutAssetsAsync(
        OdinDbContext db, IFileStorage storage, CertificateLayout layout, CancellationToken ct)
    {
        var ids = new HashSet<Guid>();
        foreach (var page in layout.Pages ?? [])
        {
            if (page.BackgroundAssetId is { } bg) ids.Add(bg);
            foreach (var f in page.Fields ?? [])
                if (f.ImageAssetId is { } img) ids.Add(img);
        }
        var dict = new Dictionary<Guid, byte[]>();
        foreach (var id in ids)
        {
            var path = await db.LetterAssets
                .Where(a => a.LetterAssetId == id && a.DeletedAt == null)
                .Select(a => a.StoragePath)
                .FirstOrDefaultAsync(ct);
            if (path is null) continue;
            try
            {
                using var stream = await storage.OpenReadAsync(path, ct);
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms, ct);
                dict[id] = ms.ToArray();
            }
            catch { /* missing asset renders as blank */ }
        }
        return dict;
    }

    private static async Task<IResult> PreviewTemplateAsync(
        Guid partnerId, [FromBody] TemplateBody body, OdinDbContext db,
        IFileStorage storage, LetterPdfRenderer renderer, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.CertificateLayoutJson))
            return Results.BadRequest(new { error = "Layout missing." });
        CertificateLayout? layout;
        try { layout = System.Text.Json.JsonSerializer.Deserialize<CertificateLayout>(body.CertificateLayoutJson); }
        catch { return Results.BadRequest(new { error = "Invalid layout." }); }
        if (layout is null) return Results.BadRequest(new { error = "Invalid layout." });

        var partnerName = await db.Partners.Where(p => p.PartnerId == partnerId)
            .Select(p => p.Name).FirstOrDefaultAsync(ct) ?? "Partner";
        var sampleInvoice = new CombinedInvoice { Number = "INV-SAMPLE-001", CreatedAt = DateTime.UtcNow };
        var sampleLines = new List<CombinedInvoiceLine>
        {
            new() { StudentName = "Sample Student", StudentNumber = "ST-20260101-AAAA", ProgrammeCode = "MBA-IBAS", ItemLabel = "Installment 1", Amount = 1500m, Currency = "USD" },
            new() { StudentName = "Second Student", StudentNumber = "ST-20260101-BBBB", ProgrammeCode = "BBA-IBSS", ItemLabel = "Installment 2", Amount = 900m, Currency = "USD" },
            new() { StudentName = "Third Student", StudentNumber = "ST-20260101-CCCC", ProgrammeCode = "DBA-IBSS", ItemLabel = "Additional invoice 1", Amount = 250m, Currency = "USD" },
        };
        var assets = await LoadLayoutAssetsAsync(db, storage, layout, ct);
        var pdf = renderer.RenderCertificate(layout, assets,
            InvoiceTagValues(sampleInvoice, sampleLines, partnerName), invoiceLines: sampleLines);
        return Results.File(pdf, "application/pdf", "invoice-template-preview.pdf");
    }

    // ── PDF ────────────────────────────────────────────────────────────────

    private static async Task<IResult> PdfPartnerAsync(
        Guid invoiceId, HttpContext httpContext, OdinDbContext db,
        IFileStorage storage, LetterPdfRenderer renderer, CancellationToken ct)
    {
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null || partnerId is null) return fail ?? Results.StatusCode(403);
        return await BuildPdfAsync(db, storage, renderer, partnerId.Value, invoiceId, ct);
    }

    private static Task<IResult> PdfAdminAsync(
        Guid partnerId, Guid invoiceId, OdinDbContext db,
        IFileStorage storage, LetterPdfRenderer renderer, CancellationToken ct) =>
        BuildPdfAsync(db, storage, renderer, partnerId, invoiceId, ct);

    private static async Task<IResult> BuildPdfAsync(
        OdinDbContext db, IFileStorage storage, LetterPdfRenderer renderer,
        Guid partnerId, Guid invoiceId, CancellationToken ct)
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

        // Partner-specific designer template wins over the built-in document.
        var template = await db.CombinedInvoiceTemplates
            .FirstOrDefaultAsync(t => t.PartnerId == partnerId, ct);
        if (template is not null && !string.IsNullOrWhiteSpace(template.CertificateLayoutJson)
            && template.CertificateLayoutJson != "{}")
        {
            try
            {
                var tplLayout = System.Text.Json.JsonSerializer.Deserialize<CertificateLayout>(template.CertificateLayoutJson);
                if (tplLayout is not null)
                {
                    var tplAssets = await LoadLayoutAssetsAsync(db, storage, tplLayout, ct);
                    var tplPdf = renderer.RenderCertificate(tplLayout, tplAssets,
                        InvoiceTagValues(invoice, lines, partnerName), invoiceLines: lines);
                    return Results.File(tplPdf, "application/pdf", $"{invoice.Number}.pdf");
                }
            }
            catch { /* fall through to the built-in document */ }
        }

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
