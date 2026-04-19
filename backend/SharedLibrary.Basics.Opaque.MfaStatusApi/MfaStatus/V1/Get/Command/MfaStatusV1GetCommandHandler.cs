namespace SharedLibrary.Basics.Opaque.MfaStatusApi.MfaStatus.V1.Get.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MfaStatusV1GetCommandHandler(
    IUserTwoFactorMethodRepository twoFactorMethodRepository,
    IFido2CredentialRepository fido2CredentialRepository)
    : ICommandHandler<MfaStatusV1GetCommand, MfaStatusV1GetCommandResult, UserTwoFactorMethod, IUserTwoFactorMethodRepository>
{
    public async Task<SuccessOrFailure<MfaStatusV1GetCommandResult>> HandleAsync(
        MfaStatusV1GetCommand command, CancellationToken cancellationToken)
    {
        var methodSpec = new Specification<UserTwoFactorMethod>()
            .AddWhere(x => x.UserId == command.UserId);

        var methods = await twoFactorMethodRepository.SearchAsync(methodSpec, cancellationToken).ConfigureAwait(false);

        var credentialSpec = new Specification<Fido2Credential>()
            .AddWhere(x => x.UserId == command.UserId);

        var credentials = await fido2CredentialRepository.SearchAsync(credentialSpec, cancellationToken).ConfigureAwait(false);

        return new SuccessOrFailure<MfaStatusV1GetCommandResult>(new MfaStatusV1GetCommandResult
        {
            EnabledMethods = methods
                .Select(m => new MfaMethodInfoResult { Method = m.MethodType.ToString().ToLower() })
                .ToArray(),
            Fido2Credentials = credentials
                .Select(c => new Fido2CredentialInfoResult
                {
                    Fido2CredentialId = c.Fido2CredentialId,
                    Label = c.Label,
                    CreatedAt = c.CreatedAt,
                    LastUsedAt = c.LastUsedAt
                })
                .ToArray()
        });
    }
}
