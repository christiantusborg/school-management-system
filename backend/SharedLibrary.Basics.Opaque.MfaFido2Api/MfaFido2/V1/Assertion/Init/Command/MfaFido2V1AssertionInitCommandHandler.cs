using Fido2NetLib;
using Fido2NetLib.Objects;
using Odin.Api.Base.Authentication;
using SharedLibrary.Basics.Opaque.Domains.Repositories;
using SharedLibrary.Basics.TransientStateCache;

namespace SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Assertion.Init.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MfaFido2V1AssertionInitCommandHandler(
    IFido2CredentialRepository repository,
    IFido2 fido2,
    ITransientStateCache transientStateCache)
    : ICommandHandler<MfaFido2V1AssertionInitCommand, MfaFido2V1AssertionInitCommandResult,
        Fido2Credential, IFido2CredentialRepository>
{
    public async Task<SuccessOrFailure<MfaFido2V1AssertionInitCommandResult>> HandleAsync(
        MfaFido2V1AssertionInitCommand command, CancellationToken cancellationToken)
    {
        var state = await transientStateCache.GetAsync<MfaPendingState>($"mfa:{command.PendingId}");
        if (state is null)
            return SuccessOrFailureHelper<MfaFido2V1AssertionInitCommandResult>.Create(
                $"{nameof(MfaFido2V1AssertionInitCommand)} - Invalid or expired session.");

        var spec = new Specification<Fido2Credential>()
            .AddWhere(x => x.UserId == state.UserId);

        var credentials = await repository.SearchAsync(spec, cancellationToken).ConfigureAwait(false);
        var descriptors = credentials
            .Select(c => new PublicKeyCredentialDescriptor(c.CredentialId))
            .ToList();

        var options = fido2.GetAssertionOptions(new GetAssertionOptionsParams
        {
            AllowedCredentials = descriptors,
            UserVerification = UserVerificationRequirement.Preferred
        });

        var optionsJson = options.ToJson();
        await transientStateCache.SetAsync($"fido2-assert:{command.PendingId}", optionsJson, TimeSpan.FromMinutes(5));

        return new SuccessOrFailure<MfaFido2V1AssertionInitCommandResult>(
            new MfaFido2V1AssertionInitCommandResult { OptionsJson = optionsJson });
    }
}
