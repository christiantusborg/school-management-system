using System.Text;
using System.Text.Json;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Odin.Api.Base.Authentication;
using SharedLibrary.Basics.Opaque.Domains.Repositories;
using SharedLibrary.Basics.TransientStateCache;

namespace SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Assertion.Finish.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MfaFido2V1AssertionFinishCommandHandler(
    IFido2CredentialRepository repository,
    IFido2 fido2,
    ITransientStateCache transientStateCache,
    SessionTokenService sessionTokenService)
    : ICommandHandler<MfaFido2V1AssertionFinishCommand, MfaFido2V1AssertionFinishCommandResult,
        Fido2Credential, IFido2CredentialRepository>
{
    public async Task<SuccessOrFailure<MfaFido2V1AssertionFinishCommandResult>> HandleAsync(
        MfaFido2V1AssertionFinishCommand command, CancellationToken cancellationToken)
    {
        var state = await transientStateCache.GetAsync<MfaPendingState>($"mfa:{command.PendingId}");
        if (state is null)
            return SuccessOrFailureHelper<MfaFido2V1AssertionFinishCommandResult>.Create(
                $"{nameof(MfaFido2V1AssertionFinishCommand)} - Invalid or expired session.");

        var optionsJson = await transientStateCache.GetAsync<string>($"fido2-assert:{command.PendingId}");
        if (optionsJson is null)
            return SuccessOrFailureHelper<MfaFido2V1AssertionFinishCommandResult>.Create(
                $"{nameof(MfaFido2V1AssertionFinishCommand)} - Assertion not initialized.");

        await transientStateCache.RemoveAsync($"mfa:{command.PendingId}");
        await transientStateCache.RemoveAsync($"fido2-assert:{command.PendingId}");

        AssertionOptions options;
        AuthenticatorAssertionRawResponse assertionResponse;
        try
        {
            options = AssertionOptions.FromJson(optionsJson);
            assertionResponse = JsonSerializer.Deserialize<AuthenticatorAssertionRawResponse>(
                command.AssertionResponse.GetRawText())!;
        }
        catch
        {
            return SuccessOrFailureHelper<MfaFido2V1AssertionFinishCommandResult>.Create(
                $"{nameof(MfaFido2V1AssertionFinishCommand)} - Invalid assertion response.");
        }

        var credentialId = assertionResponse.RawId;
        var spec = new Specification<Fido2Credential>()
            .AddWhere(x => x.CredentialId == credentialId)
            .AddWhere(x => x.UserId == state.UserId);

        var credential = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);
        if (credential is null)
            return SuccessOrFailureHelper<MfaFido2V1AssertionFinishCommandResult>.EntityNotFound(
                typeof(MfaFido2V1AssertionFinishCommand));

        var userIdBytes = Encoding.UTF8.GetBytes(state.UserId);
        IsUserHandleOwnerOfCredentialIdAsync callback = (args, _) =>
            Task.FromResult(args.UserHandle.SequenceEqual(userIdBytes));

        VerifyAssertionResult result;
        try
        {
            result = await fido2.MakeAssertionAsync(new MakeAssertionParams
            {
                AssertionResponse = assertionResponse,
                OriginalOptions = options,
                StoredPublicKey = credential.PublicKey,
                StoredSignatureCounter = credential.SignatureCounter,
                IsUserHandleOwnerOfCredentialIdCallback = callback
            }, cancellationToken);
        }
        catch
        {
            return SuccessOrFailureHelper<MfaFido2V1AssertionFinishCommandResult>.Create(
                $"{nameof(MfaFido2V1AssertionFinishCommand)} - Assertion verification failed.");
        }

        credential.SignatureCounter = result.SignCount;
        credential.LastUsedAt = DateTime.UtcNow;

        var (rawToken, session) = await sessionTokenService.CreateTokenAsync(
            state.UserId, state.DeviceInfo, cancellationToken);

        return new SuccessOrFailure<MfaFido2V1AssertionFinishCommandResult>(
            new MfaFido2V1AssertionFinishCommandResult { Token = rawToken, ExpiresAt = session.ExpiresAt });
    }
}
