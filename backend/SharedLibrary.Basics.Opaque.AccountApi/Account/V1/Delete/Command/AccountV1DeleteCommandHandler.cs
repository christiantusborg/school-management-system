namespace SharedLibrary.Basics.Opaque.AccountApi.Account.V1.Delete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class AccountV1DeleteCommandHandler(
    UserManager<ApplicationUser> userManager,
    SessionTokenService sessionTokenService)
    : ICommandHandler<AccountV1DeleteCommand, AccountV1DeleteCommandResult, ApplicationUser, IApplicationUserRepository>
{
    public async Task<SuccessOrFailure<AccountV1DeleteCommandResult>> HandleAsync(
        AccountV1DeleteCommand command, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(command.UserId);
        if (user is null)
            return SuccessOrFailureHelper<AccountV1DeleteCommandResult>.EntityNotFound(typeof(AccountV1DeleteCommand));

        await sessionTokenService.RevokeAllUserTokensAsync(command.UserId, cancellationToken);

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
            return SuccessOrFailureHelper<AccountV1DeleteCommandResult>.Create(
                string.Join("; ", result.Errors.Select(e => e.Description)));

        return new SuccessOrFailure<AccountV1DeleteCommandResult>(new AccountV1DeleteCommandResult());
    }
}
