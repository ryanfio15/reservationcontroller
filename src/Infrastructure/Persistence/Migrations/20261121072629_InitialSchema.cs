using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLog",
                columns: table => new
                {
                    AuditId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RestaurantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActorMembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Action = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    TargetType = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    TargetId = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    DetailJson = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLog", x => x.AuditId);
                });

            migrationBuilder.CreateTable(
                name: "Restaurant",
                columns: table => new
                {
                    RestaurantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AddressLine1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AddressLine2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Region = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(8,6)", nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", nullable: true),
                    TimeZoneId = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    GraceMinutes = table.Column<int>(type: "int", nullable: false),
                    BookingHorizonDays = table.Column<int>(type: "int", nullable: false),
                    HostLockThreshold = table.Column<int>(type: "int", nullable: false, defaultValue: 5),
                    HostSuspendThreshold = table.Column<int>(type: "int", nullable: false, defaultValue: 10),
                    IsListed = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Restaurant", x => x.RestaurantId);
                    table.CheckConstraint("CK_Restaurant_Horizon", "[BookingHorizonDays] > 0 AND [GraceMinutes] >= 0");
                    table.CheckConstraint("CK_Restaurant_Thresholds", "[HostLockThreshold] > 0 AND [HostSuspendThreshold] >= [HostLockThreshold]");
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "RestaurantHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "UserAccount",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalObjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalTenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SessionsValidFromUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAccount", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "AvailabilityRule",
                columns: table => new
                {
                    RuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RestaurantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayOfWeek = table.Column<byte>(type: "tinyint", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    SlotMinutes = table.Column<int>(type: "int", nullable: false),
                    PartyMin = table.Column<int>(type: "int", nullable: false),
                    PartyMax = table.Column<int>(type: "int", nullable: false),
                    CapacityPerSlot = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvailabilityRule", x => x.RuleId);
                    table.CheckConstraint("CK_AvailabilityRule_Capacity", "[CapacityPerSlot] >= 0");
                    table.CheckConstraint("CK_AvailabilityRule_DayOfWeek", "[DayOfWeek] BETWEEN 0 AND 6");
                    table.CheckConstraint("CK_AvailabilityRule_PartyBand", "[PartyMin] > 0 AND [PartyMin] <= [PartyMax]");
                    table.CheckConstraint("CK_AvailabilityRule_Window", "[StartTime] < [EndTime] AND [SlotMinutes] > 0");
                    table.ForeignKey(
                        name: "FK_AvailabilityRule_Restaurant_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurant",
                        principalColumn: "RestaurantId");
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "AvailabilityRuleHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "OperatingHours",
                columns: table => new
                {
                    OperatingHoursId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RestaurantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayOfWeek = table.Column<byte>(type: "tinyint", nullable: false),
                    OpenTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    CloseTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperatingHours", x => x.OperatingHoursId);
                    table.CheckConstraint("CK_OperatingHours_DayOfWeek", "[DayOfWeek] BETWEEN 0 AND 6");
                    table.CheckConstraint("CK_OperatingHours_NotAcrossMidnight", "[OpenTime] < [CloseTime]");
                    table.ForeignKey(
                        name: "FK_OperatingHours_Restaurant_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurant",
                        principalColumn: "RestaurantId");
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "OperatingHoursHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "RestaurantCuisine",
                columns: table => new
                {
                    RestaurantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Cuisine = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantCuisine", x => new { x.RestaurantId, x.Cuisine });
                    table.ForeignKey(
                        name: "FK_RestaurantCuisine_Restaurant_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurant",
                        principalColumn: "RestaurantId");
                });

            migrationBuilder.CreateTable(
                name: "SpecialDay",
                columns: table => new
                {
                    RestaurantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false),
                    OpenTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    CloseTime = table.Column<TimeOnly>(type: "time", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpecialDay", x => new { x.RestaurantId, x.LocalDate });
                    table.CheckConstraint("CK_SpecialDay_HoursMatchClosedFlag", "([IsClosed] = 1 AND [OpenTime] IS NULL AND [CloseTime] IS NULL) OR ([IsClosed] = 0 AND [OpenTime] IS NOT NULL AND [CloseTime] IS NOT NULL)");
                    table.CheckConstraint("CK_SpecialDay_NotAcrossMidnight", "[OpenTime] IS NULL OR [CloseTime] IS NULL OR [OpenTime] < [CloseTime]");
                    table.ForeignKey(
                        name: "FK_SpecialDay_Restaurant_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurant",
                        principalColumn: "RestaurantId");
                });

            migrationBuilder.CreateTable(
                name: "StaffMembership",
                columns: table => new
                {
                    MembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RestaurantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    InviteEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Role = table.Column<byte>(type: "tinyint", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffMembership", x => x.MembershipId);
                    table.ForeignKey(
                        name: "FK_StaffMembership_Restaurant_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurant",
                        principalColumn: "RestaurantId");
                    table.ForeignKey(
                        name: "FK_StaffMembership_UserAccount_UserId",
                        column: x => x.UserId,
                        principalTable: "UserAccount",
                        principalColumn: "UserId");
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "StaffMembershipHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "SlotInventory",
                columns: table => new
                {
                    SlotId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RestaurantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SlotStartUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LocalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    LocalTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    PartyMin = table.Column<int>(type: "int", nullable: false),
                    PartyMax = table.Column<int>(type: "int", nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    Booked = table.Column<int>(type: "int", nullable: false),
                    IsOpen = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlotInventory", x => x.SlotId);
                    table.CheckConstraint("CK_SlotInventory_Booked", "[Booked] >= 0 AND [Booked] <= [Capacity]");
                    table.CheckConstraint("CK_SlotInventory_PartyBand", "[PartyMin] > 0 AND [PartyMin] <= [PartyMax]");
                    table.ForeignKey(
                        name: "FK_SlotInventory_AvailabilityRule_RuleId",
                        column: x => x.RuleId,
                        principalTable: "AvailabilityRule",
                        principalColumn: "RuleId");
                    table.ForeignKey(
                        name: "FK_SlotInventory_Restaurant_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurant",
                        principalColumn: "RestaurantId");
                });

            migrationBuilder.CreateTable(
                name: "HostAccessCode",
                columns: table => new
                {
                    HostCodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RestaurantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodeLookupHmac = table.Column<byte[]>(type: "binary(32)", nullable: false),
                    KeyVersion = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HostAccessCode", x => x.HostCodeId);
                    table.ForeignKey(
                        name: "FK_HostAccessCode_Restaurant_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurant",
                        principalColumn: "RestaurantId");
                    table.ForeignKey(
                        name: "FK_HostAccessCode_StaffMembership_MembershipId",
                        column: x => x.MembershipId,
                        principalTable: "StaffMembership",
                        principalColumn: "MembershipId");
                });

            migrationBuilder.CreateTable(
                name: "HostStandDevice",
                columns: table => new
                {
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RestaurantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PairingCodeHash = table.Column<byte[]>(type: "binary(32)", nullable: true),
                    PairingExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CredentialHash = table.Column<byte[]>(type: "binary(32)", nullable: true),
                    CreatedByMembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PairedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LockedUntilUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SuspendedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LockoutResetAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HostStandDevice", x => x.DeviceId);
                    table.ForeignKey(
                        name: "FK_HostStandDevice_Restaurant_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurant",
                        principalColumn: "RestaurantId");
                    table.ForeignKey(
                        name: "FK_HostStandDevice_StaffMembership_CreatedByMembershipId",
                        column: x => x.CreatedByMembershipId,
                        principalTable: "StaffMembership",
                        principalColumn: "MembershipId");
                });

            migrationBuilder.CreateTable(
                name: "Reservation",
                columns: table => new
                {
                    ReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RestaurantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DinerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedByMembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SlotId = table.Column<long>(type: "bigint", nullable: false),
                    SlotStartUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LocalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    LocalTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    PartySize = table.Column<int>(type: "int", nullable: false),
                    SupersedesReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    RequestHash = table.Column<byte[]>(type: "binary(32)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservation", x => x.ReservationId);
                    table.CheckConstraint("CK_Reservation_OneCreator", "([DinerUserId] IS NOT NULL AND [CreatedByMembershipId] IS NULL) OR ([DinerUserId] IS NULL AND [CreatedByMembershipId] IS NOT NULL)");
                    table.CheckConstraint("CK_Reservation_PartySize", "[PartySize] > 0");
                    table.ForeignKey(
                        name: "FK_Reservation_Reservation_SupersedesReservationId",
                        column: x => x.SupersedesReservationId,
                        principalTable: "Reservation",
                        principalColumn: "ReservationId");
                    table.ForeignKey(
                        name: "FK_Reservation_Restaurant_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurant",
                        principalColumn: "RestaurantId");
                    table.ForeignKey(
                        name: "FK_Reservation_SlotInventory_SlotId",
                        column: x => x.SlotId,
                        principalTable: "SlotInventory",
                        principalColumn: "SlotId");
                    table.ForeignKey(
                        name: "FK_Reservation_StaffMembership_CreatedByMembershipId",
                        column: x => x.CreatedByMembershipId,
                        principalTable: "StaffMembership",
                        principalColumn: "MembershipId");
                    table.ForeignKey(
                        name: "FK_Reservation_UserAccount_DinerUserId",
                        column: x => x.DinerUserId,
                        principalTable: "UserAccount",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "HostSession",
                columns: table => new
                {
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<byte[]>(type: "binary(32)", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RestaurantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSeenAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HostSession", x => x.SessionId);
                    table.ForeignKey(
                        name: "FK_HostSession_HostStandDevice_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "HostStandDevice",
                        principalColumn: "DeviceId");
                    table.ForeignKey(
                        name: "FK_HostSession_Restaurant_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurant",
                        principalColumn: "RestaurantId");
                    table.ForeignKey(
                        name: "FK_HostSession_StaffMembership_MembershipId",
                        column: x => x.MembershipId,
                        principalTable: "StaffMembership",
                        principalColumn: "MembershipId");
                });

            migrationBuilder.CreateTable(
                name: "HostSignInAttempt",
                columns: table => new
                {
                    AttemptId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Succeeded = table.Column<bool>(type: "bit", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HostSignInAttempt", x => x.AttemptId);
                    table.ForeignKey(
                        name: "FK_HostSignInAttempt_HostStandDevice_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "HostStandDevice",
                        principalColumn: "DeviceId");
                    table.ForeignKey(
                        name: "FK_HostSignInAttempt_StaffMembership_MembershipId",
                        column: x => x.MembershipId,
                        principalTable: "StaffMembership",
                        principalColumn: "MembershipId");
                });

            migrationBuilder.CreateTable(
                name: "RedemptionAttempt",
                columns: table => new
                {
                    AttemptId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RestaurantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Method = table.Column<byte>(type: "tinyint", nullable: false),
                    Succeeded = table.Column<bool>(type: "bit", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RedemptionAttempt", x => x.AttemptId);
                    table.ForeignKey(
                        name: "FK_RedemptionAttempt_HostStandDevice_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "HostStandDevice",
                        principalColumn: "DeviceId");
                    table.ForeignKey(
                        name: "FK_RedemptionAttempt_Restaurant_RestaurantId",
                        column: x => x.RestaurantId,
                        principalTable: "Restaurant",
                        principalColumn: "RestaurantId");
                });

            migrationBuilder.CreateTable(
                name: "ReservationContact",
                columns: table => new
                {
                    ReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RestaurantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GuestName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationContact", x => x.ReservationId);
                    table.ForeignKey(
                        name: "FK_ReservationContact_Reservation_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "Reservation",
                        principalColumn: "ReservationId");
                });

            migrationBuilder.CreateTable(
                name: "ReservationEvent",
                columns: table => new
                {
                    EventId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RestaurantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<byte>(type: "tinyint", nullable: false),
                    IsTerminal = table.Column<bool>(type: "bit", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActorMembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationEvent", x => x.EventId);
                    table.CheckConstraint("CK_ReservationEvent_IsTerminal", "([EventType] = 1 AND [IsTerminal] = 0) OR ([EventType] IN (2, 3, 4, 5) AND [IsTerminal] = 1)");
                    table.ForeignKey(
                        name: "FK_ReservationEvent_Reservation_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "Reservation",
                        principalColumn: "ReservationId");
                });

            migrationBuilder.CreateTable(
                name: "ReservationTicket",
                columns: table => new
                {
                    ReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RestaurantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CodeLookupHmac = table.Column<byte[]>(type: "binary(32)", nullable: false),
                    CodeAttempt = table.Column<byte>(type: "tinyint", nullable: false),
                    KeyVersion = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    RedeemFromUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RedeemUntilUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationTicket", x => x.ReservationId);
                    table.CheckConstraint("CK_ReservationTicket_Window", "[RedeemFromUtc] < [RedeemUntilUtc]");
                    table.ForeignKey(
                        name: "FK_ReservationTicket_Reservation_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "Reservation",
                        principalColumn: "ReservationId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_Restaurant_Occurred",
                table: "AuditLog",
                columns: new[] { "RestaurantId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilityRule_Restaurant_Day",
                table: "AvailabilityRule",
                columns: new[] { "RestaurantId", "DayOfWeek" });

            migrationBuilder.CreateIndex(
                name: "IX_HostAccessCode_Membership",
                table: "HostAccessCode",
                column: "MembershipId");

            migrationBuilder.CreateIndex(
                name: "UX_HostAccessCode_Restaurant_Lookup",
                table: "HostAccessCode",
                columns: new[] { "RestaurantId", "CodeLookupHmac" },
                unique: true,
                filter: "[RevokedAtUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_HostSession_Device_Ended",
                table: "HostSession",
                columns: new[] { "DeviceId", "EndedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_HostSession_MembershipId",
                table: "HostSession",
                column: "MembershipId");

            migrationBuilder.CreateIndex(
                name: "IX_HostSession_RestaurantId",
                table: "HostSession",
                column: "RestaurantId");

            migrationBuilder.CreateIndex(
                name: "UX_HostSession_TokenHash",
                table: "HostSession",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HostSignInAttempt_Device_Occurred",
                table: "HostSignInAttempt",
                columns: new[] { "DeviceId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_HostSignInAttempt_MembershipId",
                table: "HostSignInAttempt",
                column: "MembershipId");

            migrationBuilder.CreateIndex(
                name: "IX_HostStandDevice_CreatedByMembershipId",
                table: "HostStandDevice",
                column: "CreatedByMembershipId");

            migrationBuilder.CreateIndex(
                name: "IX_HostStandDevice_Restaurant",
                table: "HostStandDevice",
                column: "RestaurantId");

            migrationBuilder.CreateIndex(
                name: "UX_HostStandDevice_Credential",
                table: "HostStandDevice",
                column: "CredentialHash",
                unique: true,
                filter: "[CredentialHash] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_HostStandDevice_PairingCode",
                table: "HostStandDevice",
                column: "PairingCodeHash",
                unique: true,
                filter: "[PairingCodeHash] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OperatingHours_Restaurant_Day",
                table: "OperatingHours",
                columns: new[] { "RestaurantId", "DayOfWeek" });

            migrationBuilder.CreateIndex(
                name: "IX_RedemptionAttempt_Device_Occurred",
                table: "RedemptionAttempt",
                columns: new[] { "DeviceId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RedemptionAttempt_RestaurantId",
                table: "RedemptionAttempt",
                column: "RestaurantId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_Diner_SlotStart",
                table: "Reservation",
                columns: new[] { "DinerUserId", "SlotStartUtc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_Restaurant_Date_Time",
                table: "Reservation",
                columns: new[] { "RestaurantId", "LocalDate", "LocalTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_SlotId",
                table: "Reservation",
                column: "SlotId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_SupersedesReservationId",
                table: "Reservation",
                column: "SupersedesReservationId");

            migrationBuilder.CreateIndex(
                name: "UX_Reservation_Diner_Idempotency",
                table: "Reservation",
                columns: new[] { "DinerUserId", "IdempotencyKey" },
                unique: true,
                filter: "[DinerUserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_Reservation_Staff_Idempotency",
                table: "Reservation",
                columns: new[] { "CreatedByMembershipId", "IdempotencyKey" },
                unique: true,
                filter: "[CreatedByMembershipId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationEvent_Reservation_Event",
                table: "ReservationEvent",
                columns: new[] { "ReservationId", "EventId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReservationEvent_Restaurant_Occurred",
                table: "ReservationEvent",
                columns: new[] { "RestaurantId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_ReservationEvent_Terminal",
                table: "ReservationEvent",
                column: "ReservationId",
                unique: true,
                filter: "[IsTerminal] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_ReservationTicket_Restaurant_Date_Lookup",
                table: "ReservationTicket",
                columns: new[] { "RestaurantId", "ServiceDate", "CodeLookupHmac" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Restaurant_Listed_City_Name",
                table: "Restaurant",
                columns: new[] { "IsListed", "City", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantCuisine_Cuisine_Restaurant",
                table: "RestaurantCuisine",
                columns: new[] { "Cuisine", "RestaurantId" });

            migrationBuilder.CreateIndex(
                name: "IX_SlotInventory_Restaurant_Date_Band",
                table: "SlotInventory",
                columns: new[] { "RestaurantId", "LocalDate", "PartyMin", "PartyMax" })
                .Annotation("SqlServer:Include", new[] { "LocalTime", "Capacity", "Booked", "IsOpen" });

            migrationBuilder.CreateIndex(
                name: "IX_SlotInventory_RuleId",
                table: "SlotInventory",
                column: "RuleId");

            migrationBuilder.CreateIndex(
                name: "UX_SlotInventory_Restaurant_Start_PartyMin",
                table: "SlotInventory",
                columns: new[] { "RestaurantId", "SlotStartUtc", "PartyMin" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StaffMembership_Restaurant",
                table: "StaffMembership",
                column: "RestaurantId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffMembership_UserId",
                table: "StaffMembership",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UX_StaffMembership_Restaurant_User",
                table: "StaffMembership",
                columns: new[] { "RestaurantId", "UserId" },
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_UserAccount_External",
                table: "UserAccount",
                columns: new[] { "ExternalTenantId", "ExternalObjectId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLog");

            migrationBuilder.DropTable(
                name: "HostAccessCode");

            migrationBuilder.DropTable(
                name: "HostSession");

            migrationBuilder.DropTable(
                name: "HostSignInAttempt");

            migrationBuilder.DropTable(
                name: "OperatingHours")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "OperatingHoursHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "RedemptionAttempt");

            migrationBuilder.DropTable(
                name: "ReservationContact");

            migrationBuilder.DropTable(
                name: "ReservationEvent");

            migrationBuilder.DropTable(
                name: "ReservationTicket");

            migrationBuilder.DropTable(
                name: "RestaurantCuisine");

            migrationBuilder.DropTable(
                name: "SpecialDay");

            migrationBuilder.DropTable(
                name: "HostStandDevice");

            migrationBuilder.DropTable(
                name: "Reservation");

            migrationBuilder.DropTable(
                name: "SlotInventory");

            migrationBuilder.DropTable(
                name: "StaffMembership")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "StaffMembershipHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "AvailabilityRule")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "AvailabilityRuleHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "UserAccount");

            migrationBuilder.DropTable(
                name: "Restaurant")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "RestaurantHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");
        }
    }
}
