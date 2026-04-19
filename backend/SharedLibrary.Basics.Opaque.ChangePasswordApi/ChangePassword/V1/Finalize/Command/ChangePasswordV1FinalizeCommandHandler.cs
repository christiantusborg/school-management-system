using QuVian.SharedLibrary.Basics.SuccessOrFailures;
using QuVian.SharedLibrary.Basics.SuccessOrFailures.Helpers;
using QuVian.SharedLibrary.Basics.Repositories.Specifications;
using SharedLibrary.Basics.Opaque.Domains;
using SharedLibrary.Basics.Opaque.Domains.Repositories;
using SharedLibrary.Basics.OpaqueService;
using SharedLibrary.Basics.TransientStateCache;

namespace SharedLibrary.Basics.Opaque.ChangePasswordApi.ChangePassword.V1.Finalize.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class ChangePasswordV1FinalizeCommandHandler(
    ITransientStateCache transientStateCache,
    OpaqueServer opaqueServer,
    IOpaqueCredentialRepository repository)
    : ICommandHandler<ChangePasswordV1FinalizeCommand, ChangePasswordV1FinalizeCommandResult,
        OpaqueCredential, IOpaqueCredentialRepository>
{
    public async Task<SuccessOrFailure<ChangePasswordV1FinalizeCommandResult>> HandleAsync(
        ChangePasswordV1FinalizeCommand command, CancellationToken cancellationToken)
    {
        var cacheKey = $"changepw:{command.ChangeId}";
        var state = await transientStateCache.GetAsync<ChangePasswordV1InitState>(cacheKey);
        if (state is null)
            return SuccessOrFailureHelper<ChangePasswordV1FinalizeCommandResult>.EntityNotFound(
                typeof(ChangePasswordV1FinalizeCommand));

        await transientStateCache.RemoveAsync(cacheKey);

        // Verify old password signature
        byte[] signatureBytes;
        try { signatureBytes = Convert.FromBase64String(command.Signature); }
        catch
        {
            return SuccessOrFailureHelper<ChangePasswordV1FinalizeCommandResult>.Create(
                $"{nameof(ChangePasswordV1FinalizeCommand)} - Invalid signature encoding.");
        }

        if (!opaqueServer.VerifySignature(signatureBytes, state.Challenge, state.ExistingClientPublicKey))
            return SuccessOrFailureHelper<ChangePasswordV1FinalizeCommandResult>.Create(
                "Current password is incorrect.");

        // Validate new public key
        if (!opaqueServer.TryConvertToClientPublicKey(command.ClientPublicKey, out var keyBytes, out var err))
            return SuccessOrFailureHelper<ChangePasswordV1FinalizeCommandResult>.Create(
                $"{nameof(ChangePasswordV1FinalizeCommand)} - {err}");

        var existing = (await repository.SearchAsync(
            new Specification<OpaqueCredential>().AddWhere(x => x.UserId == state.UserId),
            cancellationToken).ConfigureAwait(false)).FirstOrDefault();

        if (existing is null)
            return SuccessOrFailureHelper<ChangePasswordV1FinalizeCommandResult>.EntityNotFound(
                typeof(ChangePasswordV1FinalizeCommand));

        existing.OprfSeed = state.OprfSeed;
        existing.ClientPublicKey = keyBytes;

        return new SuccessOrFailure<ChangePasswordV1FinalizeCommandResult>(
            new ChangePasswordV1FinalizeCommandResult { ChangeId = command.ChangeId });
    }
}
