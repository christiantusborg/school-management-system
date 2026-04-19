using System.Security.Cryptography;
using System.Text;
using Odin.Api.Base.Authentication;
using SharedLibrary.Basics.TransientStateCache;

namespace SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Otp.Verify.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MfaEmailV1VerifyCommandHandler(
    ITransientStateCache transientStateCache,
    SessionTokenService sessionTokenService)
    : ICommandHandler<MfaEmailV1VerifyCommand, MfaEmailV1VerifyCommandResult,
        SessionToken, ISessionTokenRepository>
{
    public async Task<SuccessOrFailure<MfaEmailV1VerifyCommandResult>> HandleAsync(
        MfaEmailV1VerifyCommand command, CancellationToken cancellationToken)
    {
        var state = await transientStateCache.GetAsync<MfaPendingState>($"mfa:{command.PendingId}");
        if (state is null)
            return SuccessOrFailureHelper<MfaEmailV1VerifyCommandResult>.Create(
                $"{nameof(MfaEmailV1VerifyCommand)} - Invalid or expired session.");

        var storedCode = await transientStateCache.GetAsync<string>($"mfa-otp:{command.PendingId}");
        if (storedCode is null || !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(storedCode),
                Encoding.UTF8.GetBytes(command.Code)))
            return SuccessOrFailureHelper<MfaEmailV1VerifyCommandResult>.Create(
                $"{nameof(MfaEmailV1VerifyCommand)} - Invalid code.");

        await transientStateCache.RemoveAsync($"mfa:{command.PendingId}");
        await transientStateCache.RemoveAsync($"mfa-otp:{command.PendingId}");

        var (rawToken, session) = await sessionTokenService.CreateTokenAsync(
            state.UserId, state.DeviceInfo, cancellationToken);

        return new SuccessOrFailure<MfaEmailV1VerifyCommandResult>(
            new MfaEmailV1VerifyCommandResult { Token = rawToken, ExpiresAt = session.ExpiresAt });
    }
}
