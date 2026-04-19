namespace SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Delete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MfaTotpV1DeleteCommandHandler(IUserTwoFactorMethodRepository repository)
    : ICommandHandler<MfaTotpV1DeleteCommand, MfaTotpV1DeleteCommandResult,
        UserTwoFactorMethod, IUserTwoFactorMethodRepository>
{
    public async Task<SuccessOrFailure<MfaTotpV1DeleteCommandResult>> HandleAsync(
        MfaTotpV1DeleteCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<UserTwoFactorMethod>()
            .AddWhere(x => x.UserId == command.UserId)
            .AddWhere(x => x.MethodType == MfaMethodType.Totp);

        var existing = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);
        if (existing is null)
            return SuccessOrFailureHelper<MfaTotpV1DeleteCommandResult>.EntityNotFound(
                typeof(MfaTotpV1DeleteCommand));

        repository.Remove(existing);


        return new SuccessOrFailure<MfaTotpV1DeleteCommandResult>(
            new MfaTotpV1DeleteCommandResult { UserTwoFactorMethodId = existing.UserTwoFactorMethodId });
    }
}
