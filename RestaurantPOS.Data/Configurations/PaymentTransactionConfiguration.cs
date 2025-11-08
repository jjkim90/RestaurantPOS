using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantPOS.Core.Entities;

namespace RestaurantPOS.Data.Configurations
{
    public class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
    {
        public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
        {
            builder.ToTable("PaymentTransactions");

            builder.HasKey(p => p.PaymentTransactionId);

            builder.Property(p => p.PaymentMethod)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(p => p.Amount)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(p => p.Status)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("Completed");

            builder.Property(p => p.SyncStatus)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("Synced");

            builder.Property(p => p.PaymentKey)
                .HasMaxLength(200);

            builder.Property(p => p.TransactionId)
                .HasMaxLength(200);

            builder.Property(p => p.CancelReason)
                .HasMaxLength(500);

            builder.Property(p => p.PaymentDate)
                .IsRequired();

            builder.Property(p => p.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            // Order 관계 설정
            builder.HasOne(p => p.Order)
                .WithMany(o => o.PaymentTransactions)
                .HasForeignKey(p => p.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            // Self-referencing 관계 설정 (재결제 시 이전 거래 참조)
            builder.HasOne(p => p.ReferenceTransaction)
                .WithMany()
                .HasForeignKey(p => p.ReferenceTransactionId)
                .OnDelete(DeleteBehavior.Restrict);

            // 인덱스 설정
            builder.HasIndex(p => p.OrderId);
            builder.HasIndex(p => p.PaymentKey);
            builder.HasIndex(p => p.Status);
            builder.HasIndex(p => p.SyncStatus);
            builder.HasIndex(p => new { p.PaymentDate, p.Status });
        }
    }
}