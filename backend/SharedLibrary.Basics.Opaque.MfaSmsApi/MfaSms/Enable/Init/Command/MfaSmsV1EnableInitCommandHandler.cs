using System.Security.Cryptography;
using SharedLibrary.Basics.TransientStateCache;

namespace SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Enable.Init.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MfaSmsV1EnableInitCommandHandler(
    IUserPhoneRepository phoneRepository,
    ITransientStateCache transientStateCache,
    IGuidProvider guidProvider,
    ISmsSender smsSender)
    : ICommandHandler<MfaSmsV1EnableInitCommand, MfaSmsV1EnableInitCommandResult,
        UserPhone, IUserPhoneRepository>
{
    public async Task<SuccessOrFailure<MfaSmsV1EnableInitCommandResult>> HandleAsync(
        MfaSmsV1EnableInitCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<UserPhone>()
            .AddWhere(x => x.UserId == command.UserId)
            .AddWhere(x => x.IsPrimary);

        var phone = await phoneRepository.GetAsync(spec, cancellationToken).ConfigureAwait(false);
        if (phone is null)
            return SuccessOrFailureHelper<MfaSmsV1EnableInitCommandResult>.Create(
                $"{nameof(MfaSmsV1EnableInitCommand)} - No primary phone configured.");

        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString("D6");
        var sessionId = guidProvider.NewId();

        await transientStateCache.SetAsync(
            $"mfa-sms-enable:{sessionId}",
            new MfaSmsEnableInitState { UserId = command.UserId, Code = code },
            TimeSpan.FromMinutes(10));

        await smsSender.SendAsync(
            phone.Number,
            $"Your verification code is: {code}",
            cancellationToken);

        return new SuccessOrFailure<MfaSmsV1EnableInitCommandResult>(
            new MfaSmsV1EnableInitCommandResult { SessionId = sessionId });
    }
}
