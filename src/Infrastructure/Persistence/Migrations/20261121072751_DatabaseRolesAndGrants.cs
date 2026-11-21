using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Immutability level 1 and least privilege (B: Immutability, Security of stored data).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Three roles, as B: Security of stored data specifies — <c>app_runtime</c> (DML, with only
    /// <c>INSERT, SELECT</c> on the insert-only tables, and no DDL), <c>app_migrator</c> (DDL,
    /// the migration job's identity only) and <c>app_support_read</c> (read-only, and not the
    /// credential or session tables).
    /// </para>
    /// <para>
    /// Only roles are created here, not users. In Azure the two users come from the managed
    /// identities, created once by the server's Entra admin with
    /// <c>CREATE USER ... FROM EXTERNAL PROVIDER</c> (A6 item 4); locally the integration tests
    /// create SQL-authenticated users and add them to these roles. Either way the grants are the
    /// same, because they live on the role.
    /// </para>
    /// <para>
    /// <c>app_migrator</c> is deliberately *not* denied <c>UPDATE</c> and <c>DELETE</c> on the
    /// insert-only tables. B assigns that identity to level 2 — the <c>INSTEAD OF</c> triggers in
    /// the next migration — because a <c>DENY</c> there would be an obstacle to legitimate
    /// schema work, while the trigger stops the mistake that actually happens: a human or a
    /// migration running an <c>UPDATE</c> against history.
    /// </para>
    /// </remarks>
    public partial class DatabaseRolesAndGrants : Migration
    {
        private const string SupportDeniedTables = "HostAccessCode, HostSession";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            CreateRole(migrationBuilder, "app_runtime");
            CreateRole(migrationBuilder, "app_migrator");
            CreateRole(migrationBuilder, "app_support_read");

            // --- app_runtime: ordinary reads and writes, no schema changes -------------------
            migrationBuilder.Sql(
                """
                GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[dbo] TO [app_runtime];
                GRANT EXECUTE ON SCHEMA::[dbo] TO [app_runtime];
                """);

            // The insert-only tables lose UPDATE and DELETE. DENY beats GRANT in SQL Server, so
            // the schema-wide grant above cannot put them back.
            foreach (var table in InsertOnlyTables.Names)
            {
                migrationBuilder.Sql(
                    $"DENY UPDATE, DELETE ON [dbo].[{table}] TO [app_runtime];");
            }

            // --- app_migrator: schema changes, used by the migration job alone ---------------
            migrationBuilder.Sql(
                """
                ALTER ROLE [db_ddladmin] ADD MEMBER [app_migrator];
                ALTER ROLE [db_datareader] ADD MEMBER [app_migrator];
                ALTER ROLE [db_datawriter] ADD MEMBER [app_migrator];
                """);

            // --- app_support_read: read-only, and never a credential or a session ------------
            migrationBuilder.Sql("GRANT SELECT ON SCHEMA::[dbo] TO [app_support_read];");

            migrationBuilder.Sql(
                """
                DENY SELECT ON [dbo].[HostAccessCode] TO [app_support_read];
                DENY SELECT ON [dbo].[HostSession] TO [app_support_read];
                """);

            // A device row is useful to support; its two secret columns are not. Column-level
            // DENY keeps the label, pairing state and lockout timestamps readable while putting
            // the hashes out of reach.
            migrationBuilder.Sql(
                """
                DENY SELECT ON [dbo].[HostStandDevice]([CredentialHash]) TO [app_support_read];
                DENY SELECT ON [dbo].[HostStandDevice]([PairingCodeHash]) TO [app_support_read];
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Roles are dropped last, and only once nothing is a member of them; a role with
            // members cannot be dropped, which is the behaviour we want if a deployment still
            // has live users.
            migrationBuilder.Sql(
                """
                ALTER ROLE [db_ddladmin] DROP MEMBER [app_migrator];
                ALTER ROLE [db_datareader] DROP MEMBER [app_migrator];
                ALTER ROLE [db_datawriter] DROP MEMBER [app_migrator];
                """);

            DropRole(migrationBuilder, "app_support_read");
            DropRole(migrationBuilder, "app_migrator");
            DropRole(migrationBuilder, "app_runtime");
        }

        private static void CreateRole(MigrationBuilder migrationBuilder, string role) =>
            migrationBuilder.Sql(
                $"""
                IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE [name] = '{role}' AND [type] = 'R')
                    CREATE ROLE [{role}];
                """);

        private static void DropRole(MigrationBuilder migrationBuilder, string role) =>
            migrationBuilder.Sql(
                $"""
                IF EXISTS (SELECT 1 FROM sys.database_principals WHERE [name] = '{role}' AND [type] = 'R')
                    DROP ROLE [{role}];
                """);
    }
}
