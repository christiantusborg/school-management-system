using SharedLibrary.Basics.Opaque.Domains.Repositories;

namespace SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Delete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MfaFido2V1DeleteCommandHandler(
    IFido2CredentialRepository repository,
    IUserTwoFactorMethodRepository methodRepository)
    : ICommandHandler<MfaFido2V1DeleteCommand, MfaFido2V1DeleteCommandResult,
        Fido2Credential, IFido2CredentialRepository>
{
    public async Task<SuccessOrFailure<MfaFido2V1DeleteCommandResult>> HandleAsync(
        MfaFido2V1DeleteCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<Fido2Credential>()
            .AddWhere(x => x.Fido2CredentialId == command.Fido2CredentialId)
            .AddWhere(x => x.UserId == command.UserId);

        var credential = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);
        if (credential is null)
            return SuccessOrFailureHelper<MfaFido2V1DeleteCommandResult>.EntityNotFound(
                typeof(MfaFido2V1DeleteCommand));

        var allSpec = new Specification<Fido2Credential>()
            .AddWhere(x => x.UserId == command.UserId);

        var allCredentials = await repository.SearchAsync(allSpec, cancellationToken).ConfigureAwait(false);
        var isLast = allCredentials.Count() == 1;

        repository.Remove(credential);

        if (isLast)
        {
            var methodSpec = new Specification<UserTwoFactorMethod>()
                .AddWhere(x => x.UserId == command.UserId)
                .AddWhere(x => x.MethodType == MfaMethodType.Fido2);

            var method = await methodRepository.GetAsync(methodSpec, cancellationToken).ConfigureAwait(false);
            if (method is not null)
                methodRepository.Remove(method);
        }


        return new SuccessOrFailure<MfaFido2V1DeleteCommandResult>(
            new MfaFido2V1DeleteCommandResult { Fido2CredentialId = credential.Fido2CredentialId });
    }
}
