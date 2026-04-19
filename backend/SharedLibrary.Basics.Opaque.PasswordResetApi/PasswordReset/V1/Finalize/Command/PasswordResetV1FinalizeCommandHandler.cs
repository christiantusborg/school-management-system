using SharedLibrary.Basics.Opaque.PasswordResetApi.PasswordReset.V1;

namespace SharedLibrary.Basics.Opaque.PasswordResetApi.PasswordReset.V1.Finalize.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class PasswordResetV1FinalizeCommandHandler(
    IOpaqueCredentialRepository opaqueCredentialRepository,
    SessionTokenService sessionTokenService,
    ITransientStateCache transientStateCache,
    OpaqueServer opaqueServer)
    : ICommandHandler<PasswordResetV1FinalizeCommand, PasswordResetV1FinalizeCommandResult,
        OpaqueCredential, IOpaqueCredentialRepository>
{
    public async Task<SuccessOrFailure<PasswordResetV1FinalizeCommandResult>> HandleAsync(
        PasswordResetV1FinalizeCommand command, CancellationToken cancellationToken)
    {
        var state = await transientStateCache.GetAsync<PasswordResetInitState>($"resetreg:{command.ResetId}");
        if (state is null)
            return SuccessOrFailureHelper<PasswordResetV1FinalizeCommandResult>.Create(
                $"{nameof(PasswordResetV1FinalizeCommand)} - Invalid or expired reset session.");

        await transientStateCache.RemoveAsync($"resetreg:{command.ResetId}");
        await transientStateCache.RemoveAsync($"reset:{state.ResetToken}");

        if (!opaqueServer.TryConvertToClientPublicKey(command.ClientPublicKey, out var clientPublicKey, out var err))
            return SuccessOrFailureHelper<PasswordResetV1FinalizeCommandResult>.Create(
                $"{nameof(PasswordResetV1FinalizeCommand)} - {err}");

        var existingSpec = new Specification<OpaqueCredential>()
            .AddWhere(x => x.UserId == state.ExistingUserId);

        var existing = await opaqueCredentialRepository.GetAsync(existingSpec, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
            opaqueCredentialRepository.Remove(existing);

        opaqueCredentialRepository.Add(new OpaqueCredential
        {
            UserId = state.ExistingUserId,
            OprfSeed = state.OprfSeed,
            ClientPublicKey = clientPublicKey
        });

        var (rawToken, session) = await sessionTokenService.CreateTokenAsync(
            state.ExistingUserId, null, cancellationToken);

        return new SuccessOrFailure<PasswordResetV1FinalizeCommandResult>(new PasswordResetV1FinalizeCommandResult
        {
            Token = rawToken,
            ExpiresAt = session.ExpiresAt
        });
    }
}
