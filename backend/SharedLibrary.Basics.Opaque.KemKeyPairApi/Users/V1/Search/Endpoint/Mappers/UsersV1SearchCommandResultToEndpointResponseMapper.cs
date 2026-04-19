using SharedLibrary.Basics.Opaque.KemKeyPairApi.Users.V1.Search.Command;

namespace SharedLibrary.Basics.Opaque.KemKeyPairApi.Users.V1.Search.Endpoint.Mappers;

public sealed class UsersV1SearchCommandResultToEndpointResponseMapper
    : IMapper<UsersV1SearchCommandResult, UsersV1SearchEndpointResponse>
{
    public UsersV1SearchEndpointResponse MapFrom(UsersV1SearchCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new UsersV1SearchEndpointResponse
        {
            Items = input.Items.Select(x => new UsersV1SearchEndpointResponseItem
            {
                UserId = x.UserId,
                Username = x.Username,
                FirstName = x.FirstName,
                LastName = x.LastName,
                Email = x.Email
            }).ToList(),
            Links = []
        };
    }
}
