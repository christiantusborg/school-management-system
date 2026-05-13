using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Odin.Api.Base.Crypto;

namespace Odin.Api.Base.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<OdinDbContext>
{
    public OdinDbContext CreateDbContext(string[] args)
    {
        FieldEncryption.Configure("4258c6316815cda9d2a35dc6733344b8cdefd65154a9e3ce722a99b58cc77e89");
        // Must match Program.cs — otherwise `dotnet ef migrations add` generates
        // columns with `timestamp with time zone` and runtime treats `DateTime`
        // as `timestamp`, which triggers PendingModelChangesWarning on boot.
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        var optionsBuilder = new DbContextOptionsBuilder<OdinDbContext>();
        // Design-time factory is used by `dotnet ef migrations add/update`. It does not
        // open a real connection during `migrations add`, so any valid-shaped Npgsql
        // connection string works. Runtime Program.cs reads the real conn string from
        // configuration (appsettings.json / ConnectionStrings__DefaultConnection env).
        optionsBuilder.UseNpgsql("Host=localhost;Database=school_management_db;Username=postgres;Password=postgres");
        return new OdinDbContext(optionsBuilder.Options);
    }
}
