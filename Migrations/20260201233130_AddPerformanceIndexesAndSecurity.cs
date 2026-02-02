using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HolisticDepartmentExamSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexesAndSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExamPasswords_exam_id",
                table: "ExamPasswords");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "user_id",
                keyValue: 1,
                columns: new[] { "created_at", "password_hash" },
                values: new object[] { new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcg7b3XeKeUxWdeS86AGR0Ifq0a" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_last_activity",
                table: "Users",
                column: "last_activity");

            migrationBuilder.CreateIndex(
                name: "IX_Students_email",
                table: "Students",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "IX_Exams_results_published",
                table: "Exams",
                column: "results_published");

            migrationBuilder.CreateIndex(
                name: "IX_Exams_status",
                table: "Exams",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_ExamPasswords_exam_id_student_id",
                table: "ExamPasswords",
                columns: new[] { "exam_id", "student_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamAttempts_is_blocked",
                table: "ExamAttempts",
                column: "is_blocked");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAttempts_status",
                table: "ExamAttempts",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_timestamp",
                table: "ActivityLogs",
                column: "timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_last_activity",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Students_email",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Exams_results_published",
                table: "Exams");

            migrationBuilder.DropIndex(
                name: "IX_Exams_status",
                table: "Exams");

            migrationBuilder.DropIndex(
                name: "IX_ExamPasswords_exam_id_student_id",
                table: "ExamPasswords");

            migrationBuilder.DropIndex(
                name: "IX_ExamAttempts_is_blocked",
                table: "ExamAttempts");

            migrationBuilder.DropIndex(
                name: "IX_ExamAttempts_status",
                table: "ExamAttempts");

            migrationBuilder.DropIndex(
                name: "IX_ActivityLogs_timestamp",
                table: "ActivityLogs");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "user_id",
                keyValue: 1,
                columns: new[] { "created_at", "password_hash" },
                values: new object[] { new DateTime(2024, 12, 31, 21, 0, 0, 0, DateTimeKind.Utc), "admin123" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamPasswords_exam_id",
                table: "ExamPasswords",
                column: "exam_id");
        }
    }
}
