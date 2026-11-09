using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolAdmission.Api.Data.Migrations;

/// <inheritdoc />
[DbContext(typeof(SchoolAdmissionDbContext))]
[Migration("20261109150000_EnforceHoldStateTimestamps")]
public partial class EnforceHoldStateTimestamps : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddCheckConstraint(
            name: "ck_admission_holds_expiry_after_creation",
            table: "admission_holds",
            sql: "expires_at > created_at");

        migrationBuilder.AddCheckConstraint(
            name: "ck_admission_holds_updated_after_creation",
            table: "admission_holds",
            sql: "updated_at >= created_at");

        migrationBuilder.AddCheckConstraint(
            name: "ck_admission_holds_confirmation_state",
            table: "admission_holds",
            sql: "(status = 'CONFIRMED') = (confirmed_at IS NOT NULL)");

        migrationBuilder.AddCheckConstraint(
            name: "ck_admission_holds_cancellation_state",
            table: "admission_holds",
            sql: "(status = 'CANCELLED') = (cancelled_at IS NOT NULL)");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "ck_admission_holds_expiry_after_creation",
            table: "admission_holds");

        migrationBuilder.DropCheckConstraint(
            name: "ck_admission_holds_updated_after_creation",
            table: "admission_holds");

        migrationBuilder.DropCheckConstraint(
            name: "ck_admission_holds_confirmation_state",
            table: "admission_holds");

        migrationBuilder.DropCheckConstraint(
            name: "ck_admission_holds_cancellation_state",
            table: "admission_holds");
    }
}
