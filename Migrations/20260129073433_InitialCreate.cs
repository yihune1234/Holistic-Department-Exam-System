using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HolisticDepartmentExamSystem.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    role_id = table.Column<int>(nullable: false),
                    role_name = table.Column<string>(maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.role_id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    user_id = table.Column<int>(nullable: false),
                    username = table.Column<string>(maxLength: 100, nullable: false),
                    password_hash = table.Column<string>(nullable: false),
                    role_id = table.Column<int>(nullable: false),
                    created_at = table.Column<DateTime>(nullable: false),
                    status = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.user_id);
                    table.ForeignKey(
                        name: "FK_Users_Roles_role_id",
                        column: x => x.role_id,
                        principalTable: "Roles",
                        principalColumn: "role_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ActivityLogs",
                columns: table => new
                {
                    log_id = table.Column<int>(nullable: false),
                    user_id = table.Column<int>(nullable: false),
                    action = table.Column<string>(maxLength: 255, nullable: true),
                    timestamp = table.Column<DateTime>(nullable: false),
                    ip_address = table.Column<string>(maxLength: 50, nullable: true),
                    device_info = table.Column<string>(maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityLogs", x => x.log_id);
                    table.ForeignKey(
                        name: "FK_ActivityLogs_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Exams",
                columns: table => new
                {
                    exam_id = table.Column<int>(nullable: false),
                    title = table.Column<string>(maxLength: 255, nullable: false),
                    description = table.Column<string>(nullable: true),
                    duration_minutes = table.Column<int>(nullable: false),
                    total_marks = table.Column<int>(nullable: false),
                    status = table.Column<string>(maxLength: 50, nullable: true),
                    created_by = table.Column<int>(nullable: false),
                    created_at = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exams", x => x.exam_id);
                    table.ForeignKey(
                        name: "FK_Exams_Users_created_by",
                        column: x => x.created_by,
                        principalTable: "Users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Students",
                columns: table => new
                {
                    student_id = table.Column<int>(nullable: false),
                    user_id = table.Column<int>(nullable: false),
                    full_name = table.Column<string>(maxLength: 255, nullable: true),
                    email = table.Column<string>(maxLength: 255, nullable: true),
                    department = table.Column<string>(maxLength: 100, nullable: true),
                    year_of_study = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Students", x => x.student_id);
                    table.ForeignKey(
                        name: "FK_Students_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Feedbacks",
                columns: table => new
                {
                    feedback_id = table.Column<int>(nullable: false),
                    user_id = table.Column<int>(nullable: false),
                    exam_id = table.Column<int>(nullable: false),
                    rating = table.Column<int>(nullable: false),
                    comment = table.Column<string>(nullable: true),
                    created_at = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feedbacks", x => x.feedback_id);
                    table.ForeignKey(
                        name: "FK_Feedbacks_Exams_exam_id",
                        column: x => x.exam_id,
                        principalTable: "Exams",
                        principalColumn: "exam_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Feedbacks_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Questions",
                columns: table => new
                {
                    question_id = table.Column<int>(nullable: false),
                    exam_id = table.Column<int>(nullable: false),
                    question_text = table.Column<string>(nullable: true),
                    question_type = table.Column<string>(nullable: true),
                    marks = table.Column<int>(nullable: false),
                    question_order = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Questions", x => x.question_id);
                    table.ForeignKey(
                        name: "FK_Questions_Exams_exam_id",
                        column: x => x.exam_id,
                        principalTable: "Exams",
                        principalColumn: "exam_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExamAttempts",
                columns: table => new
                {
                    attempt_id = table.Column<int>(nullable: false),
                    student_id = table.Column<int>(nullable: false),
                    exam_id = table.Column<int>(nullable: false),
                    start_time = table.Column<DateTime>(nullable: false),
                    end_time = table.Column<DateTime>(nullable: true),
                    status = table.Column<string>(maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamAttempts", x => x.attempt_id);
                    table.ForeignKey(
                        name: "FK_ExamAttempts_Exams_exam_id",
                        column: x => x.exam_id,
                        principalTable: "Exams",
                        principalColumn: "exam_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamAttempts_Students_student_id",
                        column: x => x.student_id,
                        principalTable: "Students",
                        principalColumn: "student_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExamPasswords",
                columns: table => new
                {
                    password_id = table.Column<int>(nullable: false),
                    exam_id = table.Column<int>(nullable: false),
                    student_id = table.Column<int>(nullable: false),
                    password_hash = table.Column<string>(maxLength: 255, nullable: true),
                    is_used = table.Column<bool>(nullable: false),
                    expires_at = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamPasswords", x => x.password_id);
                    table.ForeignKey(
                        name: "FK_ExamPasswords_Exams_exam_id",
                        column: x => x.exam_id,
                        principalTable: "Exams",
                        principalColumn: "exam_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamPasswords_Students_student_id",
                        column: x => x.student_id,
                        principalTable: "Students",
                        principalColumn: "student_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Choices",
                columns: table => new
                {
                    choice_id = table.Column<int>(nullable: false),
                    question_id = table.Column<int>(nullable: false),
                    choice_text = table.Column<string>(nullable: true),
                    is_correct = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Choices", x => x.choice_id);
                    table.ForeignKey(
                        name: "FK_Choices_Questions_question_id",
                        column: x => x.question_id,
                        principalTable: "Questions",
                        principalColumn: "question_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Results",
                columns: table => new
                {
                    result_id = table.Column<int>(nullable: false),
                    attempt_id = table.Column<int>(nullable: false),
                    total_score = table.Column<int>(nullable: false),
                    grade = table.Column<string>(maxLength: 10, nullable: true),
                    pass_status = table.Column<string>(maxLength: 20, nullable: true),
                    published_at = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Results", x => x.result_id);
                    table.ForeignKey(
                        name: "FK_Results_ExamAttempts_attempt_id",
                        column: x => x.attempt_id,
                        principalTable: "ExamAttempts",
                        principalColumn: "attempt_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExamAnswers",
                columns: table => new
                {
                    answer_id = table.Column<int>(nullable: false),
                    attempt_id = table.Column<int>(nullable: false),
                    question_id = table.Column<int>(nullable: false),
                    selected_choice_id = table.Column<int>(nullable: true),
                    is_flagged = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamAnswers", x => x.answer_id);
                    table.ForeignKey(
                        name: "FK_ExamAnswers_Choices_selected_choice_id",
                        column: x => x.selected_choice_id,
                        principalTable: "Choices",
                        principalColumn: "choice_id");
                    table.ForeignKey(
                        name: "FK_ExamAnswers_ExamAttempts_attempt_id",
                        column: x => x.attempt_id,
                        principalTable: "ExamAttempts",
                        principalColumn: "attempt_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamAnswers_Questions_question_id",
                        column: x => x.question_id,
                        principalTable: "Questions",
                        principalColumn: "question_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "role_id", "role_name" },
                values: new object[,]
                {
                    { 1, "Admin" },
                    { 2, "Coordinator" },
                    { 3, "Student" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "user_id", "created_at", "password_hash", "role_id", "status", "username" },
                values: new object[] { 1, new DateTime(2024, 12, 31, 21, 0, 0, 0, DateTimeKind.Utc), "admin123", 1, true, "admin" });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_user_id",
                table: "ActivityLogs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Choices_question_id",
                table: "Choices",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAnswers_attempt_id",
                table: "ExamAnswers",
                column: "attempt_id");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAnswers_question_id",
                table: "ExamAnswers",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAnswers_selected_choice_id",
                table: "ExamAnswers",
                column: "selected_choice_id");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAttempts_exam_id",
                table: "ExamAttempts",
                column: "exam_id");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAttempts_student_id",
                table: "ExamAttempts",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_ExamPasswords_exam_id",
                table: "ExamPasswords",
                column: "exam_id");

            migrationBuilder.CreateIndex(
                name: "IX_ExamPasswords_student_id",
                table: "ExamPasswords",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_Exams_created_by",
                table: "Exams",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_exam_id",
                table: "Feedbacks",
                column: "exam_id");

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_user_id",
                table: "Feedbacks",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_exam_id",
                table: "Questions",
                column: "exam_id");

            migrationBuilder.CreateIndex(
                name: "IX_Results_attempt_id",
                table: "Results",
                column: "attempt_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Students_user_id",
                table: "Students",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_role_id",
                table: "Users",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_Users_username",
                table: "Users",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityLogs");

            migrationBuilder.DropTable(
                name: "ExamAnswers");

            migrationBuilder.DropTable(
                name: "ExamPasswords");

            migrationBuilder.DropTable(
                name: "Feedbacks");

            migrationBuilder.DropTable(
                name: "Results");

            migrationBuilder.DropTable(
                name: "Choices");

            migrationBuilder.DropTable(
                name: "ExamAttempts");

            migrationBuilder.DropTable(
                name: "Questions");

            migrationBuilder.DropTable(
                name: "Students");

            migrationBuilder.DropTable(
                name: "Exams");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
