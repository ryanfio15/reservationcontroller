using Domain.Identity;
using Domain.Reservations;
using Domain.Restaurants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

/// <summary>
/// B: Logical data model, "Reservations (insert-only unless noted)".
/// </summary>
internal sealed class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable(
            "Reservation",
            t => t.HasTrigger(InsertOnlyTables.TriggerName("Reservation")));

        builder.HasKey(e => e.ReservationId);
        builder.Property(e => e.ReservationId).ValueGeneratedNever();

        builder.Property(e => e.LocalDate).HasColumnType("date");
        builder.Property(e => e.LocalTime).HasColumnType("time");
        builder.Property(e => e.RequestHash).HasColumnType("binary(32)").IsRequired();

        builder
            .Property(e => e.IdempotencyKey)
            .HasMaxLength(ColumnLengths.IdempotencyKey)
            .IsUnicode(false)
            .IsRequired();

        // A diner booked it, or staff did. Never both, never neither — which is what makes the
        // two filtered idempotency indexes below between them cover every row.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Reservation_OneCreator",
            "([DinerUserId] IS NOT NULL AND [CreatedByMembershipId] IS NULL) "
            + "OR ([DinerUserId] IS NULL AND [CreatedByMembershipId] IS NOT NULL)"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Reservation_PartySize",
            "[PartySize] > 0"));

        // Idempotency is per creator: a retried request finds its own earlier reservation and
        // returns it, rather than booking the table twice. One index per creator kind, each
        // filtered to the rows where that creator is set.
        builder
            .HasIndex(e => new { e.DinerUserId, e.IdempotencyKey })
            .IsUnique()
            .HasFilter("[DinerUserId] IS NOT NULL")
            .HasDatabaseName("UX_Reservation_Diner_Idempotency");

        builder
            .HasIndex(e => new { e.CreatedByMembershipId, e.IdempotencyKey })
            .IsUnique()
            .HasFilter("[CreatedByMembershipId] IS NOT NULL")
            .HasDatabaseName("UX_Reservation_Staff_Idempotency");

        // The host stand's list for a service date, and the admin reservations list.
        builder
            .HasIndex(e => new { e.RestaurantId, e.LocalDate, e.LocalTime })
            .HasDatabaseName("IX_Reservation_Restaurant_Date_Time");

        // A diner's own reservations, newest first — which is also how the five-upcoming cap is
        // counted.
        builder
            .HasIndex(e => new { e.DinerUserId, e.SlotStartUtc })
            .IsDescending(false, true)
            .HasDatabaseName("IX_Reservation_Diner_SlotStart");

        builder
            .HasOne<Restaurant>()
            .WithMany()
            .HasForeignKey(e => e.RestaurantId)
            .OnDelete(DeleteBehavior.NoAction);

        builder
            .HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(e => e.DinerUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder
            .HasOne<StaffMembership>()
            .WithMany()
            .HasForeignKey(e => e.CreatedByMembershipId)
            .OnDelete(DeleteBehavior.NoAction);

        // Slots are closed, never deleted, precisely because this reference exists.
        builder
            .HasOne<SlotInventory>()
            .WithMany()
            .HasForeignKey(e => e.SlotId)
            .OnDelete(DeleteBehavior.NoAction);

        // The rebooking extension point (Production). Self-referencing and never written yet.
        builder
            .HasOne<Reservation>()
            .WithMany()
            .HasForeignKey(e => e.SupersedesReservationId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class ReservationTicketConfiguration : IEntityTypeConfiguration<ReservationTicket>
{
    public void Configure(EntityTypeBuilder<ReservationTicket> builder)
    {
        builder.ToTable(
            "ReservationTicket",
            t => t.HasTrigger(InsertOnlyTables.TriggerName("ReservationTicket")));

        builder.HasKey(e => e.ReservationId);
        builder.Property(e => e.ReservationId).ValueGeneratedNever();

        builder.Property(e => e.ServiceDate).HasColumnType("date");
        builder.Property(e => e.CodeLookupHmac).HasColumnType("binary(32)").IsRequired();
        builder.Property(e => e.CodeAttempt).HasColumnType("tinyint");

        builder
            .Property(e => e.KeyVersion)
            .HasMaxLength(ColumnLengths.KeyVersion)
            .IsUnicode(false)
            .IsRequired();

        // What the host stand's code lookup seeks on, and what makes a 4-digit code unique only
        // within one restaurant and one service date — so the same digits recur harmlessly on
        // another night, and a collision is detected at insert rather than at check-in.
        builder
            .HasIndex(e => new { e.RestaurantId, e.ServiceDate, e.CodeLookupHmac })
            .IsUnique()
            .HasDatabaseName("UX_ReservationTicket_Restaurant_Date_Lookup");

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_ReservationTicket_Window",
            "[RedeemFromUtc] < [RedeemUntilUtc]"));

        builder
            .HasOne<Reservation>()
            .WithMany()
            .HasForeignKey(e => e.ReservationId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class ReservationEventConfiguration : IEntityTypeConfiguration<ReservationEvent>
{
    public void Configure(EntityTypeBuilder<ReservationEvent> builder)
    {
        builder.ToTable(
            "ReservationEvent",
            t => t.HasTrigger(InsertOnlyTables.TriggerName("ReservationEvent")));

        builder.HasKey(e => e.EventId);
        builder.Property(e => e.EventId).UseIdentityColumn();

        builder.Property(e => e.EventType).HasColumnType("tinyint");

        // This is the whole no-double-redemption, no-cancel-after-redeem guarantee: at most one
        // terminal event per reservation, enforced by the database under any concurrency, with
        // no lock and no read-before-write. The redemption and cancellation paths are single
        // conditional inserts that either win this index or lose it.
        builder
            .HasIndex(e => e.ReservationId)
            .IsUnique()
            .HasFilter("[IsTerminal] = 1")
            .HasDatabaseName("UX_ReservationEvent_Terminal");

        // The flag the index filters on must agree with the event type, or the guarantee above
        // could be dodged by inserting a terminal event with IsTerminal = 0. The values match
        // Domain.Reservations.ReservationLifecycle.IsTerminal, and an integration test asserts
        // the two definitions agree.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_ReservationEvent_IsTerminal",
            "([EventType] = 1 AND [IsTerminal] = 0) "
            + "OR ([EventType] IN (2, 3, 4, 5) AND [IsTerminal] = 1)"));

        // The notification extension point reads this log by EventId cursor; this index is how
        // a single reservation's history is read.
        builder
            .HasIndex(e => new { e.ReservationId, e.EventId })
            .HasDatabaseName("IX_ReservationEvent_Reservation_Event");

        builder
            .HasIndex(e => new { e.RestaurantId, e.OccurredAtUtc })
            .HasDatabaseName("IX_ReservationEvent_Restaurant_Occurred");

        builder
            .HasOne<Reservation>()
            .WithMany()
            .HasForeignKey(e => e.ReservationId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class ReservationContactConfiguration : IEntityTypeConfiguration<ReservationContact>
{
    public void Configure(EntityTypeBuilder<ReservationContact> builder)
    {
        // Mutable on purpose: it is the only place a staff-created booking's personal data
        // lives, so it can be corrected and erased. No insert-only trigger here.
        builder.ToTable("ReservationContact");

        builder.HasKey(e => e.ReservationId);
        builder.Property(e => e.ReservationId).ValueGeneratedNever();

        builder.Property(e => e.GuestName).HasMaxLength(ColumnLengths.PersonName).IsRequired();
        builder.Property(e => e.Phone).HasMaxLength(ColumnLengths.Phone);

        builder
            .HasOne<Reservation>()
            .WithMany()
            .HasForeignKey(e => e.ReservationId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class RedemptionAttemptConfiguration : IEntityTypeConfiguration<RedemptionAttempt>
{
    public void Configure(EntityTypeBuilder<RedemptionAttempt> builder)
    {
        builder.ToTable(
            "RedemptionAttempt",
            t => t.HasTrigger(InsertOnlyTables.TriggerName("RedemptionAttempt")));

        builder.HasKey(e => e.AttemptId);
        builder.Property(e => e.AttemptId).UseIdentityColumn();

        builder.Property(e => e.Method).HasColumnType("tinyint");

        // The per-device code-lookup throttle counts these rows in a time range.
        builder
            .HasIndex(e => new { e.DeviceId, e.OccurredAtUtc })
            .HasDatabaseName("IX_RedemptionAttempt_Device_Occurred");

        builder
            .HasOne<Restaurant>()
            .WithMany()
            .HasForeignKey(e => e.RestaurantId)
            .OnDelete(DeleteBehavior.NoAction);

        builder
            .HasOne<HostStandDevice>()
            .WithMany()
            .HasForeignKey(e => e.DeviceId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
