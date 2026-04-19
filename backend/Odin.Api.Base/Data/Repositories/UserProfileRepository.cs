using Odin.Api.Base.Data;
using SharedLibrary.Basics.Opaque.Domains;
using QuVian.SharedLibrary.Basics.Repositories;

namespace Odin.Api.Base.Data.Repositories;

public class UserProfileRepository(OdinDbContext db)
    : EntityFrameworkCoreRepository<UserProfile>(db), IUserProfileRepository;
