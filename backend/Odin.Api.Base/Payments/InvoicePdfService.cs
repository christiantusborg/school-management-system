using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Odin.Api.Base.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Odin.Api.Base.Payments;

/// <summary>
/// Renders a tuition invoice PDF for an enrolment's payment plan: school header,
/// student + programme details, the installment schedule (with due dates and
/// paid status), and the total / balance. Also renders the plan's manually
/// created additional invoices (one-off fees, paid as a whole). Returns null
/// when no plan exists.
/// </summary>
public sealed class InvoicePdfService(OdinDbContext db)
{
    /// <summary>
    /// Renders an invoice. When <paramref name="installmentSequence"/> is given,
    /// the invoice is for that single tuition installment (Installment X of N).
    /// When <paramref name="additionalSequence"/> is given, it is for that
    /// additional invoice (its lines, paid as a whole). Otherwise the full
    /// invoice: the whole schedule plus all additional invoices. Returns null
    /// when no plan (or no such installment / additional invoice).
    /// </summary>
    public async Task<byte[]?> RenderAsync(Guid enrollmentId, CancellationToken ct,
        int? installmentSequence = null, int? additionalSequence = null)
    {
        var plan = await db.EnrollmentPaymentPlans
            .Where(p => p.StudentEnrollmentId == enrollmentId && p.DeletedAt == null)
            .Select(p => new
            {
                p.TotalTuitionFee,
                p.Currency,
                Installments = p.Installments.OrderBy(i => i.Sequence)
                    .Select(i => new { i.Sequence, i.Amount, i.DueDate, i.IsPaid, i.PaidDate,
                        i.PayByCardEnabled, i.CardPaymentLink, i.PayByBankEnabled, i.BankAccountDetails }).ToList(),
                AdditionalInvoices = p.AdditionalInvoices.OrderBy(i => i.Sequence)
                    .Select(i => new { i.Sequence, i.DueDate, i.IsPaid, i.PaidDate,
                        i.PayByCardEnabled, i.CardPaymentLink, i.PayByBankEnabled, i.BankAccountDetails, i.LinesJson }).ToList(),
            })
            .FirstOrDefaultAsync(ct);
        if (plan is null) return null;

        var enr = await db.Enrollments
            .Where(e => e.StudentEnrollmentId == enrollmentId)
            .Select(e => new
            {
                e.StudentId,
                ProgrammeName = e.Specialization.Programmes.Name,
                SchoolName = e.Specialization.Programmes.School != null ? e.Specialization.Programmes.School.Name : null,
                SpecName = e.Specialization.Name,
                StudentNumber = db.Students.Where(s => s.StudentId == e.StudentId).Select(s => s.StudentNumber).FirstOrDefault(),
                UserId = db.Students.Where(s => s.StudentId == e.StudentId).Select(s => s.UserId).FirstOrDefault(),
            })
            .FirstOrDefaultAsync(ct);

        string studentName = enr?.StudentNumber ?? string.Empty;
        string email = string.Empty;
        if (enr?.UserId is { } uid)
        {
            var profile = await db.UserProfiles.Where(p => p.UserId == uid)
                .Select(p => new { p.FirstName, p.LastName }).FirstOrDefaultAsync(ct);
            var nm = string.Join(' ', new[] { profile?.FirstName, profile?.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
            if (!string.IsNullOrWhiteSpace(nm)) studentName = nm;
            email = await db.Users.Where(u => u.Id == uid).Select(u => u.Email ?? string.Empty).FirstOrDefaultAsync(ct) ?? string.Empty;
        }

        var cur = plan.Currency;
        string Money(decimal v) => $"{cur} {v.ToString("N2", CultureInfo.InvariantCulture)}";
        string Due(DateTime? d) => d?.ToString("dd MMM yyyy", CultureInfo.InvariantCulture) ?? "—";

        var additional = plan.AdditionalInvoices.Select(a => new
        {
            a.Sequence, a.DueDate, a.IsPaid, a.PaidDate,
            a.PayByCardEnabled, a.CardPaymentLink, a.PayByBankEnabled, a.BankAccountDetails,
            Lines = AdditionalInvoiceLines.Parse(a.LinesJson),
            Total = AdditionalInvoiceLines.Total(a.LinesJson),
            Title = AdditionalInvoiceLines.Title(a.LinesJson, a.Sequence),
        }).ToList();

        var additionalTotal = additional.Sum(a => a.Total);
        var totalPaid = plan.Installments.Where(i => i.IsPaid).Sum(i => i.Amount)
            + additional.Where(a => a.IsPaid).Sum(a => a.Total);
        var grandTotal = plan.TotalTuitionFee + additionalTotal;
        var balance = grandTotal - totalPaid;
        var n = plan.Installments.Count;

        // Single-installment / single-additional-invoice modes.
        var focus = installmentSequence is { } seq
            ? plan.Installments.FirstOrDefault(i => i.Sequence == seq)
            : null;
        if (installmentSequence is not null && focus is null) return null;
        var focusAdd = additionalSequence is { } aseq
            ? additional.FirstOrDefault(a => a.Sequence == aseq)
            : null;
        if (additionalSequence is not null && focusAdd is null) return null;

        // "Paid": the focused installment / additional invoice when one is
        // requested; the full invoice only once the whole balance is settled.
        var isPaid = focus is not null ? focus.IsPaid
            : focusAdd is not null ? focusAdd.IsPaid
            : totalPaid > 0 && balance <= 0;

        // Payment instructions are configured per installment / per additional
        // invoice. The full invoice shows the next unpaid installment's
        // instructions (the one due now). Every invoice must carry payment
        // information, so when the chosen one has none configured, fall back
        // to any that does (installments first, then additional invoices).
        var methodSources = plan.Installments
            .Select(i => new MethodSource(i.PayByCardEnabled, i.CardPaymentLink, i.PayByBankEnabled, i.BankAccountDetails, i.IsPaid))
            .Concat(additional.Select(a => new MethodSource(a.PayByCardEnabled, a.CardPaymentLink, a.PayByBankEnabled, a.BankAccountDetails, a.IsPaid)))
            .ToList();
        var methods = focus is not null
            ? new MethodSource(focus.PayByCardEnabled, focus.CardPaymentLink, focus.PayByBankEnabled, focus.BankAccountDetails, focus.IsPaid)
            : focusAdd is not null
                ? new MethodSource(focusAdd.PayByCardEnabled, focusAdd.CardPaymentLink, focusAdd.PayByBankEnabled, focusAdd.BankAccountDetails, focusAdd.IsPaid)
                : methodSources.FirstOrDefault(m => !m.IsPaid) ?? methodSources.FirstOrDefault();
        if (methods is not null && !methods.HasAny)
            methods = methodSources.FirstOrDefault(m => m.HasAny) ?? methods;

        var title = focus is not null ? $"TUITION INVOICE — Installment {focus.Sequence} of {n}"
            : focusAdd is not null ? $"INVOICE — {focusAdd.Title}"
            : "TUITION INVOICE";

        var bytes = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(t => t.FontSize(10).FontColor(Colors.Black));

                page.Header().Column(col =>
                {
                    col.Item().Text(enr?.SchoolName ?? "MGW").FontSize(18).Bold().FontColor(Colors.Blue.Darken3);
                    col.Item().Text("Part of MY GLOBAL WORLD EDUCATION").FontSize(9).FontColor(Colors.Grey.Darken1);
                    col.Item().PaddingTop(10).Text(title).FontSize(14).Bold();
                });

                page.Content().PaddingVertical(12).Column(col =>
                {
                    col.Spacing(4);
                    void Row(string k, string v) => col.Item().Row(r =>
                    {
                        r.ConstantItem(140).Text(k).SemiBold().FontColor(Colors.Grey.Darken2);
                        r.RelativeItem().Text(v);
                    });
                    Row("Student", studentName);
                    if (!string.IsNullOrWhiteSpace(email)) Row("Email", email);
                    Row("Student ID", enr?.StudentNumber ?? "—");
                    Row("Programme", enr?.ProgrammeName ?? "—");
                    Row("Specialization", enr?.SpecName ?? "—");

                    if (focusAdd is not null)
                    {
                        // ── Additional invoice: its lines, paid as one ────────
                        Row("Invoice", $"Additional invoice {focusAdd.Sequence}");
                        Row("Due date", Due(focusAdd.DueDate));
                        Row("Status", focusAdd.IsPaid ? $"Paid{(focusAdd.PaidDate is { } pd ? $" on {pd:dd MMM yyyy}" : "")}" : "Unpaid");

                        col.Item().PaddingTop(14).Text("Invoice lines").FontSize(12).Bold();
                        col.Item().PaddingTop(4).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(6);   // description
                                c.RelativeColumn(3);   // amount
                            });
                            void H(string t) => table.Cell().Background(Colors.Grey.Lighten3).Border(0.5f).Padding(4).Text(t).Bold().FontSize(9);
                            H("Description"); H("Amount");
                            foreach (var l in focusAdd.Lines)
                            {
                                void C(string t) => table.Cell().Border(0.5f).Padding(4).Text(t).FontSize(9);
                                C(string.IsNullOrWhiteSpace(l.Text) ? "—" : l.Text);
                                C(Money(l.Amount));
                            }
                            table.Cell().Border(0.5f).Padding(4).Text("Total").Bold().FontSize(9);
                            table.Cell().Border(0.5f).Padding(4).Text(Money(focusAdd.Total)).Bold().FontSize(9);
                        });

                        col.Item().PaddingTop(14).AlignRight().Column(t =>
                        {
                            t.Item().Text($"Amount due this invoice: {Money(focusAdd.IsPaid ? 0m : focusAdd.Total)}").SemiBold();
                        });
                    }
                    else
                    {
                        Row("Total tuition fee", Money(plan.TotalTuitionFee));
                        if (focus is not null)
                        {
                            Row($"Installment {focus.Sequence} of {n}", Money(focus.Amount));
                            Row("Due date", Due(focus.DueDate));
                            Row("Status", focus.IsPaid ? $"Paid{(focus.PaidDate is { } pd ? $" on {pd:dd MMM yyyy}" : "")}" : "Unpaid");
                        }

                        // The installment table: just the focused row, or the whole schedule.
                        var rows = focus is null ? plan.Installments : new[] { focus }.ToList();
                        col.Item().PaddingTop(14).Text(focus is null ? "Payment schedule" : "This installment").FontSize(12).Bold();
                        col.Item().PaddingTop(4).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(60);   // #
                                c.RelativeColumn(3);     // amount
                                c.RelativeColumn(3);     // due date
                                c.RelativeColumn(3);     // status
                            });
                            void H(string t) => table.Cell().Background(Colors.Grey.Lighten3).Border(0.5f).Padding(4).Text(t).Bold().FontSize(9);
                            H("#"); H("Amount"); H("Due date"); H("Status");
                            foreach (var i in rows)
                            {
                                void C(string t) => table.Cell().Border(0.5f).Padding(4).Text(t).FontSize(9);
                                C(i.Sequence.ToString());
                                C(Money(i.Amount));
                                C(Due(i.DueDate));
                                C(i.IsPaid
                                    ? $"Paid{(i.PaidDate is { } pd ? $" · {pd:dd MMM yyyy}" : "")}"
                                    : "Unpaid");
                            }
                        });

                        // Additional invoices only appear on the full invoice.
                        if (focus is null && additional.Count > 0)
                        {
                            col.Item().PaddingTop(14).Text("Additional invoices").FontSize(12).Bold();
                            col.Item().PaddingTop(4).Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.ConstantColumn(60);   // #
                                    c.RelativeColumn(4);     // description
                                    c.RelativeColumn(2);     // amount
                                    c.RelativeColumn(2);     // due date
                                    c.RelativeColumn(2);     // status
                                });
                                void H(string t) => table.Cell().Background(Colors.Grey.Lighten3).Border(0.5f).Padding(4).Text(t).Bold().FontSize(9);
                                H("#"); H("Description"); H("Amount"); H("Due date"); H("Status");
                                foreach (var a in additional)
                                {
                                    void C(string t) => table.Cell().Border(0.5f).Padding(4).Text(t).FontSize(9);
                                    C($"A{a.Sequence}");
                                    C(a.Title);
                                    C(Money(a.Total));
                                    C(Due(a.DueDate));
                                    C(a.IsPaid
                                        ? $"Paid{(a.PaidDate is { } pd ? $" · {pd:dd MMM yyyy}" : "")}"
                                        : "Unpaid");
                                }
                            });
                        }

                        col.Item().PaddingTop(14).AlignRight().Column(t =>
                        {
                            if (focus is not null)
                                t.Item().Text($"Amount due this installment: {Money(focus.IsPaid ? 0m : focus.Amount)}").SemiBold();
                            if (focus is null && additionalTotal > 0)
                            {
                                t.Item().Text($"Tuition total: {Money(plan.TotalTuitionFee)}").SemiBold();
                                t.Item().Text($"Additional invoices: {Money(additionalTotal)}").SemiBold();
                                t.Item().Text($"Grand total: {Money(grandTotal)}").SemiBold();
                            }
                            t.Item().Text($"Total paid: {Money(totalPaid)}").SemiBold();
                            t.Item().Text($"Balance due: {Money(balance)}").FontSize(12).Bold()
                                .FontColor(balance > 0 ? Colors.Red.Darken2 : Colors.Green.Darken2);
                        });
                    }

                    // Payment instructions (Admission-Office configured per
                    // installment / invoice). Shown on every invoice, paid or not.
                    var showBank = methods is { PayByBankEnabled: true } && !string.IsNullOrWhiteSpace(methods.BankAccountDetails);
                    var showCard = methods is { PayByCardEnabled: true } && !string.IsNullOrWhiteSpace(methods.CardPaymentLink);
                    if (showBank || showCard)
                    {
                        col.Item().PaddingTop(18).Text("How to pay").FontSize(12).Bold();
                        if (showBank)
                        {
                            col.Item().PaddingTop(6).Text("Bank transfer").SemiBold().FontColor(Colors.Grey.Darken2);
                            col.Item().Text(methods!.BankAccountDetails!.Trim()).FontSize(9);
                        }
                        if (showCard)
                        {
                            col.Item().PaddingTop(6).Text("Pay online by credit card").SemiBold().FontColor(Colors.Grey.Darken2);
                            col.Item().Hyperlink(methods!.CardPaymentLink!.Trim())
                                .Text(methods!.CardPaymentLink!.Trim()).FontSize(9)
                                .FontColor(Colors.Blue.Darken2).Underline();
                        }
                    }
                });

                // Diagonal PAID stamp across the page once settled.
                if (isPaid)
                {
                    page.Foreground()
                        .TranslateX(297.5f).TranslateY(421)
                        .Rotate(-40)
                        .TranslateX(-146).TranslateY(-78)
                        .Text("PAID").FontSize(130).ExtraBold()
                        .FontColor(Color.FromHex("#50D32F2F"));
                }

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("My Global World Education Group (MGW)")
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                });
            });
        }).GeneratePdf();

        return bytes;
    }

    /// <summary>Payment-method fields shared by installments and additional
    /// invoices, used to pick which instructions to print.</summary>
    private sealed record MethodSource(
        bool PayByCardEnabled, string? CardPaymentLink,
        bool PayByBankEnabled, string? BankAccountDetails, bool IsPaid)
    {
        public bool HasAny =>
            (PayByCardEnabled && !string.IsNullOrWhiteSpace(CardPaymentLink))
            || (PayByBankEnabled && !string.IsNullOrWhiteSpace(BankAccountDetails));
    }
}
