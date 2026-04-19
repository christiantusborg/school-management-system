using Odin.Api.Base.Data;
using SharedLibrary.Basics.Opaque.Domains;
using QuVian.SharedLibrary.Basics.Repositories;

namespace Odin.Api.Base.Data.Repositories;

public class SessionTokenRepository(OdinDbContext db)
    : EntityFrameworkCoreRepository<SessionToken>(db), ISessionTokenRepository;
