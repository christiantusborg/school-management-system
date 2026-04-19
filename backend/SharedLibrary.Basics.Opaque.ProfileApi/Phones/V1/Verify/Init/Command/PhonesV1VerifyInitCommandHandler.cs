using System.Security.Cryptography;
using SharedLibrary.Basics.Opaque.Domains.Repositories;
using SharedLibrary.Basics.TransientStateCache;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Verify.Init.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class PhonesV1VerifyInitCommandHandler(
    IUserPhoneRepository repository,
    ITransientStateCache transientStateCache,
    IGuidProvider guidProvider,
    ISmsSender smsSender)
    : ICommandHandler<PhonesV1VerifyInitCommand, PhonesV1VerifyInitCommandResult,
        UserPhone, IUserPhoneRepository>
{
    public async Task<SuccessOrFailure<PhonesV1VerifyInitCommandResult>> HandleAsync(
        PhonesV1VerifyInitCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<UserPhone>()
            .AddWhere(x => x.UserPhoneId == command.UserPhoneId)
            .AddWhere(x => x.UserId == command.UserId);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);
        if (entity is null)
            return SuccessOrFailureHelper<PhonesV1VerifyInitCommandResult>.EntityNotFound(
                typeof(PhonesV1VerifyInitCommand));

        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString("D6");
        var sessionId = guidProvider.NewId();

        await transientStateCache.SetAsync(
            $"phone-verify:{sessionId}",
            new Verify.PhoneVerifyState { UserPhoneId = command.UserPhoneId, UserId = command.UserId, Code = code },
            TimeSpan.FromMinutes(10));

        await smsSender.SendAsync(entity.Number, $"Your verification code: {code}", cancellationToken);

        return new SuccessOrFailure<PhonesV1VerifyInitCommandResult>(
            new PhonesV1VerifyInitCommandResult { SessionId = sessionId });
    }
}
