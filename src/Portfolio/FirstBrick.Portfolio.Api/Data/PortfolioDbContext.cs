using FirstBrick.Portfolio.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace FirstBrick.Portfolio.Api.Data;

public class PortfolioDbContext : DbContext
{
    public PortfolioDbContext(DbContextOptions<PortfolioDbContext> options) : base(options) { }

    public DbSet<PortfolioEntry> PortfolioEntries => Set<PortfolioEntry>();
    public DbSet<ProcessedInvestment> ProcessedInvestments => Set<ProcessedInvestment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PortfolioEntry>(b =>
        {
            b.ToTable("PortfolioView");
            b.HasKey(x => new { x.UserId, x.ProjectId });
            b.Property(x => x.TotalInvested).HasColumnType("numeric(18,2)");
            b.Property(x => x.ProjectTitle).IsRequired().HasMaxLength(256);
            b.HasIndex(x => x.UserId);
        });

        modelBuilder.Entity<ProcessedInvestment>(b =>
        {
            b.ToTable("ProcessedInvestments");
            b.HasKey(x => x.RequestId);
            b.Property(x => x.AppliedAtUtc).HasColumnType("timestamp with time zone");
        });
    }
}
