namespace StockReplenishment.App.DataAccess;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<StockLocation> Locations => Set<StockLocation>();
    public DbSet<ReplenishmentRequest> Requests => Set<ReplenishmentRequest>();
    public DbSet<RequestLineItem> LineItems => Set<RequestLineItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ReplenishmentRequest>()
            .HasMany(r => r.Lines)
            .WithOne()
            .HasForeignKey(l => l.RequestId);
    }
}
