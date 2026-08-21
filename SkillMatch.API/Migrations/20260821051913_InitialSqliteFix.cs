using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillMatch.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialSqliteFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "job_categories",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "skills",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true, defaultValueSql: "'Technical'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    email = table.Column<string>(type: "TEXT", nullable: false),
                    password_hash = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    role = table.Column<string>(type: "TEXT", nullable: false, defaultValueSql: "'CANDIDATE'"),
                    is_active = table.Column<bool>(type: "INTEGER", nullable: false, defaultValueSql: "'1'"),
                    is_verified = table.Column<bool>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    user_id = table.Column<ulong>(type: "INTEGER", nullable: true),
                    action = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    details = table.Column<string>(type: "TEXT", nullable: true),
                    ip_address = table.Column<string>(type: "TEXT", maxLength: 45, nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                    table.ForeignKey(
                        name: "audit_logs_ibfk_1",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "candidate_profiles",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    user_id = table.Column<ulong>(type: "INTEGER", nullable: false),
                    full_name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    phone = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    location = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    total_experience_years = table.Column<decimal>(type: "TEXT", precision: 4, scale: 1, nullable: true, defaultValueSql: "'0.0'"),
                    education_level = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    headline = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    bio = table.Column<string>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                    table.ForeignKey(
                        name: "candidate_profiles_ibfk_1",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "recruiter_profiles",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    user_id = table.Column<ulong>(type: "INTEGER", nullable: false),
                    company_name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    company_website = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    company_size = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    designation = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    is_approved_by_admin = table.Column<bool>(type: "INTEGER", nullable: true, defaultValueSql: "'0'"),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                    table.ForeignKey(
                        name: "recruiter_profiles_ibfk_1",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "candidate_skills",
                columns: table => new
                {
                    candidate_id = table.Column<ulong>(type: "INTEGER", nullable: false),
                    skill_id = table.Column<ulong>(type: "INTEGER", nullable: false),
                    years_experience = table.Column<decimal>(type: "TEXT", precision: 4, scale: 1, nullable: true, defaultValueSql: "'0.0'"),
                    is_verified_by_user = table.Column<bool>(type: "INTEGER", nullable: true, defaultValueSql: "'1'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.candidate_id, x.skill_id });
                    table.ForeignKey(
                        name: "candidate_skills_ibfk_1",
                        column: x => x.candidate_id,
                        principalTable: "candidate_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "candidate_skills_ibfk_2",
                        column: x => x.skill_id,
                        principalTable: "skills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "resumes",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    candidate_id = table.Column<ulong>(type: "INTEGER", nullable: false),
                    file_name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    file_path = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    file_type = table.Column<string>(type: "TEXT", nullable: false),
                    file_size_kb = table.Column<uint>(type: "INTEGER", nullable: false),
                    parsed_raw_text = table.Column<string>(type: "TEXT", nullable: true),
                    parsing_status = table.Column<string>(type: "TEXT", nullable: true, defaultValueSql: "'PENDING'"),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                    table.ForeignKey(
                        name: "resumes_ibfk_1",
                        column: x => x.candidate_id,
                        principalTable: "candidate_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "jobs",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    recruiter_id = table.Column<ulong>(type: "INTEGER", nullable: false),
                    category_id = table.Column<ulong>(type: "INTEGER", nullable: true),
                    title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "TEXT", nullable: false),
                    min_experience_years = table.Column<decimal>(type: "TEXT", precision: 4, scale: 1, nullable: true, defaultValueSql: "'0.0'"),
                    max_experience_years = table.Column<decimal>(type: "TEXT", precision: 4, scale: 1, nullable: true),
                    location = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    job_type = table.Column<string>(type: "TEXT", nullable: true, defaultValueSql: "'FULL_TIME'"),
                    status = table.Column<string>(type: "TEXT", nullable: true, defaultValueSql: "'ACTIVE'"),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                    table.ForeignKey(
                        name: "jobs_ibfk_1",
                        column: x => x.recruiter_id,
                        principalTable: "recruiter_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "jobs_ibfk_2",
                        column: x => x.category_id,
                        principalTable: "job_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "applications",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    job_id = table.Column<ulong>(type: "INTEGER", nullable: false),
                    candidate_id = table.Column<ulong>(type: "INTEGER", nullable: false),
                    status = table.Column<string>(type: "TEXT", nullable: true, defaultValueSql: "'APPLIED'"),
                    applied_at = table.Column<DateTime>(type: "TEXT", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                    table.ForeignKey(
                        name: "applications_ibfk_1",
                        column: x => x.job_id,
                        principalTable: "jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "applications_ibfk_2",
                        column: x => x.candidate_id,
                        principalTable: "candidate_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "job_skills",
                columns: table => new
                {
                    job_id = table.Column<ulong>(type: "INTEGER", nullable: false),
                    skill_id = table.Column<ulong>(type: "INTEGER", nullable: false),
                    is_mandatory = table.Column<bool>(type: "INTEGER", nullable: false, defaultValueSql: "'1'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.job_id, x.skill_id });
                    table.ForeignKey(
                        name: "job_skills_ibfk_1",
                        column: x => x.job_id,
                        principalTable: "jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "job_skills_ibfk_2",
                        column: x => x.skill_id,
                        principalTable: "skills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "match_results",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    candidate_id = table.Column<ulong>(type: "INTEGER", nullable: false),
                    job_id = table.Column<ulong>(type: "INTEGER", nullable: false),
                    overall_match_score = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    skill_match_score = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    semantic_fit_score = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    experience_fit_score = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    matched_skills_json = table.Column<string>(type: "TEXT", nullable: true),
                    missing_skills_json = table.Column<string>(type: "TEXT", nullable: true),
                    explanation_notes = table.Column<string>(type: "TEXT", nullable: true),
                    computed_at = table.Column<DateTime>(type: "TEXT", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                    table.ForeignKey(
                        name: "match_results_ibfk_1",
                        column: x => x.candidate_id,
                        principalTable: "candidate_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "match_results_ibfk_2",
                        column: x => x.job_id,
                        principalTable: "jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "candidate_id",
                table: "applications",
                column: "candidate_id");

            migrationBuilder.CreateIndex(
                name: "uq_job_candidate",
                table: "applications",
                columns: new[] { "job_id", "candidate_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "user_id",
                table: "audit_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "user_id1",
                table: "candidate_profiles",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "skill_id",
                table: "candidate_skills",
                column: "skill_id");

            migrationBuilder.CreateIndex(
                name: "name",
                table: "job_categories",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "skill_id1",
                table: "job_skills",
                column: "skill_id");

            migrationBuilder.CreateIndex(
                name: "category_id",
                table: "jobs",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "idx_job_status",
                table: "jobs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "recruiter_id",
                table: "jobs",
                column: "recruiter_id");

            migrationBuilder.CreateIndex(
                name: "idx_overall_score",
                table: "match_results",
                column: "overall_match_score");

            migrationBuilder.CreateIndex(
                name: "job_id",
                table: "match_results",
                column: "job_id");

            migrationBuilder.CreateIndex(
                name: "uq_match_candidate_job",
                table: "match_results",
                columns: new[] { "candidate_id", "job_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "user_id2",
                table: "recruiter_profiles",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_resume_candidate",
                table: "resumes",
                column: "candidate_id");

            migrationBuilder.CreateIndex(
                name: "idx_skill_name",
                table: "skills",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_user_role",
                table: "users",
                column: "role");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "applications");

            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "candidate_skills");

            migrationBuilder.DropTable(
                name: "job_skills");

            migrationBuilder.DropTable(
                name: "match_results");

            migrationBuilder.DropTable(
                name: "resumes");

            migrationBuilder.DropTable(
                name: "skills");

            migrationBuilder.DropTable(
                name: "jobs");

            migrationBuilder.DropTable(
                name: "candidate_profiles");

            migrationBuilder.DropTable(
                name: "recruiter_profiles");

            migrationBuilder.DropTable(
                name: "job_categories");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
