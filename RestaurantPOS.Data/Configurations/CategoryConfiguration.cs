using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantPOS.Core.Entities;

namespace RestaurantPOS.Data.Configurations
{
    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.ToTable("Categories");

            builder.HasKey(c => c.CategoryId);

            builder.Property(c => c.CategoryId)
                .UseIdentityColumn();

            builder.Property(c => c.CategoryName)
                .IsRequired()
                .HasMaxLength(50);

            builder.HasIndex(c => c.CategoryName)
                .IsUnique();

            builder.Property(c => c.DisplayOrder)
                .HasDefaultValue(0);

            builder.Property(c => c.IsActive)
                .HasDefaultValue(true);

            builder.Property(c => c.CreatedAt)
                .HasDefaultValueSql("SYSDATETIME()");

            // Index
            builder.HasIndex(c => new { c.DisplayOrder, c.IsActive });
        }
    }
}