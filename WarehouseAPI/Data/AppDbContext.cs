using Microsoft.EntityFrameworkCore;
using WarehouseAPI.Models;

namespace WarehouseAPI.Data;

public class AppDbContext : DbContext
{
	public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

	public DbSet<Product> Products => Set<Product>();
	public DbSet<Stock> Stocks => Set<Stock>();
	public DbSet<Operation> Operations => Set<Operation>();
	public DbSet<OperationItem> OperationItems => Set<OperationItem>();
	public DbSet<Counterparty> Counterparties => Set<Counterparty>();
	public DbSet<Contact> Contacts => Set<Contact>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<Stock>()
			.HasOne(s => s.Product)
			.WithOne(p => p.Stock)
			.HasForeignKey<Stock>(s => s.ProductId);

		modelBuilder.Entity<OperationItem>()
			.HasOne(oi => oi.Operation)
			.WithMany(o => o.Items)
			.HasForeignKey(oi => oi.OperationId);

		modelBuilder.Entity<Contact>()
			.HasOne(c => c.Counterparty)
			.WithMany(cp => cp.Contacts)
			.HasForeignKey(c => c.CounterpartyId);
	}
}