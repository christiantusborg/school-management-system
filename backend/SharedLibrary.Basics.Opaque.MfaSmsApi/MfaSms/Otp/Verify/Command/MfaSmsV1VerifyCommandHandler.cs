using System.Security.Cryptography;
using System.Text;
using Odin.Api.Base.Authentication;
using SharedLibrary.Basics.TransientStateCache;

namespace SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Otp.Verify.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MfaSmsV1VerifyCommandHandler(
    ITransientStateCache transientStateCache,
    SessionTokenService sessionTokenService)
    : ICommandHandler<MfaSmsV1VerifyCommand, MfaSmsV1VerifyCommandResult,
        SessionToken, ISessionTokenRepository>
{
    public async Task<SuccessOrFailure<MfaSmsV1VerifyCommandResult>> HandleAsync(
        MfaSmsV1VerifyCommand command, CancellationToken cancellationToken)
    {
        var state = await transientStateCache.GetAsync<MfaPendingState>($"mfa:{command.PendingId}");
        if (state is null)
            return SuccessOrFailureHelper<MfaSmsV1VerifyCommandResult>.Create(
                $"{nameof(MfaSmsV1VerifyCommand)} - Invalid or expired session.");

        var storedCode = await transientStateCache.GetAsync<string>($"mfa-otp:{command.PendingId}");
        if (storedCode is null || !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(storedCode),
                Encoding.UTF8.GetBytes(command.Code)))
            return SuccessOrFailureHelper<MfaSmsV1VerifyCommandResult>.Create(
                $"{nameof(MfaSmsV1VerifyCommand)} - Invalid code.");

        await transientStateCache.RemoveAsync($"mfa:{command.PendingId}");
        await transientStateCache.RemoveAsync($"mfa-otp:{command.PendingId}");

        var (rawToken, session) = await sessionTokenService.CreateTokenAsync(
            state.UserId, state.DeviceInfo, cancellationToken);

        return new SuccessOrFailure<MfaSmsV1VerifyCommandResult>(
            new MfaSmsV1VerifyCommandResult { Token = rawToken, ExpiresAt = session.ExpiresAt });
    }
}
