using Odin.Api.Base.Data;
using SharedLibrary.Basics.Opaque.Domains;
using SharedLibrary.Basics.Opaque.Domains.Partners;
using QuVian.SharedLibrary.Basics.Repositories;

namespace Odin.Api.Base.Data.Repositories;

public class PartnerRepository(OdinDbContext db)
    : EntityFrameworkCoreRepository<Partner>(db), IPartnerRepository;
