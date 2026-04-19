using Microsoft.AspNetCore.Identity;
using Odin.Api.Base.Authentication;
using QuVian.SharedLibrary.Basics.SuccessOrFailures;
using QuVian.SharedLibrary.Basics.SuccessOrFailures.Helpers;
using QuVian.SharedLibrary.Basics.Repositories.Specifications;
using SharedLibrary.Basics.Opaque.Domains;
using SharedLibrary.Basics.Opaque.Domains.Repositories;
using SharedLibrary.Basics.OpaqueService;
using SharedLibrary.Basics.TransientStateCache;

namespace SharedLibrary.Basics.Opaque.RegisterApi.V1.Finalize.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class RegisterFinalizeV1CreateCommandHandler(
    ITransientStateCache transientStateCache,
    UserManager<ApplicationUser> userManager,
    OpaqueServer opaqueServer,
    IInviteCodeRepository inviteCodeRepository,
    IUserProfileRepository userProfileRepository,
    IOpaqueCredentialRepository opaqueCredentialRepository,
    IUserContactEmailRepository userContactEmailRepository,
    SessionTokenService sessionTokenService)
    : ICommandHandler<RegisterFinalizeV1CreateCommand, RegisterFinalizeV1CreateCommandResult, ApplicationUser,
        IRegisterRepository>
{
    public async Task<SuccessOrFailure<RegisterFinalizeV1CreateCommandResult>> HandleAsync(
        RegisterFinalizeV1CreateCommand command, CancellationToken cancellationToken)
    {
        var cacheKey = $"reg:{command.RegistrationId}";
        var state = await transientStateCache.GetAsync<RegisterInitState>(cacheKey);
        if (state is null)
            return SuccessOrFailureHelper<RegisterFinalizeV1CreateCommandResult>.EntityNotFound(
                typeof(RegisterFinalizeV1CreateCommand));

        await transientStateCache.RemoveAsync(cacheKey);

        if (!opaqueServer.TryConvertToClientPublicKey(command.ClientPublicKey, out var clientPublicKey, out var error))
            return SuccessOrFailureHelper<RegisterFinalizeV1CreateCommandResult>.Create(
                $"{nameof(RegisterFinalizeV1CreateCommand)} - {error}");

        var user = new ApplicationUser
        {
            UserName = state.Username,
            Email = state.Email,
            IsEnabled = true
        };

        var createResult = await userManager.CreateAsync(user);
        if (!createResult.Succeeded)
            return SuccessOrFailureHelper<RegisterFinalizeV1CreateCommandResult>.Create(
                string.Join("; ", createResult.Errors.Select(e => e.Description)));

        await userManager.AddToRoleAsync(user, state.AssignedRole);

        if (!string.IsNullOrWhiteSpace(state.InviteCodeId))
        {
            var inviteCodeSpec = new Specification<InviteCode>()
                .AddWhere(x => x.Code == state.InviteCodeId);
            var invite = await inviteCodeRepository.GetAsync(inviteCodeSpec, cancellationToken).ConfigureAwait(false);
            if (invite is not null)
                invite.RedeemedByUserId = user.Id;
        }

        userProfileRepository.Add(new UserProfile { UserId = user.Id });
        opaqueCredentialRepository.Add(new OpaqueCredential
        {
            UserId = user.Id,
            OprfSeed = state.Seed,
            ClientPublicKey = clientPublicKey
        });
        userContactEmailRepository.Add(new UserContactEmail
        {
            UserId = user.Id,
            Email = user.Email!,
            Label = "Primary",
            IsPrimary = true
        });

        var (rawToken, _) = await sessionTokenService.CreateTokenAsync(user.Id, cancellationToken: cancellationToken);

        return new SuccessOrFailure<RegisterFinalizeV1CreateCommandResult>(
            new RegisterFinalizeV1CreateCommandResult { UserId = user.Id, Token = rawToken });
    }
}
