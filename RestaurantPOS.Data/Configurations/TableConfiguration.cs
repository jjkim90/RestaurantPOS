using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantPOS.Core.Entities;

namespace RestaurantPOS.Data.Configurations
{
    public class TableConfiguration : IEntityTypeConfiguration<Table>
    {
        public void Configure(EntityTypeBuilder<Table> builder)
        {
            builder.ToTable("Tables");

            builder.HasKey(t => t.TableId);

            builder.Property(t => t.TableId)
                .UseIdentityColumn();

            builder.Property(t => t.TableName)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(t => t.Shape)
                .HasMaxLength(20);

            builder.Property(t => t.Status)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("Available");

            builder.Property(t => t.IsEditable)
                .HasDefaultValue(true);

            builder.Property(t => t.CreatedAt)
                .HasDefaultValueSql("SYSDATETIME()");

            // Relationships
            builder.HasOne(t => t.Space)
                .WithMany(s => s.Tables)
                .HasForeignKey(t => t.SpaceId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(t => t.SpaceId);
            builder.HasIndex(t => t.Status);
            builder.HasIndex(t => new { t.SpaceId, t.Status });

            // Check constraints
            builder.HasCheckConstraint("CK_Tables_Status", 
                "[Status] IN ('Available', 'Occupied', 'PaymentPending')");
            builder.HasCheckConstraint("CK_Tables_Shape", 
                "[Shape] IN ('Circle', 'Rectangle') OR [Shape] IS NULL");
        }
    }
}