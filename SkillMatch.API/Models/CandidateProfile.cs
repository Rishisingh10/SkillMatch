using System;
using System.Collections.Generic;

namespace SkillMatch.API.Models;

public partial class CandidateProfile
{
    public ulong Id { get; set; }

    public ulong UserId { get; set; }

    public string FullName { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Location { get; set; }

    public decimal? TotalExperienceYears { get; set; }

    public string? EducationLevel { get; set; }

    public string? Headline { get; set; }

    public string? Bio { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Application> Applications { get; set; } = new List<Application>();

    public virtual ICollection<CandidateSkill> CandidateSkills { get; set; } = new List<CandidateSkill>();

    public virtual ICollection<MatchResult> MatchResults { get; set; } = new List<MatchResult>();

    public virtual ICollection<Resume> Resumes { get; set; } = new List<Resume>();

    public virtual User User { get; set; } = null!;
}
