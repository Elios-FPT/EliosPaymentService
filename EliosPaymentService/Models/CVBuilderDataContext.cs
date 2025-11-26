using System.Collections.Generic;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EliosPaymentService.Models;

public class CVBuilderDataContext(DbContextOptions<CVBuilderDataContext> options) : DbContext(options)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.General);

    private static readonly ValueConverter<List<string>?, string?> TransferCategoryConverter =
        new(
            v => v == null ? null : JsonSerializer.Serialize(v, JsonOptions),
            v => string.IsNullOrWhiteSpace(v)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(v, JsonOptions) ?? new List<string>());

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderInvoice> OrderInvoices => Set<OrderInvoice>();
    public DbSet<OrderTransaction> OrderTransactions => Set<OrderTransaction>();
    public DbSet<Transfer> Transfers => Set<Transfer>();
    public DbSet<TransferTransaction> TransferTransactions => Set<TransferTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureOrders(modelBuilder);
        ConfigureTransfers(modelBuilder);
        ConfigureOrderInvoices(modelBuilder);
        ConfigureOrderTransactions(modelBuilder);
        ConfigureTransferTransactions(modelBuilder);
    }

    private static void ConfigureOrders(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(order =>
        {
            order.HasKey(o => o.Id);
            order.Property(o => o.UserId).IsRequired();
            order.Property(o => o.TotalAmount).IsRequired();

            order.OwnsMany(o => o.Items, navigation =>
            {
                navigation.WithOwner().HasForeignKey("OrderId");
                navigation.Property<int>("OrderId");
                navigation.HasKey("OrderId", nameof(OrderItem.Name));
                navigation.Property(i => i.Name).IsRequired();
                navigation.Property(i => i.Quantity).IsRequired();
                navigation.Property(i => i.Price).IsRequired();
                navigation.ToTable("OrderItems");
            });
        });
    }

    private static void ConfigureOrderInvoices(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrderInvoice>(invoice =>
        {
            invoice.HasKey(i => i.Id);
            invoice.HasIndex(i => i.OrderId);
            invoice.HasIndex(i => i.OrderCode);
            invoice.Property(i => i.InvoiceId).IsRequired();
        });
    }

    private static void ConfigureOrderTransactions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrderTransaction>(transaction =>
        {
            transaction.HasKey(t => t.Id);
            transaction.HasIndex(t => t.OrderId);
            transaction.HasIndex(t => t.OrderCode);
            transaction.Property(t => t.PaymentLinkId).IsRequired();
            transaction.Property(t => t.Reference).IsRequired();
        });
    }

    private static void ConfigureTransfers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transfer>(transfer =>
        {
            transfer.HasKey(t => t.Id);
            transfer.Property(t => t.Category)
                .HasConversion(TransferCategoryConverter)
                .HasColumnType("jsonb");

            transfer.HasMany(t => t.Transactions)
                .WithOne()
                .HasForeignKey("TransferId")
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureTransferTransactions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TransferTransaction>(transaction =>
        {
            transaction.HasKey(t => t.Id);
            transaction.Property<string>("TransferId");
            transaction.HasIndex("TransferId");
        });
    }
}

