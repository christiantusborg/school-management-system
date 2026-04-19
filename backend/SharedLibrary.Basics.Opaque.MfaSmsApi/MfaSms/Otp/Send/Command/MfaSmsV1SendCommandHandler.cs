using System.Security.Cryptography;
using Odin.Api.Base.Authentication;
using SharedLibrary.Basics.TransientStateCache;

namespace SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Otp.Send.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MfaSmsV1SendCommandHandler(
    ITransientStateCache transientStateCache,
    IUserPhoneRepository phoneRepository,
    ISmsSender smsSender)
    : ICommandHandler<MfaSmsV1SendCommand, MfaSmsV1SendCommandResult,
        UserPhone, IUserPhoneRepository>
{
    public async Task<SuccessOrFailure<MfaSmsV1SendCommandResult>> HandleAsync(
        MfaSmsV1SendCommand command, CancellationToken cancellationToken)
    {
        var state = await transientStateCache.GetAsync<MfaPendingState>($"mfa:{command.PendingId}");
        if (state is null)
            return SuccessOrFailureHelper<MfaSmsV1SendCommandResult>.Create(
                $"{nameof(MfaSmsV1SendCommand)} - Invalid or expired session.");

        var spec = new Specification<UserPhone>()
            .AddWhere(x => x.UserId == state.UserId)
            .AddWhere(x => x.IsPrimary);

        var phone = await phoneRepository.GetAsync(spec, cancellationToken).ConfigureAwait(false);
        if (phone is null)
            return SuccessOrFailureHelper<MfaSmsV1SendCommandResult>.Create(
                $"{nameof(MfaSmsV1SendCommand)} - No primary phone configured.");

        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString("D6");
        await transientStateCache.SetAsync($"mfa-otp:{command.PendingId}", code, TimeSpan.FromMinutes(5));

        await smsSender.SendAsync(
            phone.Number,
            $"Your verification code is: {code}",
            cancellationToken);

        return new SuccessOrFailure<MfaSmsV1SendCommandResult>(new MfaSmsV1SendCommandResult());
    }
}
