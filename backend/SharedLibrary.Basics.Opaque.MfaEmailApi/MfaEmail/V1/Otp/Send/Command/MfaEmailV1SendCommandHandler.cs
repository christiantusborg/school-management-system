using System.Security.Cryptography;
using Odin.Api.Base.Authentication;
using SharedLibrary.Basics.TransientStateCache;

namespace SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Otp.Send.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MfaEmailV1SendCommandHandler(
    ITransientStateCache transientStateCache,
    IUserContactEmailRepository contactEmailRepository,
    IEmailSender emailSender)
    : ICommandHandler<MfaEmailV1SendCommand, MfaEmailV1SendCommandResult,
        UserContactEmail, IUserContactEmailRepository>
{
    public async Task<SuccessOrFailure<MfaEmailV1SendCommandResult>> HandleAsync(
        MfaEmailV1SendCommand command, CancellationToken cancellationToken)
    {
        var state = await transientStateCache.GetAsync<MfaPendingState>($"mfa:{command.PendingId}");
        if (state is null)
            return SuccessOrFailureHelper<MfaEmailV1SendCommandResult>.Create(
                $"{nameof(MfaEmailV1SendCommand)} - Invalid or expired session.");

        var spec = new Specification<UserContactEmail>()
            .AddWhere(x => x.UserId == state.UserId)
            .AddWhere(x => x.IsPrimary);

        var contactEmail = await contactEmailRepository.GetAsync(spec, cancellationToken).ConfigureAwait(false);
        if (contactEmail is null)
            return SuccessOrFailureHelper<MfaEmailV1SendCommandResult>.Create(
                $"{nameof(MfaEmailV1SendCommand)} - No primary email configured.");

        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString("D6");
        await transientStateCache.SetAsync($"mfa-otp:{command.PendingId}", code, TimeSpan.FromMinutes(5));

        await emailSender.SendAsync(
            contactEmail.Email,
            "Your login verification code",
            $"Your verification code is: {code}",
            cancellationToken);

        return new SuccessOrFailure<MfaEmailV1SendCommandResult>(new MfaEmailV1SendCommandResult());
    }
}
