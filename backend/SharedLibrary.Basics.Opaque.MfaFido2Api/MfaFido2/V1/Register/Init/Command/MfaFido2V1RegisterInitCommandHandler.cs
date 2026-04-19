using System.Text;
using Fido2NetLib;
using Fido2NetLib.Objects;
using SharedLibrary.Basics.Opaque.Domains.Repositories;
using SharedLibrary.Basics.TransientStateCache;

namespace SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Register.Init.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MfaFido2V1RegisterInitCommandHandler(
    IFido2CredentialRepository repository,
    IFido2 fido2,
    ITransientStateCache transientStateCache)
    : ICommandHandler<MfaFido2V1RegisterInitCommand, MfaFido2V1RegisterInitCommandResult,
        Fido2Credential, IFido2CredentialRepository>
{
    public async Task<SuccessOrFailure<MfaFido2V1RegisterInitCommandResult>> HandleAsync(
        MfaFido2V1RegisterInitCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<Fido2Credential>()
            .AddWhere(x => x.UserId == command.UserId);

        var existing = await repository.SearchAsync(spec, cancellationToken).ConfigureAwait(false);
        var excludeCredentials = existing
            .Select(c => new PublicKeyCredentialDescriptor(c.CredentialId))
            .ToList();

        var fido2User = new Fido2User
        {
            Id = Encoding.UTF8.GetBytes(command.UserId),
            Name = command.Username,
            DisplayName = command.Username
        };

        var options = fido2.RequestNewCredential(new RequestNewCredentialParams
        {
            User = fido2User,
            ExcludeCredentials = excludeCredentials,
            AuthenticatorSelection = new AuthenticatorSelection
            {
                UserVerification = UserVerificationRequirement.Preferred
            }
        });

        var optionsJson = options.ToJson();
        await transientStateCache.SetAsync($"fido2-reg:{command.UserId}", optionsJson, TimeSpan.FromMinutes(5));

        return new SuccessOrFailure<MfaFido2V1RegisterInitCommandResult>(
            new MfaFido2V1RegisterInitCommandResult { OptionsJson = optionsJson });
    }
}
