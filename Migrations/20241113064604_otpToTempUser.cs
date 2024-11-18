﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AonFreelancing.Migrations
{
    /// <inheritdoc />
    public partial class otpToTempUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_otps_AspNetUsers_PhoneNumber",
                table: "otps");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_AspNetUsers_PhoneNumber",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_TempUser_PhoneNumber",
                table: "TempUser",
                column: "PhoneNumber");

            migrationBuilder.AddForeignKey(
                name: "FK_otps_TempUser_PhoneNumber",
                table: "otps",
                column: "PhoneNumber",
                principalTable: "TempUser",
                principalColumn: "PhoneNumber",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_otps_TempUser_PhoneNumber",
                table: "otps");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_TempUser_PhoneNumber",
                table: "TempUser");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_AspNetUsers_PhoneNumber",
                table: "AspNetUsers",
                column: "PhoneNumber");

            migrationBuilder.AddForeignKey(
                name: "FK_otps_AspNetUsers_PhoneNumber",
                table: "otps",
                column: "PhoneNumber",
                principalTable: "AspNetUsers",
                principalColumn: "PhoneNumber",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
