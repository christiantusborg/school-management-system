using QuVian.CaseApi.CaseMembers.V1.RevokeUser.Command;

namespace QuVian.CaseApi.CaseMembers.V1.RevokeUser.Endpoint.Mappers;

public class CaseMembersV1RevokeUserCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<CaseMembersV1RevokeUserCommandResult, CaseMembersV1RevokeUserEndpointResponse>
{
    public CaseMembersV1RevokeUserEndpointResponse MapFrom(CaseMembersV1RevokeUserCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new CaseMembersV1RevokeUserEndpointResponse { CaseUserMemberId = input.CaseUserMemberId, Links = [] };
    }
}
