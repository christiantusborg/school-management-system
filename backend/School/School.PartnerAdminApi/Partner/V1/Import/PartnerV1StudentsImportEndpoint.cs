using School.PartnerAdminApi.Admin.V1.Students.Endpoint;
using School.PartnerAdminApi.Partner.V1.MyUsers;
using PartnerEntity = SharedLibrary.Basics.Opaque.Domains.Partners.Partner;

namespace School.PartnerAdminApi.Partner.V1.Import;

/// <summary>
/// Partner-portal CSV imports (the partner Import tab): students and grades
/// for the caller's OWN partner only. Thin wrappers around the admin import
/// cores with the partner forced from the logged-in user's record, so the
/// CSV needs no PartnerNumber column. Where imported students land (direct
/// admission vs review queue) follows Partner.ImportDirectAdmission, which
/// only the Admission Office can change; the response reports it read-only.
/// </summary>
[Route("/v1/partner/students/import")]
[EndpointTag("Partner.Import")]
public sealed class PartnerV1StudentsImportEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/partner/students/import/help", HelpAsync)
            .RequireAuthorization("PartnerOnly");
        app.MapGet("/v1/partner/students/import/sample", StudentSample)
            .RequireAuthorization("PartnerOnly");
        app.MapPost("/v1/partner/students/import/validate", StudentValidateAsync)
            .RequireAuthorization("PartnerOnly").DisableAntiforgery();
        app.MapPost("/v1/partner/students/import", StudentImportAsync)
            .RequireAuthorization("PartnerOnly").DisableAntiforgery();

        app.MapGet("/v1/partner/students/import/grades/sample", GradeSample)
            .RequireAuthorization("PartnerOnly");
        app.MapPost("/v1/partner/students/import/grades/validate", GradeValidateAsync)
            .RequireAuthorization("PartnerOnly").DisableAntiforgery();
        app.MapPost("/v1/partner/students/import/grades", GradeImportAsync)
            .RequireAuthorization("PartnerOnly").DisableAntiforgery();
        return app;
    }

    private static async Task<(PartnerEntity? Partner, string? UserId, IResult? Fail)> ResolvePartnerAsync(
        HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var (userId, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null) return (null, null, fail);
        var partner = await db.Partners
            .FirstOrDefaultAsync(p => p.PartnerId == partnerId && p.DeletedAt == null, ct);
        return partner is null ? (null, null, Results.NotFound()) : (partner, userId, null);
    }

    private static IResult StudentSample() => AdminV1StudentsImportEndpoint.SampleFile(scoped: true);

    /// <summary>Import help .txt scoped to the caller's partner.</summary>
    private static async Task<IResult> HelpAsync(
        HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;
        var text = await ImportHelpText.BuildAsync(db, scoped: true, partnerId, ct);
        return Results.File(System.Text.Encoding.UTF8.GetBytes(text), "text/plain", "import-help.txt");
    }

    private static async Task<IResult> StudentValidateAsync(
        HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var (partner, _, fail) = await ResolvePartnerAsync(httpContext, db, ct);
        if (fail is not null) return fail;
        return await AdminV1StudentsImportEndpoint.ValidateCoreAsync(db, httpContext.Request, partner, ct);
    }

    private static async Task<IResult> StudentImportAsync(
        HttpContext httpContext, OdinDbContext db,
        [FromServices] OpaqueUserCreationService creator, CancellationToken ct)
    {
        var (partner, userId, fail) = await ResolvePartnerAsync(httpContext, db, ct);
        if (fail is not null) return fail;
        var actorId = Guid.TryParse(userId, out var g) ? g : Guid.Empty;
        return await AdminV1StudentsImportEndpoint.ImportCoreAsync(
            db, creator, httpContext.Request, partner, actorId, ct);
    }

    private static IResult GradeSample() => AdminV1StudentsGradeImportEndpoint.Sample();

    private static async Task<IResult> GradeValidateAsync(
        HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var (partner, _, fail) = await ResolvePartnerAsync(httpContext, db, ct);
        if (fail is not null) return fail;
        return await AdminV1StudentsGradeImportEndpoint.ValidateCoreAsync(
            db, httpContext.Request, partner!.PartnerId, ct);
    }

    private static async Task<IResult> GradeImportAsync(
        HttpContext httpContext, OdinDbContext db,
        [FromServices] Odin.Api.Base.Letters.LetterReleaseService letterRelease, CancellationToken ct)
    {
        var (partner, _, fail) = await ResolvePartnerAsync(httpContext, db, ct);
        if (fail is not null) return fail;
        return await AdminV1StudentsGradeImportEndpoint.ImportCoreAsync(
            db, letterRelease, httpContext.Request, partner!.PartnerId, ct);
    }
}
