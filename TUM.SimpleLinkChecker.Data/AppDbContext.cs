using Microsoft.EntityFrameworkCore;

namespace TUM.SimpleLinkChecker.Data;

public class AppDbContext : DbContext
{
    public DbSet<Scrape> Scrapes { get; set; } = null!;
    public DbSet<Snapshot> Snapshots { get; set; } = null!;
    public DbSet<WebRef> WebRefs { get; set; } = null!;


    public static AppDbContext CreateDefaultDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=tum;Username=tum;Password=tum");
        return new AppDbContext(options.Options);
    }
    
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WebRef>()
            .HasOne(wr => wr.Source)
            .WithMany(s => s.OutgoingLinks)
            .HasForeignKey(wr => wr.SourceId);
        modelBuilder.Entity<WebRef>()
            .HasOne(webRef => webRef.Target)
            .WithMany(snapshot => snapshot.IncomingLinks)
            .HasForeignKey(webRef => webRef.TargetId);

        modelBuilder.Entity<Snapshot>()
            .HasOne(s => s.Scrape)
            .WithMany(scrape => scrape.Snapshots)
            .HasForeignKey(snapshot => snapshot.ScrapeId);
    }
}