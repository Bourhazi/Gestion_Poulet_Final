using Microsoft.EntityFrameworkCore;
using Poulet.Domain;
namespace Poulet.Infrastructure;
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Supplier>(); b.Entity<Client>(); b.Entity<Chamber>(); b.Entity<Purchase>(); b.Entity<Allocation>(); b.Entity<Sale>(); b.Entity<SaleAllocation>(); b.Entity<MondayLine>(); b.Entity<ChickenPiece>(); b.Entity<Feed>(); b.Entity<ProductPurchase>(); b.Entity<ProductSale>(); b.Entity<User>();
        b.Entity<Purchase>().HasMany(p => p.Allocations).WithOne().HasForeignKey(a => a.PurchaseId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Sale>().HasMany(s => s.Allocations).WithOne().HasForeignKey(a => a.SaleId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Sale>().HasMany(s => s.Lines).WithOne().HasForeignKey(l => l.SaleId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<MondayLine>().HasMany(l => l.Pieces).WithOne().HasForeignKey(p => p.MondayLineId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Purchase>().HasOne<Supplier>().WithMany().HasForeignKey(p => p.SupplierId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ProductPurchase>().HasOne<Supplier>().WithMany().HasForeignKey(p => p.SupplierId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Allocation>().HasOne<Chamber>().WithMany().HasForeignKey(a => a.ChamberId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Allocation>().HasOne<Client>().WithMany().HasForeignKey(a => a.ClientId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<SaleAllocation>().HasOne<Chamber>().WithMany().HasForeignKey(a => a.ChamberId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Sale>().HasOne<Client>().WithMany().HasForeignKey(s => s.ClientId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<User>().HasOne<Client>().WithMany().HasForeignKey(u => u.ClientId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Feed>().HasOne<Chamber>().WithMany().HasForeignKey(f => f.ChamberId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<User>().HasIndex(u => u.Username).IsUnique(); b.Entity<Chamber>().HasIndex(c => c.Name).IsUnique(); b.Entity<Sale>().HasIndex(s => s.Date); b.Entity<Purchase>().HasIndex(p => p.Date);
    }
}
