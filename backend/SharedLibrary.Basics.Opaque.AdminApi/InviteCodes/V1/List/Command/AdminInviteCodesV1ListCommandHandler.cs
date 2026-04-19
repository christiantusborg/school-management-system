namespace SharedLibrary.Basics.Opaque.AdminApi.InviteCodes.V1.List.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class AdminInviteCodesV1ListCommandHandler(IInviteCodeRepository repository)
    : ICommandHandler<AdminInviteCodesV1ListCommand, CommandSearchResult<AdminInviteCodesV1ListCommandResultItem>,
        InviteCode, IInviteCodeRepository>
{
    public async Task<SuccessOrFailure<CommandSearchResult<AdminInviteCodesV1ListCommandResultItem>>> HandleAsync(
        AdminInviteCodesV1ListCommand command, CancellationToken cancellationToken)
    {
        var all = await repository.SearchAsync(new Specification<InviteCode>(), cancellationToken).ConfigureAwait(false);

        var items = all
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new AdminInviteCodesV1ListCommandResultItem
            {
                InviteCodeId = x.InviteCodeId,
                Code = x.Code,
                AssignedRole = x.AssignedRole,
                ExpiresAt = x.ExpiresAt,
                RedeemedByUserId = x.RedeemedByUserId,
                CreatedAt = x.CreatedAt
            })
            .ToList();

        return new SuccessOrFailure<CommandSearchResult<AdminInviteCodesV1ListCommandResultItem>>(
            new CommandSearchResult<AdminInviteCodesV1ListCommandResultItem>
            {
                Items = items,
                Total = items.Count
            });
    }
}
