using Domain.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

/// <summary>
/// B: Logical data model, and B: Cross-cutting concerns for what may go in <c>DetailJson</c>.
/// </summary>
internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLog", t => t.HasTrigger(InsertOnlyTables.TriggerName("AuditLog")));

        builder.HasKey(e => e.AuditId);
        builder.Property(e => e.AuditId).UseIdentityColumn();

        builder
            .Property(e => e.Action)
            .HasMaxLength(ColumnLengths.AuditAction)
            .IsUnicode(false)
            .IsRequired();

        builder
            .Property(e => e.TargetType)
            .HasMaxLength(ColumnLengths.AuditTargetType)
            .IsUnicode(false)
            .IsRequired();

        builder
            .Property(e => e.TargetId)
            .HasMaxLength(ColumnLengths.AuditTargetId)
            .IsUnicode(false)
            .IsRequired();

        // Narrow on purpose: ids and enum values only, never a code, token or QR string.
        builder.Property(e => e.DetailJson).HasMaxLength(ColumnLengths.AuditDetailJson);

        builder
            .HasIndex(e => new { e.RestaurantId, e.OccurredAtUtc })
            .HasDatabaseName("IX_AuditLog_Restaurant_Occurred");

        // No foreign keys. An audit row must survive whatever it describes — a revoked device,
        // a deactivated membership, an erased account — and a reference that could fail to
        // insert would mean losing the record of the very action being audited.
    }
}
