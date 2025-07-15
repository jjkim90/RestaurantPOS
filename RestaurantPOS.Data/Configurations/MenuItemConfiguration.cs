using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantPOS.Core.Entities;

namespace RestaurantPOS.Data.Configurations
{
    public class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
    {
        public void Configure(EntityTypeBuilder<MenuItem> builder)
        {
            builder.ToTable("MenuItems");

            builder.HasKey(m => m.MenuItemId);

            builder.Property(m => m.MenuItemId)
                .UseIdentityColumn();

            builder.Property(m => m.ItemName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(m => m.Price)
                .HasPrecision(10, 2);

            builder.Property(m => m.Description)
                .HasMaxLength(500);

            builder.Property(m => m.IsAvailable)
                .HasDefaultValue(true);

            builder.Property(m => m.CreatedAt)
                .HasDefaultValueSql("SYSDATETIME()");

            // Relationships
            builder.HasOne(m => m.Category)
                .WithMany(c => c.MenuItems)
                .HasForeignKey(m => m.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(m => m.CategoryId);
            builder.HasIndex(m => new { m.CategoryId, m.IsAvailable });

            // Check constraints
            builder.HasCheckConstraint("CK_MenuItems_Price", "[Price] > 0");
        }
    }
}