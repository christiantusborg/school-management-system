using SharedLibrary.Basics.Opaque.PasswordResetApi.PasswordReset.V1;

namespace SharedLibrary.Basics.Opaque.PasswordResetApi.PasswordReset.V1.Init.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class PasswordResetV1InitCommandHandler(
    UserManager<ApplicationUser> userManager,
    OpaqueServer opaqueServer,
    ITransientStateCache transientStateCache,
    IGuidProvider guidProvider)
    : ICommandHandler<PasswordResetV1InitCommand, PasswordResetV1InitCommandResult,
        ApplicationUser, IApplicationUserRepository>
{
    public async Task<SuccessOrFailure<PasswordResetV1InitCommandResult>> HandleAsync(
        PasswordResetV1InitCommand command, CancellationToken cancellationToken)
    {
        var userId = await transientStateCache.GetAsync<string>($"reset:{command.ResetToken}");
        if (userId is null)
            return SuccessOrFailureHelper<PasswordResetV1InitCommandResult>.Create(
                $"{nameof(PasswordResetV1InitCommand)} - Invalid or expired reset token.");

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return SuccessOrFailureHelper<PasswordResetV1InitCommandResult>.Create(
                $"{nameof(PasswordResetV1InitCommand)} - User not found.");

        if (!opaqueServer.TryConvertToBlindedElement(command.BlindedElement, out var blindedElement, out var err))
            return SuccessOrFailureHelper<PasswordResetV1InitCommandResult>.Create(
                $"{nameof(PasswordResetV1InitCommand)} - {err}");

        if (!opaqueServer.TryGenerateSeed(out var oprfSeed, out err))
            return SuccessOrFailureHelper<PasswordResetV1InitCommandResult>.Create(
                $"{nameof(PasswordResetV1InitCommand)} - {err}");

        if (!opaqueServer.TryBlindEvaluate(oprfSeed, blindedElement, out var evaluatedElement, out err))
            return SuccessOrFailureHelper<PasswordResetV1InitCommandResult>.Create(
                $"{nameof(PasswordResetV1InitCommand)} - {err}");

        var roles = await userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "User";

        var resetId = guidProvider.NewId().ToString("N");
        var state = new PasswordResetInitState
        {
            ExistingUserId = userId,
            ExistingRole = role,
            OprfSeed = oprfSeed,
            ResetToken = command.ResetToken
        };

        await transientStateCache.SetAsync($"resetreg:{resetId}", state, TimeSpan.FromMinutes(5));

        return new SuccessOrFailure<PasswordResetV1InitCommandResult>(new PasswordResetV1InitCommandResult
        {
            ResetId = resetId,
            EvaluatedElement = Convert.ToBase64String(evaluatedElement)
        });
    }
}
