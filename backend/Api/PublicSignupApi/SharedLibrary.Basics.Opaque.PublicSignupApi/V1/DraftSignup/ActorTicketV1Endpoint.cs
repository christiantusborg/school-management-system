using System.Security.Claims;
using SharedLibrary.Basics.TransientStateCache;

namespace SharedLibrary.Basics.Opaque.PublicSignupApi.V1.DraftSignup;

/// <summary>
/// Mints a short-lived "actor ticket" for the staff add-student modal. The
/// wizard iframe passes the ticket to /draft-signup/start, which resolves it
/// back to the staff user so the created student carries "added by" — without
/// ever exposing the staff session token to the public wizard. Random id,
/// 2-hour TTL, usable only for creation attribution.
/// </summary>
[Route("/v1/admin/students/actor-ticket")]
[EndpointTag("Public.DraftSignup")]
public sealed class ActorTicketV1Endpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/admin/students/actor-ticket", HandleAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/partner/my-students/actor-ticket", HandleAsync).RequireAuthorization("PartnerOnly");
        return app;
    }

    private static async Task<IResult> HandleAsync(
        HttpContext httpContext, ITransientStateCache cache)
    {
        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

        var ticket = Guid.NewGuid().ToString("N");
        await cache.SetAsync($"wizactor:{ticket}", userId, TimeSpan.FromHours(2));
        return Results.Ok(new { ticket });
    }
}
