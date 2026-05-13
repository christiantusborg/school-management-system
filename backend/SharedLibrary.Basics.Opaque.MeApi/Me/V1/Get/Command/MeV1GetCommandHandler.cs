using Microsoft.EntityFrameworkCore;
using Odin.Api.Base.Data;

namespace SharedLibrary.Basics.Opaque.MeApi.Me.V1.Get.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MeV1GetCommandHandler(
    UserManager<ApplicationUser> userManager,
    OdinDbContext db)
    : ICommandHandler<MeV1GetCommand, MeV1GetCommandResult, ApplicationUser, IApplicationUserRepository>
{
    public async Task<SuccessOrFailure<MeV1GetCommandResult>> HandleAsync(
        MeV1GetCommand command, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(command.UserId);
        if (user is null)
            return SuccessOrFailureHelper<MeV1GetCommandResult>.EntityNotFound(typeof(MeV1GetCommand));

        var roles = await userManager.GetRolesAsync(user);

        // Partner users carry a PartnerId on the ApplicationUser record. Surface
        // the slug so the frontend can build partner-scoped URLs (e.g. the
        // student signup wizard URL needed by "+ Add Student").
        string? partnerSlug = null;
        if (user.PartnerId is { } pid)
        {
            partnerSlug = await db.Partners
                .Where(p => p.PartnerId == pid && p.DeletedAt == null)
                .Select(p => p.Slug)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new SuccessOrFailure<MeV1GetCommandResult>(new MeV1GetCommandResult
        {
            UserId = user.Id,
            Username = user.UserName!,
            Email = user.Email,
            Roles = roles.ToArray(),
            IsEnabled = user.IsEnabled,
            CreatedAt = user.CreatedAt,
            PartnerSlug = partnerSlug,
        });
    }
}
