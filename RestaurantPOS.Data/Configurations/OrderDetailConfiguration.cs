using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantPOS.Core.Entities;

namespace RestaurantPOS.Data.Configurations
{
    public class OrderDetailConfiguration : IEntityTypeConfiguration<OrderDetail>
    {
        public void Configure(EntityTypeBuilder<OrderDetail> builder)
        {
            builder.ToTable("OrderDetails");

            builder.HasKey(od => od.OrderDetailId);

            builder.Property(od => od.OrderDetailId)
                .UseIdentityColumn();

            builder.Property(od => od.Quantity);

            builder.Property(od => od.UnitPrice)
                .HasPrecision(10, 2);

            builder.Property(od => od.SubTotal)
                .HasPrecision(10, 2);

            builder.Property(od => od.Note)
                .HasMaxLength(200);

            builder.Property(od => od.CreatedAt)
                .HasDefaultValueSql("SYSDATETIME()");

            // Relationships
            builder.HasOne(od => od.Order)
                .WithMany(o => o.OrderDetails)
                .HasForeignKey(od => od.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(od => od.MenuItem)
                .WithMany(m => m.OrderDetails)
                .HasForeignKey(od => od.MenuItemId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(od => od.OrderId);
            builder.HasIndex(od => od.MenuItemId);

            // Check constraints
            builder.HasCheckConstraint("CK_OrderDetails_Quantity", "[Quantity] > 0");
            builder.HasCheckConstraint("CK_OrderDetails_UnitPrice", "[UnitPrice] > 0");
            builder.HasCheckConstraint("CK_OrderDetails_SubTotal", "[SubTotal] > 0");
        }
    }
}