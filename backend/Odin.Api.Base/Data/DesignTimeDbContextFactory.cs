using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Odin.Api.Base.Crypto;

namespace Odin.Api.Base.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<OdinDbContext>
{
    public OdinDbContext CreateDbContext(string[] args)
    {
        FieldEncryption.Configure("4258c6316815cda9d2a35dc6733344b8cdefd65154a9e3ce722a99b58cc77e89");
        var optionsBuilder = new DbContextOptionsBuilder<OdinDbContext>();
        optionsBuilder.UseSqlite("Data Source=odin.db");
        return new OdinDbContext(optionsBuilder.Options);
    }
}
