using System.Text.Json;
using Fido2NetLib;
using Fido2NetLib.Objects;
using SharedLibrary.Basics.Opaque.Domains.Repositories;
using SharedLibrary.Basics.TransientStateCache;

namespace SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Register.Finish.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MfaFido2V1RegisterFinishCommandHandler(
    IFido2CredentialRepository repository,
    IUserTwoFactorMethodRepository methodRepository,
    IFido2 fido2,
    ITransientStateCache transientStateCache)
    : ICommandHandler<MfaFido2V1RegisterFinishCommand, MfaFido2V1RegisterFinishCommandResult,
        Fido2Credential, IFido2CredentialRepository>
{
    public async Task<SuccessOrFailure<MfaFido2V1RegisterFinishCommandResult>> HandleAsync(
        MfaFido2V1RegisterFinishCommand command, CancellationToken cancellationToken)
    {
        var optionsJson = await transientStateCache.GetAsync<string>($"fido2-reg:{command.UserId}");
        if (optionsJson is null)
            return SuccessOrFailureHelper<MfaFido2V1RegisterFinishCommandResult>.Create(
                $"{nameof(MfaFido2V1RegisterFinishCommand)} - Registration not initiated or expired.");

        CredentialCreateOptions options;
        AuthenticatorAttestationRawResponse attestationResponse;
        try
        {
            options = CredentialCreateOptions.FromJson(optionsJson);
            attestationResponse = JsonSerializer.Deserialize<AuthenticatorAttestationRawResponse>(
                command.AttestationResponse.GetRawText())!;
        }
        catch
        {
            return SuccessOrFailureHelper<MfaFido2V1RegisterFinishCommandResult>.Create(
                $"{nameof(MfaFido2V1RegisterFinishCommand)} - Invalid attestation response.");
        }

        IsCredentialIdUniqueToUserAsyncDelegate callback = async (args, ct) =>
        {
            var spec = new Specification<Fido2Credential>()
                .AddWhere(x => x.CredentialId == args.CredentialId);
            var existing = await repository.GetAsync(spec, ct).ConfigureAwait(false);
            return existing is null;
        };

        RegisteredPublicKeyCredential result;
        try
        {
            result = await fido2.MakeNewCredentialAsync(new MakeNewCredentialParams
            {
                AttestationResponse = attestationResponse,
                OriginalOptions = options,
                IsCredentialIdUniqueToUserCallback = callback
            }, cancellationToken);
        }
        catch
        {
            return SuccessOrFailureHelper<MfaFido2V1RegisterFinishCommandResult>.Create(
                $"{nameof(MfaFido2V1RegisterFinishCommand)} - Attestation verification failed.");
        }

        var transports = result.Transports is not null
            ? JsonSerializer.Serialize(result.Transports)
            : null;

        repository.Add(new Fido2Credential
        {
            UserId = command.UserId,
            CredentialId = result.Id,
            PublicKey = result.PublicKey,
            SignatureCounter = result.SignCount,
            AaGuid = result.AaGuid,
            Transports = transports,
            Label = command.Label
        });

        var methodSpec = new Specification<UserTwoFactorMethod>()
            .AddWhere(x => x.UserId == command.UserId)
            .AddWhere(x => x.MethodType == MfaMethodType.Fido2);

        var existingMethod = await methodRepository.GetAsync(methodSpec, cancellationToken).ConfigureAwait(false);
        if (existingMethod is null)
        {
            methodRepository.Add(new UserTwoFactorMethod
            {
                UserId = command.UserId,
                MethodType = MfaMethodType.Fido2
            });
        }

        await transientStateCache.RemoveAsync($"fido2-reg:{command.UserId}");


        return new SuccessOrFailure<MfaFido2V1RegisterFinishCommandResult>(new MfaFido2V1RegisterFinishCommandResult());
    }
}
