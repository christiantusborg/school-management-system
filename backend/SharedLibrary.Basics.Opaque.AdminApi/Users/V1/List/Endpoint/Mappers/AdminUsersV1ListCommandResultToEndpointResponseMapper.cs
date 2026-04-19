using SharedLibrary.Basics.Opaque.AdminApi.Users.V1.List.Command;

namespace SharedLibrary.Basics.Opaque.AdminApi.Users.V1.List.Endpoint.Mappers;

public sealed class AdminUsersV1ListCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<CommandSearchResult<AdminUsersV1ListCommandResultItem>, AdminUsersV1ListEndpointResponse>
{
    public AdminUsersV1ListEndpointResponse MapFrom(CommandSearchResult<AdminUsersV1ListCommandResultItem> input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new AdminUsersV1ListEndpointResponse
        {
            Items = input.Items.Select(x => new AdminUsersV1ListEndpointResponseItem
            {
                UserId = x.UserId,
                Username = x.Username,
                Email = x.Email,
                IsEnabled = x.IsEnabled,
                Roles = x.Roles,
                CreatedAt = x.CreatedAt
            }).ToList(),
            Total = input.Total,
            Page = 0,
            PageSize = 0,
            Links = []
        };
    }
}
