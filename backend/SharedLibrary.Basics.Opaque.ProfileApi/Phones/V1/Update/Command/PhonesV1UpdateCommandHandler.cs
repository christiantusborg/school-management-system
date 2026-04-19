using SharedLibrary.Basics.Opaque.Domains.Repositories;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Update.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class PhonesV1UpdateCommandHandler(IUserPhoneRepository repository)
    : ICommandHandler<PhonesV1UpdateCommand, PhonesV1UpdateCommandResult, UserPhone, IUserPhoneRepository>
{
    public async Task<SuccessOrFailure<PhonesV1UpdateCommandResult>> HandleAsync(
        PhonesV1UpdateCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<UserPhone>()
            .AddWhere(x => x.UserPhoneId == command.UserPhoneId)
            .AddWhere(x => x.UserId == command.UserId);

        var existing = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (existing == null)
            return SuccessOrFailureHelper<PhonesV1UpdateCommandResult>.EntityNotFound(typeof(PhonesV1UpdateCommand));

        existing.Number = command.Number;
        existing.Label = command.Label;

        return new SuccessOrFailure<PhonesV1UpdateCommandResult>(
            new PhonesV1UpdateCommandResult { UserPhoneId = existing.UserPhoneId });
    }
}
