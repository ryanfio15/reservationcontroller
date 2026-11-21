using Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

/// <summary>
/// B: Logical data model, "Identity and tenancy".
/// </summary>
internal sealed class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        builder.ToTable("UserAccount");

        builder.HasKey(e => e.UserId);
        builder.Property(e => e.UserId).ValueGeneratedNever();

        builder.Property(e => e.Email).HasMaxLength(ColumnLengths.Email).IsRequired();
        builder.Property(e => e.DisplayName).HasMaxLength(ColumnLengths.PersonName).IsRequired();
        builder.Property(e => e.Phone).HasMaxLength(ColumnLengths.Phone);

        // An object id is only unique within its tenant, so the key is the pair. This is also
        // what a sign-in looks the account up by.
        builder
            .HasIndex(e => new { e.ExternalTenantId, e.ExternalObjectId })
            .IsUnique()
            .HasDatabaseName("UX_UserAccount_External");
    }
}

internal sealed class StaffMembershipConfiguration : IEntityTypeConfiguration<StaffMembership>
{
    public void Configure(EntityTypeBuilder<StaffMembership> builder)
    {
        // Temporal: who held which role when is exactly the question an audit asks, and the
        // history side answers it without the application writing anything.
        builder.ToTable("StaffMembership", t => t.IsTemporal());

        builder.HasKey(e => e.MembershipId);
        builder.Property(e => e.MembershipId).ValueGeneratedNever();

        builder.Property(e => e.DisplayName).HasMaxLength(ColumnLengths.PersonName).IsRequired();
        builder.Property(e => e.InviteEmail).HasMaxLength(ColumnLengths.Email);
        builder.Property(e => e.Role).HasColumnType("tinyint");
        builder.Property(e => e.Status).HasColumnType("tinyint");
        builder.Property(e => e.RowVersion).IsRowVersion();

        // One membership per person per restaurant — but UserId is null for every code-only
        // host, and several of those can exist, so the uniqueness has to be filtered.
        builder
            .HasIndex(e => new { e.RestaurantId, e.UserId })
            .IsUnique()
            .HasFilter("[UserId] IS NOT NULL")
            .HasDatabaseName("UX_StaffMembership_Restaurant_User");

        builder.HasIndex(e => e.RestaurantId).HasDatabaseName("IX_StaffMembership_Restaurant");

        builder
            .HasOne<Domain.Restaurants.Restaurant>()
            .WithMany()
            .HasForeignKey(e => e.RestaurantId)
            .OnDelete(DeleteBehavior.NoAction);

        builder
            .HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class HostStandDeviceConfiguration : IEntityTypeConfiguration<HostStandDevice>
{
    public void Configure(EntityTypeBuilder<HostStandDevice> builder)
    {
        builder.ToTable("HostStandDevice");

        builder.HasKey(e => e.DeviceId);
        builder.Property(e => e.DeviceId).ValueGeneratedNever();

        builder.Property(e => e.Label).HasMaxLength(ColumnLengths.Label).IsRequired();
        builder.Property(e => e.PairingCodeHash).HasColumnType("binary(32)");
        builder.Property(e => e.CredentialHash).HasColumnType("binary(32)");

        // Both are nullable and both must be unique while set: an unfiltered unique index would
        // allow only one unpaired device per restaurant.
        builder
            .HasIndex(e => e.CredentialHash)
            .IsUnique()
            .HasFilter("[CredentialHash] IS NOT NULL")
            .HasDatabaseName("UX_HostStandDevice_Credential");

        builder
            .HasIndex(e => e.PairingCodeHash)
            .IsUnique()
            .HasFilter("[PairingCodeHash] IS NOT NULL")
            .HasDatabaseName("UX_HostStandDevice_PairingCode");

        builder.HasIndex(e => e.RestaurantId).HasDatabaseName("IX_HostStandDevice_Restaurant");

        builder
            .HasOne<Domain.Restaurants.Restaurant>()
            .WithMany()
            .HasForeignKey(e => e.RestaurantId)
            .OnDelete(DeleteBehavior.NoAction);

        builder
            .HasOne<StaffMembership>()
            .WithMany()
            .HasForeignKey(e => e.CreatedByMembershipId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class HostSignInAttemptConfiguration : IEntityTypeConfiguration<HostSignInAttempt>
{
    public void Configure(EntityTypeBuilder<HostSignInAttempt> builder)
    {
        builder.ToTable(
            "HostSignInAttempt",
            t => t.HasTrigger(InsertOnlyTables.TriggerName("HostSignInAttempt")));

        builder.HasKey(e => e.AttemptId);
        builder.Property(e => e.AttemptId).UseIdentityColumn();

        // The lockout windows are a count of these rows in a time range for one device.
        builder
            .HasIndex(e => new { e.DeviceId, e.OccurredAtUtc })
            .HasDatabaseName("IX_HostSignInAttempt_Device_Occurred");

        builder
            .HasOne<HostStandDevice>()
            .WithMany()
            .HasForeignKey(e => e.DeviceId)
            .OnDelete(DeleteBehavior.NoAction);

        builder
            .HasOne<StaffMembership>()
            .WithMany()
            .HasForeignKey(e => e.MembershipId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class HostAccessCodeConfiguration : IEntityTypeConfiguration<HostAccessCode>
{
    public void Configure(EntityTypeBuilder<HostAccessCode> builder)
    {
        builder.ToTable("HostAccessCode");

        builder.HasKey(e => e.HostCodeId);
        builder.Property(e => e.HostCodeId).ValueGeneratedNever();

        builder.Property(e => e.CodeLookupHmac).HasColumnType("binary(32)").IsRequired();
        builder.Property(e => e.KeyVersion).HasMaxLength(ColumnLengths.KeyVersion).IsUnicode(false).IsRequired();

        // Two hosts at one restaurant may not share a live code. Revoked rows are kept for the
        // audit trail, so the uniqueness is filtered to the live ones.
        builder
            .HasIndex(e => new { e.RestaurantId, e.CodeLookupHmac })
            .IsUnique()
            .HasFilter("[RevokedAtUtc] IS NULL")
            .HasDatabaseName("UX_HostAccessCode_Restaurant_Lookup");

        builder
            .HasIndex(e => e.MembershipId)
            .HasDatabaseName("IX_HostAccessCode_Membership");

        builder
            .HasOne<Domain.Restaurants.Restaurant>()
            .WithMany()
            .HasForeignKey(e => e.RestaurantId)
            .OnDelete(DeleteBehavior.NoAction);

        builder
            .HasOne<StaffMembership>()
            .WithMany()
            .HasForeignKey(e => e.MembershipId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class HostSessionConfiguration : IEntityTypeConfiguration<HostSession>
{
    public void Configure(EntityTypeBuilder<HostSession> builder)
    {
        builder.ToTable("HostSession");

        builder.HasKey(e => e.SessionId);
        builder.Property(e => e.SessionId).ValueGeneratedNever();

        builder.Property(e => e.TokenHash).HasColumnType("binary(32)").IsRequired();

        // The session is looked up by its token hash on every host request.
        builder
            .HasIndex(e => e.TokenHash)
            .IsUnique()
            .HasDatabaseName("UX_HostSession_TokenHash");

        builder
            .HasIndex(e => new { e.DeviceId, e.EndedAtUtc })
            .HasDatabaseName("IX_HostSession_Device_Ended");

        builder
            .HasOne<HostStandDevice>()
            .WithMany()
            .HasForeignKey(e => e.DeviceId)
            .OnDelete(DeleteBehavior.NoAction);

        builder
            .HasOne<StaffMembership>()
            .WithMany()
            .HasForeignKey(e => e.MembershipId)
            .OnDelete(DeleteBehavior.NoAction);

        builder
            .HasOne<Domain.Restaurants.Restaurant>()
            .WithMany()
            .HasForeignKey(e => e.RestaurantId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
