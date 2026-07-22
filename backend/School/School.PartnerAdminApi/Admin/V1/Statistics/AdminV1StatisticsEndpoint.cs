namespace School.PartnerAdminApi.Admin.V1.Statistics;

/// <summary>
/// Dashboard → Statistics: outcome percentages for the cohort of enrolments
/// whose COMMENCEMENT date falls in the chosen period — grouped per partner
/// and per school (the school of the enrolment's programme). Outcomes:
/// Passed (GradesApproved), Dropped Out, Deferred, everything else = Still
/// active. Rejected/draft applications are excluded — they never commenced.
/// </summary>
[Route("/v1/admin/statistics")]
[EndpointTag("Admin.Statistics")]
public sealed class AdminV1StatisticsEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/admin/statistics/outcomes", OutcomesAsync).RequireAuthorization("AdminOnly");
        return app;
    }

    private static readonly string[] ExcludedCodes =
    [
        "Draft", "ApplicationRejectedByPartner", "ApplicationRejectedByAdmission",
    ];

    private static async Task<IResult> OutcomesAsync(
        OdinDbContext db, CancellationToken ct,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
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

        static object Bucket(string label, List<string> codes)
        {
            var total = codes.Count;
            int Count(Func<string, bool> pred) => codes.Count(pred);
            var passed = Count(c => c == "GradesApproved");
            var dropped = Count(c => c == "DroppedOut");
            var deferred = Count(c => c == "Deferred");
            var active = total - passed - dropped - deferred;
            static double Pct(int n, int total) => total == 0 ? 0 : Math.Round(n * 100.0 / total, 1);
            return new
            {
                label,
                total,
                passed, passedPct = Pct(passed, total),
                dropped, droppedPct = Pct(dropped, total),
                deferred, deferredPct = Pct(deferred, total),
                active, activePct = Pct(active, total),
            };
        }

        var byPartner = rows
            .GroupBy(r => r.PartnerName ?? "(no partner)")
            .OrderBy(g => g.Key)
            .Select(g => Bucket(g.Key, g.Select(x => x.StatusCode).ToList()))
            .ToList();
        var bySchool = rows
            .GroupBy(r => r.SchoolName ?? "(no school)")
            .OrderBy(g => g.Key)
            .Select(g => Bucket(g.Key, g.Select(x => x.StatusCode).ToList()))
            .ToList();
        // Programme aggregate + one nested bucket per specialization.
        var byProgramme = rows
            .GroupBy(r => r.ProgrammeLabel)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                programme = Bucket(g.Key, g.Select(x => x.StatusCode).ToList()),
                specializations = g
                    .GroupBy(x => x.SpecializationName ?? "(no specialization)")
                    .OrderBy(sg => sg.Key)
                    .Select(sg => Bucket(sg.Key, sg.Select(x => x.StatusCode).ToList()))
                    .ToList(),
            })
            .ToList();
        var overall = Bucket("All", rows.Select(x => x.StatusCode).ToList());

        return Results.Ok(new { byPartner, bySchool, byProgramme, overall });
    }
}
