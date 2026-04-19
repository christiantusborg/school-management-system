using Odin.Api.Base.Data;
using SharedLibrary.Basics.Opaque.Domains;
using QuVian.SharedLibrary.Basics.Repositories;

namespace Odin.Api.Base.Data.Repositories;

public class PathwayDocumentRequirementRepository(OdinDbContext db)
    : EntityFrameworkCoreRepository<PathwayDocumentRequirement>(db), IPathwayDocumentRequirementRepository;
