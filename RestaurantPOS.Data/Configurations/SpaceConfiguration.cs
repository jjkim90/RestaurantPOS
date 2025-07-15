using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantPOS.Core.Entities;

namespace RestaurantPOS.Data.Configurations
{
    public class SpaceConfiguration : IEntityTypeConfiguration<Space>
    {
        public void Configure(EntityTypeBuilder<Space> builder)
        {
            builder.ToTable("Spaces");

            builder.HasKey(s => s.SpaceId);

            builder.Property(s => s.SpaceId)
                .UseIdentityColumn();

            builder.Property(s => s.SpaceName)
                .IsRequired()
                .HasMaxLength(50);

            builder.HasIndex(s => s.SpaceName)
                .IsUnique();

            builder.Property(s => s.IsSystem)
                .HasDefaultValue(false);

            builder.Property(s => s.IsActive)
                .HasDefaultValue(true);

            builder.Property(s => s.CreatedAt)
                .HasDefaultValueSql("SYSDATETIME()");
        }
    }
}