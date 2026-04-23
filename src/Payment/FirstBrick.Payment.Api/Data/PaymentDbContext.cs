using FirstBrick.Payment.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace FirstBrick.Payment.Api.Data;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options) { }

    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Wallet>(b =>
        {
            b.ToTable("Wallets");
            b.HasKey(x => x.UserId);
            b.Property(x => x.Balance).HasColumnType("numeric(18,2)");
        });

        modelBuilder.Entity<Transaction>(b =>
        {
            b.ToTable("Transactions");
            b.HasKey(x => x.Id);
            b.Property(x => x.Amount).HasColumnType("numeric(18,2)");
            b.Property(x => x.Type).HasConversion<string>().HasMaxLength(32);
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
            b.Property(x => x.IdempotencyKey).IsRequired().HasMaxLength(128);
            b.HasIndex(x => x.IdempotencyKey).IsUnique();
            b.HasIndex(x => x.InvestmentRequestId).IsUnique().HasFilter("\"InvestmentRequestId\" IS NOT NULL");
            b.HasIndex(x => new { x.UserId, x.CreatedAtUtc });
        });
    }
}
