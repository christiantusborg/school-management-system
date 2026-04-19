using QuVian.SharedLibrary.Basics.Extensions.StringExtensions;

namespace SharedLibrary.Basics.Opaque.Domains;

public static class TeamTypeConstants
{
    public static readonly Guid SupportId     = "TeamType.Support".AsDeterministicGuid();
    public static readonly Guid DevelopmentId = "TeamType.Development".AsDeterministicGuid();
    public static readonly Guid OperationsId  = "TeamType.Operations".AsDeterministicGuid();
    public static readonly Guid ManagementId  = "TeamType.Management".AsDeterministicGuid();
}
