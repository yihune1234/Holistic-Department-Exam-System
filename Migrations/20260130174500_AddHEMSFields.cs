using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace HolisticDepartmentExamSystem.Migrations
{
    public partial class AddHEMSFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "last_activity",
                table: "Users",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "results_published",
                table: "Exams",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_blocked",
                table: "ExamAttempts",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "last_activity",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "results_published",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "is_blocked",
                table: "ExamAttempts");
        }
    }
}
