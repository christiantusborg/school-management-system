using QuVian.SharedLibrary.Basics.SuccessOrFailures;
using QuVian.SharedLibrary.Basics.SuccessOrFailures.Helpers;
using QuVian.SharedLibrary.Basics.Repositories.Specifications;
using SharedLibrary.Basics.Opaque.Domains;
using SharedLibrary.Basics.Opaque.Domains.Repositories;
using SharedLibrary.Basics.OpaqueService;
using SharedLibrary.Basics.TransientStateCache;

namespace SharedLibrary.Basics.Opaque.ChangePasswordApi.ChangePassword.V1.Init.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class ChangePasswordV1InitCommandHandler(
    OpaqueServer opaqueServer,
    ITransientStateCache transientStateCache,
    IGuidProvider guidProvider,
    IOpaqueCredentialRepository credentialRepository)
    : ICommandHandler<ChangePasswordV1InitCommand, ChangePasswordV1InitCommandResult,
        OpaqueCredential, IOpaqueCredentialRepository>
{
    public async Task<SuccessOrFailure<ChangePasswordV1InitCommandResult>> HandleAsync(
        ChangePasswordV1InitCommand command, CancellationToken cancellationToken)
    {
        // Validate blinded elements
        if (!opaqueServer.TryConvertToBlindedElement(command.OldBlindedElement, out var oldBlindedBytes, out var err))
            return SuccessOrFailureHelper<ChangePasswordV1InitCommandResult>.Create(
                $"{nameof(ChangePasswordV1InitCommand)} - {err}");

        if (!opaqueServer.TryConvertToBlindedElement(command.BlindedElement, out var newBlindedBytes, out err))
            return SuccessOrFailureHelper<ChangePasswordV1InitCommandResult>.Create(
                $"{nameof(ChangePasswordV1InitCommand)} - {err}");

        // Look up existing credential to evaluate OPRF for old password
        var spec = new Specification<OpaqueCredential>()
            .AddWhere(x => x.UserId == command.UserId);
        var existing = (await credentialRepository.SearchAsync(spec, cancellationToken).ConfigureAwait(false))
            .FirstOrDefault();

        if (existing is null)
            return SuccessOrFailureHelper<ChangePasswordV1InitCommandResult>.EntityNotFound(
                typeof(ChangePasswordV1InitCommand));

        // Evaluate OPRF for old password using existing seed
        if (!opaqueServer.TryBlindEvaluate(existing.OprfSeed, oldBlindedBytes, out var oldEvaluated, out err))
            return SuccessOrFailureHelper<ChangePasswordV1InitCommandResult>.Create(
                $"{nameof(ChangePasswordV1InitCommand)} - {err}");

        // Generate new seed and evaluate OPRF for new password
        if (!opaqueServer.TryGenerateSeed(out var newSeed, out err))
            return SuccessOrFailureHelper<ChangePasswordV1InitCommandResult>.Create(
                $"{nameof(ChangePasswordV1InitCommand)} - {err}");

        if (!opaqueServer.TryBlindEvaluate(newSeed, newBlindedBytes, out var newEvaluated, out err))
            return SuccessOrFailureHelper<ChangePasswordV1InitCommandResult>.Create(
                $"{nameof(ChangePasswordV1InitCommand)} - {err}");

        // Generate challenge for client to sign with old Ed25519 private key
        var challenge = opaqueServer.GenerateChallenge();

        var changeId = guidProvider.NewId();
        await transientStateCache.SetAsync($"changepw:{changeId}", new ChangePasswordV1InitState
        {
            UserId = command.UserId,
            OprfSeed = newSeed,
            Challenge = challenge,
            ExistingClientPublicKey = existing.ClientPublicKey
        }, TimeSpan.FromMinutes(5));

        return new SuccessOrFailure<ChangePasswordV1InitCommandResult>(
            new ChangePasswordV1InitCommandResult
            {
                ChangeId = changeId,
                OldEvaluatedElement = Convert.ToBase64String(oldEvaluated),
                Challenge = Convert.ToBase64String(challenge),
                EvaluatedElement = Convert.ToBase64String(newEvaluated)
            });
    }
}
