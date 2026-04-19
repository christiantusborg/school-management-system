using SharedLibrary.Basics.Opaque.Domains.Repositories;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Delete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class PhonesV1DeleteCommandHandler(IUserPhoneRepository repository)
    : ICommandHandler<PhonesV1DeleteCommand, PhonesV1DeleteCommandResult, UserPhone, IUserPhoneRepository>
{
    public async Task<SuccessOrFailure<PhonesV1DeleteCommandResult>> HandleAsync(
        PhonesV1DeleteCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<UserPhone>()
            .AddWhere(x => x.UserPhoneId == command.UserPhoneId)
            .AddWhere(x => x.UserId == command.UserId);

        var existing = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (existing == null)
            return SuccessOrFailureHelper<PhonesV1DeleteCommandResult>.EntityNotFound(typeof(PhonesV1DeleteCommand));

        repository.Remove(existing);


        return new SuccessOrFailure<PhonesV1DeleteCommandResult>(
            new PhonesV1DeleteCommandResult { UserPhoneId = existing.UserPhoneId });
    }
}
