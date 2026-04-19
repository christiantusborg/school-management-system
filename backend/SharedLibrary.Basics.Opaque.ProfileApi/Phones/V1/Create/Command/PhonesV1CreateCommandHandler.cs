using SharedLibrary.Basics.Opaque.Domains.Repositories;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Create.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class PhonesV1CreateCommandHandler(IUserPhoneRepository repository)
    : ICommandHandler<PhonesV1CreateCommand, PhonesV1CreateCommandResult, UserPhone, IUserPhoneRepository>
{
    public async Task<SuccessOrFailure<PhonesV1CreateCommandResult>> HandleAsync(
        PhonesV1CreateCommand command, CancellationToken cancellationToken)
    {
        var countSpec = new Specification<UserPhone>()
            .AddWhere(x => x.UserId == command.UserId);
        var count = await repository.CountAsync(countSpec, cancellationToken).ConfigureAwait(false);
        if (count >= 10)
            return SuccessOrFailureHelper<PhonesV1CreateCommandResult>.Create(
                $"{nameof(PhonesV1CreateCommand)} - Maximum of 10 phone numbers allowed.");

        var entity = new UserPhone
        {
            UserId = command.UserId,
            Number = command.Number,
            Label = command.Label,
            IsPrimary = count == 0
        };
        repository.Add(entity);


        return new SuccessOrFailure<PhonesV1CreateCommandResult>(
            new PhonesV1CreateCommandResult { UserPhoneId = entity.UserPhoneId });
    }
}
