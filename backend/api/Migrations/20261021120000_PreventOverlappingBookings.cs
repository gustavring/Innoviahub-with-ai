using api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace api.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261021120000_PreventOverlappingBookings")]
public class PreventOverlappingBookings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE EXTENSION IF NOT EXISTS btree_gist;

            ALTER TABLE "Bookings"
            ADD CONSTRAINT "Bookings_NoOverlap"
            EXCLUDE USING gist (
                "ResourceId" WITH =,
                tstzrange("StartTime", "EndTime", '[)') WITH &&
            );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE "Bookings"
            DROP CONSTRAINT "Bookings_NoOverlap";
            """);
    }
}
