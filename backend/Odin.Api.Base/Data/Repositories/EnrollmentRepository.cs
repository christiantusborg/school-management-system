using Odin.Api.Base.Data;
using SharedLibrary.Basics.Opaque.Domains;
using QuVian.SharedLibrary.Basics.Repositories;

namespace Odin.Api.Base.Data.Repositories;

public class EnrollmentRepository(OdinDbContext db)
    : EntityFrameworkCoreRepository<Enrollment>(db), IEnrollmentRepository;
