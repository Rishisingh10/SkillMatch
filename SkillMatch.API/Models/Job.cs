using System;
using System.Collections.Generic;

namespace SkillMatch.API.Models;

public partial class Job
{
    public ulong Id { get; set; }

    public ulong RecruiterId { get; set; }

    public ulong? CategoryId { get; set; }

    public string Title { get; set; } = null!;

    public string Description { get; set; } = null!;

    public decimal? MinExperienceYears { get; set; }

    public decimal? MaxExperienceYears { get; set; }

    public string? Location { get; set; }

    public string? JobType { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Application> Applications { get; set; } = new List<Application>();

    public virtual JobCategory? Category { get; set; }

    public virtual ICollection<JobSkill> JobSkills { get; set; } = new List<JobSkill>();

    public virtual ICollection<MatchResult> MatchResults { get; set; } = new List<MatchResult>();

    public virtual RecruiterProfile Recruiter { get; set; } = null!;
}
