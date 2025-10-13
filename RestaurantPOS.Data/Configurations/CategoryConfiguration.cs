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

            // Unique index only for non-deleted categories
            builder.HasIndex(c => new { c.CategoryName, c.IsDeleted })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            builder.Property(c => c.DisplayOrder)
                .HasDefaultValue(0);

            builder.Property(c => c.IsActive)
                .HasDefaultValue(true);

            builder.Property(c => c.CreatedAt)
                .HasDefaultValueSql("SYSDATETIME()");

            builder.Property(c => c.IsDeleted)
                .HasDefaultValue(false);

            // Index
            builder.HasIndex(c => new { c.DisplayOrder, c.IsActive });
            builder.HasIndex(c => c.IsDeleted);
            
            // Global query filter to exclude soft deleted records
            builder.HasQueryFilter(c => !c.IsDeleted);
        }
    }
}