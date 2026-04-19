using System.Security.Cryptography;
using SharedLibrary.Basics.TransientStateCache;

namespace SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Enable.Init.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MfaEmailV1EnableInitCommandHandler(
    IUserContactEmailRepository contactEmailRepository,
    ITransientStateCache transientStateCache,
    IGuidProvider guidProvider,
    IEmailSender emailSender)
    : ICommandHandler<MfaEmailV1EnableInitCommand, MfaEmailV1EnableInitCommandResult,
        UserContactEmail, IUserContactEmailRepository>
{
    public async Task<SuccessOrFailure<MfaEmailV1EnableInitCommandResult>> HandleAsync(
        MfaEmailV1EnableInitCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<UserContactEmail>()
            .AddWhere(x => x.UserId == command.UserId)
            .AddWhere(x => x.IsPrimary);

        var contactEmail = await contactEmailRepository.GetAsync(spec, cancellationToken).ConfigureAwait(false);
        if (contactEmail is null)
            return SuccessOrFailureHelper<MfaEmailV1EnableInitCommandResult>.Create(
                $"{nameof(MfaEmailV1EnableInitCommand)} - No primary email configured.");

        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString("D6");
        var sessionId = guidProvider.NewId();

        await transientStateCache.SetAsync(
            $"mfa-email-enable:{sessionId}",
            new MfaEmailEnableInitState { UserId = command.UserId, Code = code },
            TimeSpan.FromMinutes(10));

        await emailSender.SendAsync(
            contactEmail.Email,
            "Enable Email MFA",
            $"Your verification code is: {code}",
            cancellationToken);

        return new SuccessOrFailure<MfaEmailV1EnableInitCommandResult>(
            new MfaEmailV1EnableInitCommandResult { SessionId = sessionId });
    }
}
