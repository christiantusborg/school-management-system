using SharedLibrary.Basics.Opaque.Domains.Repositories;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Get.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class PhonesV1GetCommandHandler(IUserPhoneRepository repository)
    : ICommandHandler<PhonesV1GetCommand, PhonesV1GetCommandResult, UserPhone, IUserPhoneRepository>
{
    public async Task<SuccessOrFailure<PhonesV1GetCommandResult>> HandleAsync(
        PhonesV1GetCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<UserPhone>()
            .AddWhere(x => x.UserPhoneId == command.UserPhoneId)
            .AddWhere(x => x.UserId == command.UserId);

        var existing = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (existing == null)
            return SuccessOrFailureHelper<PhonesV1GetCommandResult>.EntityNotFound(typeof(PhonesV1GetCommand));

        return new SuccessOrFailure<PhonesV1GetCommandResult>(new PhonesV1GetCommandResult
        {
            UserPhoneId = existing.UserPhoneId,
            Number = existing.Number,
            Label = existing.Label,
            IsPrimary = existing.IsPrimary
        });
    }
}
