using Domain.Restaurants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

/// <summary>
/// B: Logical data model, "Identity and tenancy" and "Hours and availability".
/// </summary>
internal sealed class RestaurantConfiguration : IEntityTypeConfiguration<Restaurant>
{
    public void Configure(EntityTypeBuilder<Restaurant> builder)
    {
        builder.ToTable("Restaurant", t => t.IsTemporal());

        builder.HasKey(e => e.RestaurantId);
        builder.Property(e => e.RestaurantId).ValueGeneratedNever();

        builder.Property(e => e.Name).HasMaxLength(ColumnLengths.RestaurantName).IsRequired();
        builder.Property(e => e.AddressLine1).HasMaxLength(ColumnLengths.AddressLine).IsRequired();
        builder.Property(e => e.AddressLine2).HasMaxLength(ColumnLengths.AddressLine);
        builder.Property(e => e.City).HasMaxLength(ColumnLengths.City).IsRequired();
        builder.Property(e => e.Region).HasMaxLength(ColumnLengths.Region).IsRequired();
        builder.Property(e => e.PostalCode).HasMaxLength(ColumnLengths.PostalCode).IsRequired();
        builder.Property(e => e.Country).HasMaxLength(ColumnLengths.Country).IsRequired();

        // ±90 and ±180 to six decimal places, which is about 0.1 m — far finer than a
        // restaurant's front door needs.
        builder.Property(e => e.Latitude).HasColumnType("decimal(8,6)");
        builder.Property(e => e.Longitude).HasColumnType("decimal(9,6)");

        builder
            .Property(e => e.TimeZoneId)
            .HasMaxLength(ColumnLengths.TimeZoneId)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(e => e.HostLockThreshold).HasDefaultValue(5);
        builder.Property(e => e.HostSuspendThreshold).HasDefaultValue(10);
        builder.Property(e => e.RowVersion).IsRowVersion();

        // Diner search is a name prefix within a city, over listed restaurants only.
        builder
            .HasIndex(e => new { e.IsListed, e.City, e.Name })
            .HasDatabaseName("IX_Restaurant_Listed_City_Name");

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Restaurant_Thresholds",
            "[HostLockThreshold] > 0 AND [HostSuspendThreshold] >= [HostLockThreshold]"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Restaurant_Horizon",
            "[BookingHorizonDays] > 0 AND [GraceMinutes] >= 0"));
    }
}

internal sealed class RestaurantCuisineConfiguration : IEntityTypeConfiguration<RestaurantCuisine>
{
    public void Configure(EntityTypeBuilder<RestaurantCuisine> builder)
    {
        builder.ToTable("RestaurantCuisine");

        builder.HasKey(e => new { e.RestaurantId, e.Cuisine });

        builder
            .Property(e => e.Cuisine)
            .HasMaxLength(ColumnLengths.Cuisine)
            .IsUnicode(false)
            .IsRequired();

        // The reverse order of the key, for the Cut-first "restaurants serving X" filter.
        builder
            .HasIndex(e => new { e.Cuisine, e.RestaurantId })
            .HasDatabaseName("IX_RestaurantCuisine_Cuisine_Restaurant");

        builder
            .HasOne<Restaurant>()
            .WithMany()
            .HasForeignKey(e => e.RestaurantId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class OperatingHoursConfiguration : IEntityTypeConfiguration<OperatingHours>
{
    public void Configure(EntityTypeBuilder<OperatingHours> builder)
    {
        builder.ToTable("OperatingHours", t => t.IsTemporal());

        builder.HasKey(e => e.OperatingHoursId);
        builder.Property(e => e.OperatingHoursId).ValueGeneratedNever();

        builder.Property(e => e.DayOfWeek).HasColumnType("tinyint");
        builder.Property(e => e.OpenTime).HasColumnType("time");
        builder.Property(e => e.CloseTime).HasColumnType("time");

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_OperatingHours_DayOfWeek",
            "[DayOfWeek] BETWEEN 0 AND 6"));

        // "Hours may not cross midnight in the prototype (OpenTime < CloseTime is enforced)"
        // — B: Core domain logic. Enforced here as well as in validation, because the slot
        // generator's arithmetic assumes it.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_OperatingHours_NotAcrossMidnight",
            "[OpenTime] < [CloseTime]"));

        builder
            .HasIndex(e => new { e.RestaurantId, e.DayOfWeek })
            .HasDatabaseName("IX_OperatingHours_Restaurant_Day");

