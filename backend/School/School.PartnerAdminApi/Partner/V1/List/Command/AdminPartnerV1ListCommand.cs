namespace School.PartnerAdminApi.Partner.V1.List.Command;

public sealed record AdminPartnerV1ListCommand : IHandleableCommand<
    AdminPartnerV1ListCommand,
    AdminPartnerV1ListCommandValidator,
    AdminPartnerV1ListCommandHandler,
    CommandSearchResult<AdminPartnerV1ListCommandResultItem>>;
