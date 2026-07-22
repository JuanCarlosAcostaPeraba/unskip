using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unskip.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialLocalDeviceDirectory : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Devices",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Alias = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                AliasKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                ComputerName = table.Column<string>(type: "TEXT", maxLength: 253, nullable: true),
                Ipv4Address = table.Column<string>(type: "TEXT", maxLength: 15, nullable: true),
                Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                IsFavorite = table.Column<bool>(type: "INTEGER", nullable: false),
                PreferredDestination = table.Column<int>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                UpdatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                LastUsedAt = table.Column<long>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Devices", x => x.Id);
                table.CheckConstraint("CK_Devices_Destination", "\"ComputerName\" IS NOT NULL OR \"Ipv4Address\" IS NOT NULL");
            });

        migrationBuilder.CreateTable(
            name: "SendHistoryRecords",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                DeviceId = table.Column<Guid>(type: "TEXT", nullable: true),
                AliasSnapshot = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                DestinationKind = table.Column<int>(type: "INTEGER", nullable: false),
                DestinationSnapshot = table.Column<string>(type: "TEXT", maxLength: 253, nullable: false),
                DeliveryStatus = table.Column<int>(type: "INTEGER", nullable: false),
                OccurredAt = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SendHistoryRecords", x => x.Id);
                table.ForeignKey(
                    name: "FK_SendHistoryRecords_Devices_DeviceId",
                    column: x => x.DeviceId,
                    principalTable: "Devices",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Devices_AliasKey",
            table: "Devices",
            column: "AliasKey",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Devices_ComputerName",
            table: "Devices",
            column: "ComputerName",
            unique: true,
            filter: "\"ComputerName\" IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_Devices_Ipv4Address",
            table: "Devices",
            column: "Ipv4Address",
            unique: true,
            filter: "\"Ipv4Address\" IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_SendHistoryRecords_DeviceId",
            table: "SendHistoryRecords",
            column: "DeviceId");

        migrationBuilder.CreateIndex(
            name: "IX_SendHistoryRecords_OccurredAt",
            table: "SendHistoryRecords",
            column: "OccurredAt");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "SendHistoryRecords");

        migrationBuilder.DropTable(
            name: "Devices");
    }
}
