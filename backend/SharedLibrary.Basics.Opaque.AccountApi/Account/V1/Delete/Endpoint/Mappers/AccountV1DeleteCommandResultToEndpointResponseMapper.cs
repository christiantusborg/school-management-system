using SharedLibrary.Basics.Opaque.AccountApi.Account.V1.Delete.Command;

namespace SharedLibrary.Basics.Opaque.AccountApi.Account.V1.Delete.Endpoint.Mappers;

public sealed class AccountV1DeleteCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<AccountV1DeleteCommandResult, AccountV1DeleteEndpointResponse>
{
    public AccountV1DeleteEndpointResponse MapFrom(AccountV1DeleteCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new AccountV1DeleteEndpointResponse
        {
            Links = []
        };
    }
}