        builder
            .HasOne<Restaurant>()
            .WithMany()
            .HasForeignKey(e => e.RestaurantId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class SpecialDayConfiguration : IEntityTypeConfiguration<SpecialDay>
{
    public void Configure(EntityTypeBuilder<SpecialDay> builder)
    {
        builder.ToTable("SpecialDay");

        // One override per restaurant-day, so the weekly pattern is replaced rather than merged.
        builder.HasKey(e => new { e.RestaurantId, e.LocalDate });

        builder.Property(e => e.LocalDate).HasColumnType("date");
        builder.Property(e => e.OpenTime).HasColumnType("time");
        builder.Property(e => e.CloseTime).HasColumnType("time");

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_SpecialDay_NotAcrossMidnight",
            "[OpenTime] IS NULL OR [CloseTime] IS NULL OR [OpenTime] < [CloseTime]"));

        // A closed day has no hours; an open override needs both ends.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_SpecialDay_HoursMatchClosedFlag",
            "([IsClosed] = 1 AND [OpenTime] IS NULL AND [CloseTime] IS NULL) "
            + "OR ([IsClosed] = 0 AND [OpenTime] IS NOT NULL AND [CloseTime] IS NOT NULL)"));

        builder
            .HasOne<Restaurant>()
            .WithMany()
            .HasForeignKey(e => e.RestaurantId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class AvailabilityRuleConfiguration : IEntityTypeConfiguration<AvailabilityRule>
{
    public void Configure(EntityTypeBuilder<AvailabilityRule> builder)
    {
        builder.ToTable("AvailabilityRule", t => t.IsTemporal());

        builder.HasKey(e => e.RuleId);
        builder.Property(e => e.RuleId).ValueGeneratedNever();

        builder.Property(e => e.DayOfWeek).HasColumnType("tinyint");
        builder.Property(e => e.StartTime).HasColumnType("time");
        builder.Property(e => e.EndTime).HasColumnType("time");
        builder.Property(e => e.RowVersion).IsRowVersion();

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_AvailabilityRule_PartyBand",
            "[PartyMin] > 0 AND [PartyMin] <= [PartyMax]"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_AvailabilityRule_Capacity",
            "[CapacityPerSlot] >= 0"));

        // Same invariants as OperatingHours, for the same reason: B states the day-of-week range
        // and the no-midnight rule for hours, and the slot generator applies both to rules too.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_AvailabilityRule_DayOfWeek",
            "[DayOfWeek] BETWEEN 0 AND 6"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_AvailabilityRule_Window",
            "[StartTime] < [EndTime] AND [SlotMinutes] > 0"));

        builder
            .HasIndex(e => new { e.RestaurantId, e.DayOfWeek })
            .HasDatabaseName("IX_AvailabilityRule_Restaurant_Day");

        builder
            .HasOne<Restaurant>()
            .WithMany()
            .HasForeignKey(e => e.RestaurantId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

internal sealed class SlotInventoryConfiguration : IEntityTypeConfiguration<SlotInventory>
{
    public void Configure(EntityTypeBuilder<SlotInventory> builder)
    {
        builder.ToTable("SlotInventory");

        builder.HasKey(e => e.SlotId);
        builder.Property(e => e.SlotId).UseIdentityColumn();

        builder.Property(e => e.LocalDate).HasColumnType("date");
        builder.Property(e => e.LocalTime).HasColumnType("time");

        // The last line of defence against overselling. Even if every layer above were wrong,
        // the database refuses the row — which is also why capacity is shrunk with a set-based
        // CASE statement rather than read-then-write (B: Core domain logic).
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_SlotInventory_Booked",
            "[Booked] >= 0 AND [Booked] <= [Capacity]"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_SlotInventory_PartyBand",
            "[PartyMin] > 0 AND [PartyMin] <= [PartyMax]"));

        // The generator's upsert key. Two rules for the same day with overlapping times may not
        // have overlapping party bands, because two such rules sharing a PartyMin would collide
        // here.
        builder
            .HasIndex(e => new { e.RestaurantId, e.SlotStartUtc, e.PartyMin })
            .IsUnique()
            .HasDatabaseName("UX_SlotInventory_Restaurant_Start_PartyMin");

        // The availability read: one indexed seek, covered, with no time-zone arithmetic in SQL.
        builder
            .HasIndex(e => new { e.RestaurantId, e.LocalDate, e.PartyMin, e.PartyMax })
            .IncludeProperties(e => new { e.LocalTime, e.Capacity, e.Booked, e.IsOpen })
            .HasDatabaseName("IX_SlotInventory_Restaurant_Date_Band");

        builder
            .HasOne<Restaurant>()
            .WithMany()
            .HasForeignKey(e => e.RestaurantId)
            .OnDelete(DeleteBehavior.NoAction);

        builder
            .HasOne<AvailabilityRule>()
            .WithMany()
            .HasForeignKey(e => e.RuleId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
