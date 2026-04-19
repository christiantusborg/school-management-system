using QuVian.SharedLibrary.Basics.Extensions.StringExtensions;

namespace SharedLibrary.Basics.Opaque.Domains;

public static class TeamRoleConstants
{
    public static readonly Guid AdministratorRoleId = "Administrator".AsDeterministicGuid();
    public static readonly Guid MemberRoleId        = "Member".AsDeterministicGuid();
}
