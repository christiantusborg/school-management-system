using SharedLibrary.Basics.Opaque.Domains.Repositories;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.SetPrimary.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class PhonesV1SetPrimaryCommandHandler(IUserPhoneRepository repository)
    : ICommandHandler<PhonesV1SetPrimaryCommand, PhonesV1SetPrimaryCommandResult, UserPhone, IUserPhoneRepository>
{
    public async Task<SuccessOrFailure<PhonesV1SetPrimaryCommandResult>> HandleAsync(
        PhonesV1SetPrimaryCommand command, CancellationToken cancellationToken)
    {
        var targetSpec = new Specification<UserPhone>()
            .AddWhere(x => x.UserPhoneId == command.UserPhoneId)
            .AddWhere(x => x.UserId == command.UserId);

        var target = await repository.GetAsync(targetSpec, cancellationToken).ConfigureAwait(false);

        if (target == null)
            return SuccessOrFailureHelper<PhonesV1SetPrimaryCommandResult>.EntityNotFound(typeof(PhonesV1SetPrimaryCommand));

        var allSpec = new Specification<UserPhone>()
            .AddWhere(x => x.UserId == command.UserId);

        var all = await repository.SearchAsync(allSpec, cancellationToken).ConfigureAwait(false);

        foreach (var phone in all)
            phone.IsPrimary = false;

        target.IsPrimary = true;

        return new SuccessOrFailure<PhonesV1SetPrimaryCommandResult>(
            new PhonesV1SetPrimaryCommandResult { UserPhoneId = target.UserPhoneId });
    }
}
