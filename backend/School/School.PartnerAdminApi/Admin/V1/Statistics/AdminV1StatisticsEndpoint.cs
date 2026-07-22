using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace School.PartnerAdminApi.Admin.V1.Statistics;

/// <summary>
/// Dashboard → Statistics: outcome percentages for the cohort of enrolments
/// whose COMMENCEMENT date falls in the chosen period — collapsible trees
/// Partner→Programme→Specialization and School→Programme→Specialization.
/// Outcomes: Passed (GradesApproved), Dropped Out, Deferred, everything
/// else = Still active. Drafts/rejected are excluded — they never
/// commenced. /export renders the same data as CSV or PDF.
/// </summary>
[Route("/v1/admin/statistics")]
[EndpointTag("Admin.Statistics")]
public sealed class AdminV1StatisticsEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/admin/statistics/outcomes", OutcomesAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/statistics/outcomes/export", ExportAsync).RequireAuthorization("AdminOnly");
        return app;
    }

    private static readonly string[] ExcludedCodes =
    [
        "Draft", "ApplicationRejectedByPartner", "ApplicationRejectedByAdmission",
    ];

    // ── Typed model ────────────────────────────────────────────────────────

    private sealed record Bucket(
        string Label, int Total,
        int Passed, double PassedPct,
        int Dropped, double DroppedPct,
        int Deferred, double DeferredPct,
        int Active, double ActivePct)
    {
        internal static Bucket From(string label, List<string> codes)
        {
            var total = codes.Count;
            var passed = codes.Count(c => c == "GradesApproved");
            var dropped = codes.Count(c => c == "DroppedOut");
            var deferred = codes.Count(c => c == "Deferred");
            var active = total - passed - dropped - deferred;
            static double Pct(int n, int t) => t == 0 ? 0 : Math.Round(n * 100.0 / t, 1);
            return new Bucket(label, total,
                passed, Pct(passed, total),
                dropped, Pct(dropped, total),
                deferred, Pct(deferred, total),
                active, Pct(active, total));
        }
    }

    private sealed record ProgrammeNode(Bucket Programme, List<Bucket> Specializations);
    private sealed record TreeNode(Bucket Node, List<ProgrammeNode> Programmes);
    private sealed record Outcomes(List<TreeNode> ByPartner, List<TreeNode> BySchool, Bucket Overall);

    private static async Task<Outcomes> BuildAsync(
        OdinDbContext db, DateTime? from, DateTime? to, CancellationToken ct)
    {
        var fromD = from is { } f ? DateTime.SpecifyKind(f.Date, DateTimeKind.Unspecified) : (DateTime?)null;
        var toD = to is { } t ? DateTime.SpecifyKind(t.Date, DateTimeKind.Unspecified) : (DateTime?)null;

        var rows = await (
            from e in db.Enrollments
            join sp in db.Specializations on e.SpecializationId equals sp.SpecializationId
            join p in db.Programmes on sp.ProgrammeId equals p.ProgrammeId
            where e.DeletedAt == null
                && e.CommencementDate != null
                && !ExcludedCodes.Contains(e.Status.Code)
                && (fromD == null || e.CommencementDate >= fromD)
                && (toD == null || e.CommencementDate <= toD)
            select new
            {
                PartnerName = db.Partners.Where(x => x.PartnerId == e.PartnerId).Select(x => x.Name).FirstOrDefault(),
                SchoolName = db.Schools.Where(x => x.SchoolId == p.SchoolId).Select(x => x.Name).FirstOrDefault(),
                ProgrammeLabel = (p.Code ?? "") + " — " + p.Name,
                SpecializationName = sp.Name,
                StatusCode = e.Status.Code,
            }).ToListAsync(ct);

        List<ProgrammeNode> ProgrammeTree(IEnumerable<(string Prog, string? Spec, string Code)> subset) => subset
            .GroupBy(x => x.Prog)
            .OrderBy(g => g.Key)
            .Select(g => new ProgrammeNode(
                Bucket.From(g.Key, g.Select(x => x.Code).ToList()),
                g.GroupBy(x => x.Spec ?? "(no specialization)")
                    .OrderBy(sg => sg.Key)
                    .Select(sg => Bucket.From(sg.Key, sg.Select(x => x.Code).ToList()))
                    .ToList()))
            .ToList();

        var byPartner = rows
            .GroupBy(r => r.PartnerName ?? "(no partner)")
            .OrderBy(g => g.Key)
            .Select(g => new TreeNode(
                Bucket.From(g.Key, g.Select(x => x.StatusCode).ToList()),
                ProgrammeTree(g.Select(x => (x.ProgrammeLabel, (string?)x.SpecializationName, x.StatusCode)))))
            .ToList();
        var bySchool = rows
            .GroupBy(r => r.SchoolName ?? "(no school)")
            .OrderBy(g => g.Key)
            .Select(g => new TreeNode(
                Bucket.From(g.Key, g.Select(x => x.StatusCode).ToList()),
                ProgrammeTree(g.Select(x => (x.ProgrammeLabel, (string?)x.SpecializationName, x.StatusCode)))))
            .ToList();
        var overall = Bucket.From("All", rows.Select(x => x.StatusCode).ToList());
        return new Outcomes(byPartner, bySchool, overall);
    }

    // ── JSON for the page ──────────────────────────────────────────────────

    private static object Shape(Bucket b) => new
    {
        label = b.Label,
        total = b.Total,
        passed = b.Passed, passedPct = b.PassedPct,
        dropped = b.Dropped, droppedPct = b.DroppedPct,
        deferred = b.Deferred, deferredPct = b.DeferredPct,
        active = b.Active, activePct = b.ActivePct,
    };

    private static async Task<IResult> OutcomesAsync(
        OdinDbContext db, CancellationToken ct,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var o = await BuildAsync(db, from, to, ct);
        object Tree(TreeNode n) => new
        {
            bucket = Shape(n.Node),
            programmes = n.Programmes.Select(pg => new
            {
                programme = Shape(pg.Programme),
                specializations = pg.Specializations.Select(Shape).ToList(),
            }).ToList(),
        };
        return Results.Ok(new
        {
            byPartner = o.ByPartner.Select(Tree).ToList(),
            bySchool = o.BySchool.Select(Tree).ToList(),
            overall = Shape(o.Overall),
        });
    }

    // ── Exports ────────────────────────────────────────────────────────────

    private static async Task<IResult> ExportAsync(
        OdinDbContext db, CancellationToken ct,
        [FromQuery] string? format = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var o = await BuildAsync(db, from, to, ct);
        var period = (from, to) switch
        {
            (null, null) => "all time",
            ({ } f, null) => $"from {f:yyyy-MM-dd}",
            (null, { } t) => $"until {t:yyyy-MM-dd}",
            ({ } f, { } t) => $"{f:yyyy-MM-dd} to {t:yyyy-MM-dd}",
        };
        var stamp = DateTime.UtcNow.ToString("yyyyMMdd");
        return string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase)
            ? Results.File(BuildPdf(o, period), "application/pdf", $"statistics-{stamp}.pdf")
            : Results.File(BuildCsv(o), "text/csv", $"statistics-{stamp}.csv");
    }

    private static byte[] BuildCsv(Outcomes o)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Section,Group,Programme,Specialization,Students,Passed,Passed %,Dropped Out,Dropped %,Deferred,Deferred %,Still active,Active %");
        string E(string v) => v.Contains(',') || v.Contains('"') ? $"\"{v.Replace("\"", "\"\"")}\"" : v;
        void Line(string section, string group, string prog, string spec, Bucket b) =>
            sb.AppendLine($"{E(section)},{E(group)},{E(prog)},{E(spec)},{b.Total},{b.Passed},{b.PassedPct},{b.Dropped},{b.DroppedPct},{b.Deferred},{b.DeferredPct},{b.Active},{b.ActivePct}");
        void Tree(string section, List<TreeNode> nodes)
        {
            foreach (var n in nodes)
            {
                Line(section, n.Node.Label, "", "", n.Node);
                foreach (var pg in n.Programmes)
                {
                    Line(section, n.Node.Label, pg.Programme.Label, "", pg.Programme);
                    foreach (var sp in pg.Specializations)
                        Line(section, n.Node.Label, pg.Programme.Label, sp.Label, sp);
                }
            }
        }
        Tree("Partner", o.ByPartner);
        Tree("School", o.BySchool);
        Line("All", "All", "", "", o.Overall);
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    private static byte[] BuildPdf(Outcomes o, string period)
    {
        return Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(col =>
                {
                    col.Item().Text("MGW — Outcome Statistics").FontSize(16).Bold().FontColor("#003366");
                    col.Item().Text($"Period: {period} · generated {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC · " +
                        $"Total {o.Overall.Total} · Passed {o.Overall.PassedPct}% · Dropped {o.Overall.DroppedPct}% · Deferred {o.Overall.DeferredPct}%")
                        .FontSize(9).FontColor("#555555");
                    col.Item().PaddingTop(6);
                });

                page.Content().Column(col =>
                {
                    void Table(string title, List<TreeNode> nodes)
                    {
                        col.Item().PaddingTop(8).Text(title).FontSize(12).Bold().FontColor("#003366");
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(4);
                                c.RelativeColumn(1);
                                for (var i = 0; i < 4; i++) c.RelativeColumn(2);
                            });
                            void Header(string t) => table.Cell().Background("#f0f3f7").Padding(3).Text(t).Bold().FontSize(8);
                            Header("Group / Programme / Specialization");
                            Header("Students"); Header("Passed"); Header("Dropped Out"); Header("Deferred"); Header("Still active");

                            void Row(string label, Bucket b, int indent, bool bold, string? bg)
                            {
                                IContainer Boxed()
                                {
                                    var cell = table.Cell();
                                    return bg is null ? cell.Padding(3) : cell.Background(bg).Padding(3);
                                }
                                var tl = Boxed().PaddingLeft(indent * 12).Text(label);
                                if (bold) tl.Bold();
                                foreach (var text in new[]
                                {
                                    b.Total.ToString(),
                                    $"{b.PassedPct}% ({b.Passed})",
                                    $"{b.DroppedPct}% ({b.Dropped})",
                                    $"{b.DeferredPct}% ({b.Deferred})",
                                    $"{b.ActivePct}% ({b.Active})",
                                })
                                {
                                    var t = Boxed().Text(text);
                                    if (bold) t.Bold();
                                }
                            }

                            foreach (var n in nodes)
                            {
                                Row(n.Node.Label, n.Node, 0, bold: true, bg: "#e8eef6");
                                foreach (var pg in n.Programmes)
                                {
                                    Row(pg.Programme.Label, pg.Programme, 1, bold: false, bg: "#f6f9fd");
                                    foreach (var sp in pg.Specializations)
                                        Row("- " + sp.Label, sp, 2, bold: false, bg: null);
                                }
                            }
                        });
                    }
                    Table("Per partner", o.ByPartner);
                    Table("Per school", o.BySchool);
                });

                page.Footer().AlignRight().Text(t =>
                {
                    t.Span("Page ").FontSize(8);
                    t.CurrentPageNumber().FontSize(8);
                    t.Span(" of ").FontSize(8);
                    t.TotalPages().FontSize(8);
                });
            });
        }).GeneratePdf();
    }
}
