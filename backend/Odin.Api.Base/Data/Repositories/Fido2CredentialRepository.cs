using Odin.Api.Base.Data;
using SharedLibrary.Basics.Opaque.Domains;
using QuVian.SharedLibrary.Basics.Repositories;

namespace Odin.Api.Base.Data.Repositories;

public class Fido2CredentialRepository(OdinDbContext db)
    : EntityFrameworkCoreRepository<Fido2Credential>(db), IFido2CredentialRepository;
