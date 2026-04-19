namespace SharedLibrary.Basics.Opaque.AdminApi.Users.V1.List.Command;

public sealed class AdminUsersV1ListCommand
    : IHandleableCommand<AdminUsersV1ListCommand, AdminUsersV1ListCommandValidator,
        AdminUsersV1ListCommandHandler, CommandSearchResult<AdminUsersV1ListCommandResultItem>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
}
