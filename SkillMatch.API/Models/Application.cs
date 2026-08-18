using System;
using System.Collections.Generic;

namespace SkillMatch.API.Models;

public partial class Application
{
    public ulong Id { get; set; }

    public ulong JobId { get; set; }

    public ulong CandidateId { get; set; }

    public string? Status { get; set; }

    public DateTime? AppliedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual CandidateProfile Candidate { get; set; } = null!;

    public virtual Job Job { get; set; } = null!;
}
