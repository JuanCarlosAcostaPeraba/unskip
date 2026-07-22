using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unskip.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class ExpandSendHistory : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ComputerNameSnapshot",
            table: "SendHistoryRecords",
            type: "TEXT",
            maxLength: 253,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "DiagnosticSummary",
            table: "SendHistoryRecords",
            type: "TEXT",
            maxLength: 1024,
            nullable: true);

        migrationBuilder.AddColumn<long>(
            name: "DurationTicks",
            table: "SendHistoryRecords",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0L);

        migrationBuilder.AddColumn<int>(
            name: "ExitCode",
            table: "SendHistoryRecords",
            type: "INTEGER",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "FailureCategory",
            table: "SendHistoryRecords",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<string>(
            name: "Ipv4AddressSnapshot",
            table: "SendHistoryRecords",
            type: "TEXT",
            maxLength: 15,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "MessageLength",
            table: "SendHistoryRecords",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ComputerNameSnapshot",
            table: "SendHistoryRecords");

        migrationBuilder.DropColumn(
            name: "DiagnosticSummary",
            table: "SendHistoryRecords");

        migrationBuilder.DropColumn(
            name: "DurationTicks",
            table: "SendHistoryRecords");

        migrationBuilder.DropColumn(
            name: "ExitCode",
            table: "SendHistoryRecords");

        migrationBuilder.DropColumn(
            name: "FailureCategory",
            table: "SendHistoryRecords");

        migrationBuilder.DropColumn(
            name: "Ipv4AddressSnapshot",
            table: "SendHistoryRecords");

        migrationBuilder.DropColumn(
            name: "MessageLength",
            table: "SendHistoryRecords");
    }
}
