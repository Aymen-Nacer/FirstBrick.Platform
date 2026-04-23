using FirstBrick.Investment.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace FirstBrick.Investment.Api.Data;

public class InvestmentDbContext : DbContext
{
    public InvestmentDbContext(DbContextOptions<InvestmentDbContext> options) : base(options) { }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<InvestmentRequest> InvestmentRequests => Set<InvestmentRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>(b =>
        {
            b.ToTable("Projects", t => t.HasCheckConstraint(
                "CK_Projects_CurrentLeTarget",
                "\"CurrentAmount\" <= \"TargetAmount\""));
            b.HasKey(x => x.Id);
            b.Property(x => x.Title).IsRequired().HasMaxLength(256);
            b.Property(x => x.TargetAmount).HasColumnType("numeric(18,2)");
            b.Property(x => x.CurrentAmount).HasColumnType("numeric(18,2)");
            b.HasIndex(x => x.OwnerId);
        });

        modelBuilder.Entity<InvestmentRequest>(b =>
        {
            b.ToTable("InvestmentRequests");
            b.HasKey(x => x.Id);
            b.Property(x => x.Amount).HasColumnType("numeric(18,2)");
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            b.HasIndex(x => new { x.UserId, x.ProjectId });
            b.HasIndex(x => x.ProjectId);
        });
    }
}
