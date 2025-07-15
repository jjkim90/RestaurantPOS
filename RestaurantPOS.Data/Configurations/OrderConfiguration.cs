using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantPOS.Core.Entities;

namespace RestaurantPOS.Data.Configurations
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.ToTable("Orders");

            builder.HasKey(o => o.OrderId);

            builder.Property(o => o.OrderId)
                .UseIdentityColumn();

            builder.Property(o => o.OrderNumber)
                .IsRequired()
                .HasMaxLength(20);

            builder.HasIndex(o => o.OrderNumber)
                .IsUnique();

            builder.Property(o => o.OrderDate)
                .HasDefaultValueSql("SYSDATETIME()");

            builder.Property(o => o.TotalAmount)
                .HasPrecision(10, 2)
                .HasDefaultValue(0);

            builder.Property(o => o.Status)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("Pending");

            builder.Property(o => o.PaymentMethod)
                .HasMaxLength(20);

            builder.Property(o => o.IsPrinted)
                .HasDefaultValue(false);

            builder.Property(o => o.CreatedAt)
                .HasDefaultValueSql("SYSDATETIME()");

            // Relationships
            builder.HasOne(o => o.Table)
                .WithMany(t => t.Orders)
                .HasForeignKey(o => o.TableId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(o => o.TableId);
            builder.HasIndex(o => o.Status);
            builder.HasIndex(o => o.OrderDate);
            builder.HasIndex(o => new { o.TableId, o.Status });

            // Check constraints
            builder.HasCheckConstraint("CK_Orders_Status", 
                "[Status] IN ('Pending', 'Completed', 'Cancelled')");
            builder.HasCheckConstraint("CK_Orders_PaymentMethod", 
                "[PaymentMethod] IN ('Cash', 'Card') OR [PaymentMethod] IS NULL");
            builder.HasCheckConstraint("CK_Orders_PaymentDate", 
                "[PaymentDate] >= [OrderDate] OR [PaymentDate] IS NULL");
            builder.HasCheckConstraint("CK_Orders_TotalAmount", 
                "[TotalAmount] >= 0");
        }
    }
}