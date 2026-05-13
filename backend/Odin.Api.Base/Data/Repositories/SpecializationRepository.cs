using Odin.Api.Base.Data;
using SharedLibrary.Basics.Opaque.Domains;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;
using QuVian.SharedLibrary.Basics.Repositories;

namespace Odin.Api.Base.Data.Repositories;

public class SpecializationRepository(OdinDbContext db)
    : EntityFrameworkCoreRepository<Specialization>(db), ISpecializationRepository;
