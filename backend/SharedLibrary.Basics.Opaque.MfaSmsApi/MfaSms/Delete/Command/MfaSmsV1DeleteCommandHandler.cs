namespace SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Delete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MfaSmsV1DeleteCommandHandler(IUserTwoFactorMethodRepository repository)
    : ICommandHandler<MfaSmsV1DeleteCommand, MfaSmsV1DeleteCommandResult,
        UserTwoFactorMethod, IUserTwoFactorMethodRepository>
{
    public async Task<SuccessOrFailure<MfaSmsV1DeleteCommandResult>> HandleAsync(
        MfaSmsV1DeleteCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<UserTwoFactorMethod>()
            .AddWhere(x => x.UserId == command.UserId)
            .AddWhere(x => x.MethodType == MfaMethodType.Sms);

        var existing = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);
        if (existing is null)
            return SuccessOrFailureHelper<MfaSmsV1DeleteCommandResult>.EntityNotFound(
                typeof(MfaSmsV1DeleteCommand));

        repository.Remove(existing);


        return new SuccessOrFailure<MfaSmsV1DeleteCommandResult>(
            new MfaSmsV1DeleteCommandResult { UserTwoFactorMethodId = existing.UserTwoFactorMethodId });
    }
}
