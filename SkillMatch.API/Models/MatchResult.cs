using System;
using System.Collections.Generic;

namespace SkillMatch.API.Models;

public partial class MatchResult
{
    public ulong Id { get; set; }

    public ulong CandidateId { get; set; }

    public ulong JobId { get; set; }

    public decimal OverallMatchScore { get; set; }

    public decimal SkillMatchScore { get; set; }

    public decimal SemanticFitScore { get; set; }

    public decimal ExperienceFitScore { get; set; }

    public string? MatchedSkillsJson { get; set; }

    public string? MissingSkillsJson { get; set; }

    public string? ExplanationNotes { get; set; }

    public DateTime? ComputedAt { get; set; }

    public virtual CandidateProfile Candidate { get; set; } = null!;

    public virtual Job Job { get; set; } = null!;
}
