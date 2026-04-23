using FirstBrick.Account.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace FirstBrick.Account.Api.Data;

public class AccountDbContext : DbContext
{
    public AccountDbContext(DbContextOptions<AccountDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(b =>
        {
            b.ToTable("Users");
            b.HasKey(x => x.Id);
            b.Property(x => x.Username).IsRequired().HasMaxLength(64);
            b.HasIndex(x => x.Username).IsUnique();
            b.Property(x => x.PasswordHash).IsRequired();
            b.Property(x => x.FullName).IsRequired().HasMaxLength(128);
            b.Property(x => x.Role).IsRequired().HasMaxLength(32);
        });
    }
}
