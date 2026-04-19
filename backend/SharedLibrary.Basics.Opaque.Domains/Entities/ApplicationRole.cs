using Microsoft.AspNetCore.Identity;
using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains;

public class ApplicationRole : IdentityRole, IEntity
{
    public Guid TenantId { get; set; } = TenantConstants.DefaultTenantId;
}
