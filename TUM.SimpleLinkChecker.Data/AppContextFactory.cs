using Microsoft.EntityFrameworkCore.Design;

namespace TUM.SimpleLinkChecker.Data;

public class AppContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        return AppDbContext.CreateDefaultDbContext();
    }
}