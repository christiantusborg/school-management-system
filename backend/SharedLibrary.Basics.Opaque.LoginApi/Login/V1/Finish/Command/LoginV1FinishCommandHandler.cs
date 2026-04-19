using Odin.Api.Base.Authentication;
using Odin.Api.Base.Crypto;
using SharedLibrary.Basics.Opaque.Domains;
using SharedLibrary.Basics.TransientStateCache;
using SharedLibrary.Basics.Opaque.LoginApi.Login.V1;

namespace SharedLibrary.Basics.Opaque.LoginApi.Login.V1.Finish.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class LoginV1FinishCommandHandler(
    OpaqueServer opaqueServer,
    ITransientStateCache transientStateCache,
    IOpaqueRecoveryCodeRepository recoveryCodeRepository,
    IUserTwoFactorMethodRepository twoFactorMethodRepository,
    SessionTokenService sessionTokenService,
    IGuidProvider guidProvider)
    : ICommandHandler<LoginV1FinishCommand, LoginV1FinishCommandResult,
        SessionToken, ISessionTokenRepository>
{
    public async Task<SuccessOrFailure<LoginV1FinishCommandResult>> HandleAsync(
        LoginV1FinishCommand command, CancellationToken cancellationToken)
    {
        var cacheKey = $"login:{command.LoginId}";
        var state = await transientStateCache.GetAsync<OpaqueLoginInitState>(cacheKey);
        if (state is null)
            return SuccessOrFailureHelper<LoginV1FinishCommandResult>.Create(
                $"{nameof(LoginV1FinishCommand)} - Invalid credentials.");

        await transientStateCache.RemoveAsync(cacheKey);

        byte[] signature;
        try
        {
            signature = Convert.FromBase64String(command.Signature);
            if (signature.Length != 64)
                return SuccessOrFailureHelper<LoginV1FinishCommandResult>.Create(
                    $"{nameof(LoginV1FinishCommand)} - Invalid credentials.");
        }
        catch
        {
            return SuccessOrFailureHelper<LoginV1FinishCommandResult>.Create(
                $"{nameof(LoginV1FinishCommand)} - Invalid credentials.");
        }

        if (!opaqueServer.VerifySignature(signature, state.Challenge, state.ClientPublicKey))
            return SuccessOrFailureHelper<LoginV1FinishCommandResult>.Create(
                $"{nameof(LoginV1FinishCommand)} - Invalid credentials.");

        if (state.OpaqueRecoveryCodeId.HasValue)
        {
            var recoverySpec = new Specification<OpaqueRecoveryCode>()
                .AddWhere(x => x.OpaqueRecoveryCodeId == state.OpaqueRecoveryCodeId.Value);
            var recoveryCode = await recoveryCodeRepository.GetAsync(recoverySpec, cancellationToken).ConfigureAwait(false);
            if (recoveryCode is not null)
                recoveryCodeRepository.Remove(recoveryCode);
        }
        else
        {
            var methodSpec = new Specification<UserTwoFactorMethod>()
                .AddWhere(x => x.UserId == state.UserId);
            var methods = await twoFactorMethodRepository.SearchAsync(methodSpec, cancellationToken).ConfigureAwait(false);
            var methodList = methods.ToList();

            if (methodList.Count > 0)
            {
                var pendingId = guidProvider.NewId().ToString("N");
                var availableMethods = methodList.Select(m => m.MethodType.ToString().ToLower()).ToArray();
                var pendingState = new MfaPendingState
                {
                    UserId = state.UserId,
                    DeviceInfo = state.DeviceInfo,
                    AvailableMethods = availableMethods
                };
                await transientStateCache.SetAsync($"mfa:{pendingId}", pendingState, TimeSpan.FromMinutes(10));

                return new SuccessOrFailure<LoginV1FinishCommandResult>(new LoginV1FinishCommandResult
                {
                    MfaPendingId = pendingId,
                    AvailableMethods = availableMethods
                });
            }
        }

        var (rawToken, session) = await sessionTokenService.CreateTokenAsync(
            state.UserId, state.DeviceInfo, cancellationToken);

        return new SuccessOrFailure<LoginV1FinishCommandResult>(new LoginV1FinishCommandResult
        {
            Token = rawToken,
            ExpiresAt = session.ExpiresAt
        });
    }
}
