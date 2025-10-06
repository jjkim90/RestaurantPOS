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

            builder.Property(t => t.TableNumber)
                .IsRequired();

            builder.Property(t => t.Shape)
                .HasMaxLength(20);

            builder.Property(t => t.TableStatus)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(Core.Enums.TableStatus.Available);

            builder.Property(t => t.IsEditable)
                .HasDefaultValue(true);

            builder.Property(t => t.CreatedAt)
                .HasDefaultValueSql("SYSDATETIME()");

            builder.Property(t => t.IsDeleted)
                .HasDefaultValue(false);

            // Relationships
            builder.HasOne(t => t.Space)
                .WithMany(s => s.Tables)
                .HasForeignKey(t => t.SpaceId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(t => t.SpaceId);
            builder.HasIndex(t => t.TableStatus);
            builder.HasIndex(t => new { t.SpaceId, t.TableStatus });

            // Check constraints
            builder.HasCheckConstraint("CK_Tables_Status", 
                "[TableStatus] IN ('Available', 'Occupied', 'PaymentPending', 'Reserved', 'Cleaning')");
            builder.HasCheckConstraint("CK_Tables_Shape", 
                "[Shape] IN ('Circle', 'Rectangle') OR [Shape] IS NULL");
        }
    }
}