using Odin.Api.Base.Data;
using SharedLibrary.Basics.Opaque.Domains;
using QuVian.SharedLibrary.Basics.Repositories;

namespace Odin.Api.Base.Data.Repositories;

public class ApplicationUserRepository(OdinDbContext db)
    : EntityFrameworkCoreRepository<ApplicationUser>(db), IApplicationUserRepository;
