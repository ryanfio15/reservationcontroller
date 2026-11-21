using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Immutability level 2 (B: Immutability).
    /// </summary>
    /// <remarks>
    /// <para>
    /// An <c>INSTEAD OF UPDATE, DELETE</c> trigger on each insert-only table. "Instead of" is the
    /// point: the statement never runs, so there is nothing to roll back and no partial write,
    /// and the trigger throws whatever privilege the caller holds. That is what level 1's
    /// <c>DENY</c> cannot do — a <c>DENY</c> is scoped to a principal, and the mistake this
    /// catches is made by the migration identity or by a human at a query window.
    /// </para>
    /// <para>
    /// What it still does not stop is an administrator disabling the trigger. Closing that needs
    /// Azure SQL ledger tables, which is Production (B: Immutability, level 3).
    /// </para>
    /// <para>
    /// Each table is also declared with <c>HasTrigger</c> in its EF configuration. Without that,
    /// EF writes rows with an <c>OUTPUT</c> clause, which SQL Server rejects on a table carrying
    /// a trigger — so an undeclared trigger would break every insert rather than only the
    /// updates it is meant to block.
    /// </para>
    /// </remarks>
    public partial class InsertOnlyTriggers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var table in InsertOnlyTables.Names)
            {
                var trigger = InsertOnlyTables.TriggerName(table);

                migrationBuilder.Sql(
                    $"""
                    CREATE OR ALTER TRIGGER [dbo].[{trigger}]
                        ON [dbo].[{table}]
                        INSTEAD OF UPDATE, DELETE
                    AS
                    BEGIN
                        SET NOCOUNT ON;

                        THROW 50001,
                            '{table} is insert-only. Record what changed as a new row; UPDATE and DELETE are refused by design (ARCHITECTURE.md, Immutability).',
                            1;
                    END;
                    """);
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in InsertOnlyTables.Names)
            {
                migrationBuilder.Sql(
                    $"DROP TRIGGER IF EXISTS [dbo].[{InsertOnlyTables.TriggerName(table)}];");
            }
        }
    }
}
