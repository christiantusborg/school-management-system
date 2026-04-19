using QuVian.CaseApi.CaseMembers.V1.GrantUser.Command;

namespace QuVian.CaseApi.CaseMembers.V1.GrantUser.Endpoint.Mappers;

public class CaseMembersV1GrantUserCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<CaseMembersV1GrantUserCommandResult, CaseMembersV1GrantUserEndpointResponse>
{
    public CaseMembersV1GrantUserEndpointResponse MapFrom(CaseMembersV1GrantUserCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new CaseMembersV1GrantUserEndpointResponse { CaseUserMemberId = input.CaseUserMemberId, Links = [] };
    }
}
