namespace SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Delete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MfaEmailV1DeleteCommandHandler(IUserTwoFactorMethodRepository repository)
    : ICommandHandler<MfaEmailV1DeleteCommand, MfaEmailV1DeleteCommandResult,
        UserTwoFactorMethod, IUserTwoFactorMethodRepository>
{
    public async Task<SuccessOrFailure<MfaEmailV1DeleteCommandResult>> HandleAsync(
        MfaEmailV1DeleteCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<UserTwoFactorMethod>()
            .AddWhere(x => x.UserId == command.UserId)
            .AddWhere(x => x.MethodType == MfaMethodType.Email);

        var existing = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);
        if (existing is null)
            return SuccessOrFailureHelper<MfaEmailV1DeleteCommandResult>.EntityNotFound(
                typeof(MfaEmailV1DeleteCommand));

        repository.Remove(existing);


        return new SuccessOrFailure<MfaEmailV1DeleteCommandResult>(
            new MfaEmailV1DeleteCommandResult { UserTwoFactorMethodId = existing.UserTwoFactorMethodId });
    }
}
