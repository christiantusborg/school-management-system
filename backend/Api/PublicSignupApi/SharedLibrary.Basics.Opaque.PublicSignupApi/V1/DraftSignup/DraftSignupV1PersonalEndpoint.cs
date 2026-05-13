using Odin.Api.Base.Authentication;

namespace SharedLibrary.Basics.Opaque.PublicSignupApi.V1.DraftSignup;

[Route("/v1/public/draft-signup/personal")]
[EndpointTag("Public.DraftSignup")]
public sealed class DraftSignupV1PersonalEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPatch("/v1/public/draft-signup/personal", HandleAsync).AllowAnonymous();
        return app;
    }

    public sealed class PersonalRequest
    {
        public DateTime? DateOfBirth { get; init; }
        public string? PassportId { get; init; }
        public int? NationalityId { get; init; }
        public string? AddressLine1 { get; init; }
        public string? AddressLine2 { get; init; }
        public string? City { get; init; }
        public string? StateRegion { get; init; }
        public string? PostalCode { get; init; }
        public string? CountryCode { get; init; }
        public string? Phone { get; init; }
    }

    private static async Task<IResult> HandleAsync(
        HttpContext http,
        [FromBody] PersonalRequest body,
        OdinDbContext db,
        WizardSessionService wizard,
        CancellationToken ct)
    {
        var session = await WizardTokenAuth.ResolveAsync(http, wizard);
        if (session is null) return WizardTokenAuth.Unauthorised();

        var student = await db.Students.FirstOrDefaultAsync(s => s.StudentId == session.StudentId, ct);
        if (student is null) return WizardTokenAuth.Unauthorised();

        student.DateOfBirth = body.DateOfBirth;
        student.PassportId = body.PassportId;
        student.NationalityId = body.NationalityId;

        var profile = await db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == session.UserId, ct);
        if (profile is not null) profile.DateOfBirth = body.DateOfBirth;

        // Upsert primary UserAddress (Street + City + State + ZipCode + Country).
        // The wizard's two address lines are concatenated into Street since the
        // entity only has one street field.
        var address = await db.UserAddresses.FirstOrDefaultAsync(a => a.UserId == session.UserId && a.IsPrimary, ct);
        if (address is null)
        {
            address = new SharedLibrary.Basics.Opaque.Domains.UserAddress
            {
                UserId = session.UserId,
                IsPrimary = true,
                Label = "Primary",
            };
            db.UserAddresses.Add(address);
        }
        var streetCombined = string.IsNullOrWhiteSpace(body.AddressLine2)
            ? body.AddressLine1
            : $"{body.AddressLine1}\n{body.AddressLine2}";
        address.Street = streetCombined;
        address.City = body.City;
        address.State = body.StateRegion;
        address.ZipCode = body.PostalCode;
        address.Country = body.CountryCode;

        // Optional primary phone.
        if (!string.IsNullOrWhiteSpace(body.Phone))
        {
            var phone = await db.UserPhones.FirstOrDefaultAsync(p => p.UserId == session.UserId && p.IsPrimary, ct);
            if (phone is null)
            {
                db.UserPhones.Add(new SharedLibrary.Basics.Opaque.Domains.UserPhone
                {
                    UserId = session.UserId,
                    Number = body.Phone.Trim(),
                    IsPrimary = true,
                    Label = "Primary",
                });
            }
            else
            {
                phone.Number = body.Phone.Trim();
            }
        }

        if (student.WizardStep < 2) student.WizardStep = 2;

        await db.SaveChangesAsync(ct);
        return Results.Ok(new { studentId = student.StudentId });
    }
}
