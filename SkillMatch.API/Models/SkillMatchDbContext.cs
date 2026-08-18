using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace SkillMatch.API.Models;

public partial class SkillMatchDbContext : DbContext
{
    public SkillMatchDbContext()
    {
    }

    public SkillMatchDbContext(DbContextOptions<SkillMatchDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Application> Applications { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<CandidateProfile> CandidateProfiles { get; set; }

    public virtual DbSet<CandidateSkill> CandidateSkills { get; set; }

    public virtual DbSet<Job> Jobs { get; set; }

    public virtual DbSet<JobCategory> JobCategories { get; set; }

    public virtual DbSet<JobSkill> JobSkills { get; set; }

    public virtual DbSet<MatchResult> MatchResults { get; set; }

    public virtual DbSet<RecruiterProfile> RecruiterProfiles { get; set; }

    public virtual DbSet<Resume> Resumes { get; set; }

    public virtual DbSet<Skill> Skills { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseMySql("server=localhost;port=3306;user id=root;password=Romeo@123;database=skillmatch_db", Microsoft.EntityFrameworkCore.ServerVersion.Parse("8.0.39-mysql"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_unicode_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Application>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("applications");

            entity.HasIndex(e => e.CandidateId, "candidate_id");

            entity.HasIndex(e => new { e.JobId, e.CandidateId }, "uq_job_candidate").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AppliedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("applied_at");
            entity.Property(e => e.CandidateId).HasColumnName("candidate_id");
            entity.Property(e => e.JobId).HasColumnName("job_id");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'APPLIED'")
                .HasColumnType("enum('APPLIED','IN_REVIEW','SHORTLISTED','REJECTED')")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Candidate).WithMany(p => p.Applications)
                .HasForeignKey(d => d.CandidateId)
                .HasConstraintName("applications_ibfk_2");

            entity.HasOne(d => d.Job).WithMany(p => p.Applications)
                .HasForeignKey(d => d.JobId)
                .HasConstraintName("applications_ibfk_1");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("audit_logs");

            entity.HasIndex(e => e.UserId, "user_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Action)
                .HasMaxLength(100)
                .HasColumnName("action");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.Details)
                .HasColumnType("text")
                .HasColumnName("details");
            entity.Property(e => e.IpAddress)
                .HasMaxLength(45)
                .HasColumnName("ip_address");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("audit_logs_ibfk_1");
        });

        modelBuilder.Entity<CandidateProfile>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("candidate_profiles");

            entity.HasIndex(e => e.UserId, "user_id").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Bio)
                .HasColumnType("text")
                .HasColumnName("bio");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.EducationLevel)
                .HasMaxLength(100)
                .HasColumnName("education_level");
            entity.Property(e => e.FullName)
                .HasMaxLength(150)
                .HasColumnName("full_name");
            entity.Property(e => e.Headline)
                .HasMaxLength(255)
                .HasColumnName("headline");
            entity.Property(e => e.Location)
                .HasMaxLength(150)
                .HasColumnName("location");
            entity.Property(e => e.Phone)
                .HasMaxLength(30)
                .HasColumnName("phone");
            entity.Property(e => e.TotalExperienceYears)
                .HasPrecision(4, 1)
                .HasDefaultValueSql("'0.0'")
                .HasColumnName("total_experience_years");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithOne(p => p.CandidateProfile)
                .HasForeignKey<CandidateProfile>(d => d.UserId)
                .HasConstraintName("candidate_profiles_ibfk_1");
        });

        modelBuilder.Entity<CandidateSkill>(entity =>
        {
            entity.HasKey(e => new { e.CandidateId, e.SkillId })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity.ToTable("candidate_skills");

            entity.HasIndex(e => e.SkillId, "skill_id");

            entity.Property(e => e.CandidateId).HasColumnName("candidate_id");
            entity.Property(e => e.SkillId).HasColumnName("skill_id");
            entity.Property(e => e.IsVerifiedByUser)
                .HasDefaultValueSql("'1'")
                .HasColumnName("is_verified_by_user");
            entity.Property(e => e.YearsExperience)
                .HasPrecision(4, 1)
                .HasDefaultValueSql("'0.0'")
                .HasColumnName("years_experience");

            entity.HasOne(d => d.Candidate).WithMany(p => p.CandidateSkills)
                .HasForeignKey(d => d.CandidateId)
                .HasConstraintName("candidate_skills_ibfk_1");

            entity.HasOne(d => d.Skill).WithMany(p => p.CandidateSkills)
                .HasForeignKey(d => d.SkillId)
                .HasConstraintName("candidate_skills_ibfk_2");
        });

        modelBuilder.Entity<Job>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("jobs");

            entity.HasIndex(e => e.CategoryId, "category_id");

            entity.HasIndex(e => e.Status, "idx_job_status");

            entity.HasIndex(e => e.RecruiterId, "recruiter_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasColumnType("mediumtext")
                .HasColumnName("description");
            entity.Property(e => e.JobType)
                .HasDefaultValueSql("'FULL_TIME'")
                .HasColumnType("enum('FULL_TIME','PART_TIME','CONTRACT','REMOTE','HYBRID')")
                .HasColumnName("job_type");
            entity.Property(e => e.Location)
                .HasMaxLength(150)
                .HasColumnName("location");
            entity.Property(e => e.MaxExperienceYears)
                .HasPrecision(4, 1)
                .HasColumnName("max_experience_years");
            entity.Property(e => e.MinExperienceYears)
                .HasPrecision(4, 1)
                .HasDefaultValueSql("'0.0'")
                .HasColumnName("min_experience_years");
            entity.Property(e => e.RecruiterId).HasColumnName("recruiter_id");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'ACTIVE'")
                .HasColumnType("enum('DRAFT','ACTIVE','PAUSED','CLOSED')")
                .HasColumnName("status");
            entity.Property(e => e.Title)
                .HasMaxLength(200)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Category).WithMany(p => p.Jobs)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("jobs_ibfk_2");

            entity.HasOne(d => d.Recruiter).WithMany(p => p.Jobs)
                .HasForeignKey(d => d.RecruiterId)
                .HasConstraintName("jobs_ibfk_1");
        });

        modelBuilder.Entity<JobCategory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("job_categories");

            entity.HasIndex(e => e.Name, "name").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        modelBuilder.Entity<JobSkill>(entity =>
        {
            entity.HasKey(e => new { e.JobId, e.SkillId })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity.ToTable("job_skills");

            entity.HasIndex(e => e.SkillId, "skill_id");

            entity.Property(e => e.JobId).HasColumnName("job_id");
            entity.Property(e => e.SkillId).HasColumnName("skill_id");
            entity.Property(e => e.IsMandatory)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("is_mandatory");

            entity.HasOne(d => d.Job).WithMany(p => p.JobSkills)
                .HasForeignKey(d => d.JobId)
                .HasConstraintName("job_skills_ibfk_1");

            entity.HasOne(d => d.Skill).WithMany(p => p.JobSkills)
                .HasForeignKey(d => d.SkillId)
                .HasConstraintName("job_skills_ibfk_2");
        });

        modelBuilder.Entity<MatchResult>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("match_results");

            entity.HasIndex(e => e.OverallMatchScore, "idx_overall_score");

            entity.HasIndex(e => e.JobId, "job_id");

            entity.HasIndex(e => new { e.CandidateId, e.JobId }, "uq_match_candidate_job").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CandidateId).HasColumnName("candidate_id");
            entity.Property(e => e.ComputedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("computed_at");
            entity.Property(e => e.ExperienceFitScore)
                .HasPrecision(5, 2)
                .HasColumnName("experience_fit_score");
            entity.Property(e => e.ExplanationNotes)
                .HasColumnType("text")
                .HasColumnName("explanation_notes");
            entity.Property(e => e.JobId).HasColumnName("job_id");
            entity.Property(e => e.MatchedSkillsJson)
                .HasColumnType("json")
                .HasColumnName("matched_skills_json");
            entity.Property(e => e.MissingSkillsJson)
                .HasColumnType("json")
                .HasColumnName("missing_skills_json");
            entity.Property(e => e.OverallMatchScore)
                .HasPrecision(5, 2)
                .HasColumnName("overall_match_score");
            entity.Property(e => e.SemanticFitScore)
                .HasPrecision(5, 2)
                .HasColumnName("semantic_fit_score");
            entity.Property(e => e.SkillMatchScore)
                .HasPrecision(5, 2)
                .HasColumnName("skill_match_score");

            entity.HasOne(d => d.Candidate).WithMany(p => p.MatchResults)
                .HasForeignKey(d => d.CandidateId)
                .HasConstraintName("match_results_ibfk_1");

            entity.HasOne(d => d.Job).WithMany(p => p.MatchResults)
                .HasForeignKey(d => d.JobId)
                .HasConstraintName("match_results_ibfk_2");
        });

        modelBuilder.Entity<RecruiterProfile>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("recruiter_profiles");

            entity.HasIndex(e => e.UserId, "user_id").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CompanyName)
                .HasMaxLength(150)
                .HasColumnName("company_name");
            entity.Property(e => e.CompanySize)
                .HasMaxLength(50)
                .HasColumnName("company_size");
            entity.Property(e => e.CompanyWebsite)
                .HasMaxLength(255)
                .HasColumnName("company_website");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.Designation)
                .HasMaxLength(100)
                .HasColumnName("designation");
            entity.Property(e => e.IsApprovedByAdmin)
                .HasDefaultValueSql("'0'")
                .HasColumnName("is_approved_by_admin");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithOne(p => p.RecruiterProfile)
                .HasForeignKey<RecruiterProfile>(d => d.UserId)
                .HasConstraintName("recruiter_profiles_ibfk_1");
        });

        modelBuilder.Entity<Resume>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("resumes");

            entity.HasIndex(e => e.CandidateId, "idx_resume_candidate");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CandidateId).HasColumnName("candidate_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.FileName)
                .HasMaxLength(255)
                .HasColumnName("file_name");
            entity.Property(e => e.FilePath)
                .HasMaxLength(500)
                .HasColumnName("file_path");
            entity.Property(e => e.FileSizeKb).HasColumnName("file_size_kb");
            entity.Property(e => e.FileType)
                .HasColumnType("enum('PDF','DOCX')")
                .HasColumnName("file_type");
            entity.Property(e => e.ParsedRawText)
                .HasColumnType("mediumtext")
                .HasColumnName("parsed_raw_text");
            entity.Property(e => e.ParsingStatus)
                .HasDefaultValueSql("'PENDING'")
                .HasColumnType("enum('PENDING','PROCESSING','COMPLETED','FAILED')")
                .HasColumnName("parsing_status");

            entity.HasOne(d => d.Candidate).WithMany(p => p.Resumes)
                .HasForeignKey(d => d.CandidateId)
                .HasConstraintName("resumes_ibfk_1");
        });

        modelBuilder.Entity<Skill>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("skills");

            entity.HasIndex(e => e.Name, "idx_skill_name").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Category)
                .HasMaxLength(100)
                .HasDefaultValueSql("'Technical'")
                .HasColumnName("category");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "email").IsUnique();

            entity.HasIndex(e => e.Role, "idx_user_role");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("created_at");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("is_active");
            entity.Property(e => e.IsVerified).HasColumnName("is_verified");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.Role)
                .HasDefaultValueSql("'CANDIDATE'")
                .HasColumnType("enum('CANDIDATE','RECRUITER','ADMIN')")
                .HasColumnName("role");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("updated_at");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
