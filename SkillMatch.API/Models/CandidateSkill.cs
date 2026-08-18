using System;
using System.Collections.Generic;

namespace SkillMatch.API.Models;

public partial class CandidateSkill
{
    public ulong CandidateId { get; set; }

    public ulong SkillId { get; set; }

    public decimal? YearsExperience { get; set; }

    public bool? IsVerifiedByUser { get; set; }

    public virtual CandidateProfile Candidate { get; set; } = null!;

    public virtual Skill Skill { get; set; } = null!;
}
