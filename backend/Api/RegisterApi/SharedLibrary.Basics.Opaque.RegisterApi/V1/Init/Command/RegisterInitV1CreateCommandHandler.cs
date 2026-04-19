using Microsoft.AspNetCore.Identity;
using QuVian.SharedLibrary.Basics.SuccessOrFailures;
using QuVian.SharedLibrary.Basics.SuccessOrFailures.Helpers;
using QuVian.SharedLibrary.Basics.Repositories.Specifications;
using SharedLibrary.Basics.Opaque.Domains;
using SharedLibrary.Basics.Opaque.Domains.Repositories;
using SharedLibrary.Basics.OpaqueService;
using SharedLibrary.Basics.TransientStateCache;

namespace SharedLibrary.Basics.Opaque.RegisterApi.V1.Init.Command;


[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class RegisterInitV1CreateCommandHandler(
    IInviteCodeRepository inviteCodeRepository,
    ITransientStateCache transientStateCache,
    IGuidProvider guidProvider,
    UserManager<ApplicationUser> userManager,
    OpaqueServer opaqueServer)
    : ICommandHandler<RegisterInitV1CreateCommand, RegisterInitV1CreateCommandResult, ApplicationUser,
        IRegisterRepository>
{
    public async Task<SuccessOrFailure<RegisterInitV1CreateCommandResult>> HandleAsync(
        RegisterInitV1CreateCommand command, CancellationToken cancellationToken)
    {
        var existingApplicationUser = await userManager.FindByNameAsync(command.Username);
        if (existingApplicationUser is not null)
            return SuccessOrFailureHelper<RegisterInitV1CreateCommandResult>.EntityNotFound(
                typeof(RegisterInitV1CreateCommand));

        var assignedRole = "User";
        string? inviteCodeId = null;

        if (!string.IsNullOrWhiteSpace(command.InviteCode))
        {
            var inviteCodeSpec = new Specification<InviteCode>()
                .AddWhere(x => x.Code == command.InviteCode
                               && x.RedeemedByUserId == null
                               && x.ExpiresAt > DateTime.UtcNow);

            var inviteCode = await inviteCodeRepository.GetAsync(inviteCodeSpec, cancellationToken).ConfigureAwait(false);
            if (inviteCode is null)
                return SuccessOrFailureHelper<RegisterInitV1CreateCommandResult>.EntityNotFound(
                    typeof(RegisterInitV1CreateCommand));

            assignedRole = inviteCode.AssignedRole;
            inviteCodeId = inviteCode.Code;
        }

        if (!opaqueServer.TryConvertToBlindedElement(command.BlindedElement, out var blindedElement, out var error))
            return SuccessOrFailureHelper<RegisterInitV1CreateCommandResult>.Create(
                $"{nameof(RegisterInitV1CreateCommand)} - {error}");

        if (!opaqueServer.TryGenerateSeed(out var seed, out error))
            return SuccessOrFailureHelper<RegisterInitV1CreateCommandResult>.Create(
                $"{nameof(RegisterInitV1CreateCommand)} - {error}");

        if (!opaqueServer.TryBlindEvaluate(seed, blindedElement, out var evaluatedElement, out error))
            return SuccessOrFailureHelper<RegisterInitV1CreateCommandResult>.Create(
                $"{nameof(RegisterInitV1CreateCommand)} - {error}");

        var registrationId = guidProvider.NewId();
        var registerInitState = new RegisterInitState
        {
            Username = command.Username,
            Email = command.Email,
            Seed = seed,
            InviteCodeId = inviteCodeId,
            AssignedRole = assignedRole,
        };

        await transientStateCache.SetAsync($"reg:{registrationId}", registerInitState, TimeSpan.FromMinutes(5));

        var result = new RegisterInitV1CreateCommandResult
        {
            RegistrationId = registrationId,
            EvaluatedElement = evaluatedElement
        };

        return new SuccessOrFailure<RegisterInitV1CreateCommandResult>(result);
    }
}
