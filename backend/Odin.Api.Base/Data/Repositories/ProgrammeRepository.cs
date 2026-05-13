using Odin.Api.Base.Data;
using SharedLibrary.Basics.Opaque.Domains;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;
using QuVian.SharedLibrary.Basics.Repositories;

namespace Odin.Api.Base.Data.Repositories;

public class ProgrammeRepository(OdinDbContext db)
    : EntityFrameworkCoreRepository<Programme>(db), IProgrammeRepository;
