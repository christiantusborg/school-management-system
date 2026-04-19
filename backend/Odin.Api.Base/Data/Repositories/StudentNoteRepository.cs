using Odin.Api.Base.Data;
using SharedLibrary.Basics.Opaque.Domains;
using QuVian.SharedLibrary.Basics.Repositories;

namespace Odin.Api.Base.Data.Repositories;

public class StudentNoteRepository(OdinDbContext db)
    : EntityFrameworkCoreRepository<StudentNote>(db), IStudentNoteRepository;
