using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Unskip.Infrastructure.Persistence;

public sealed class UnskipDbContextDesignTimeFactory : IDesignTimeDbContextFactory<UnskipDbContext>
{
    public UnskipDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<UnskipDbContext>()
            .UseSqlite("Data Source=unskip.design.db;Foreign Keys=True")
            .Options;
        return new UnskipDbContext(options);
    }
}
