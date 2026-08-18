using System;
using System.Collections.Generic;

namespace SkillMatch.API.Models;

public partial class JobSkill
{
    public ulong JobId { get; set; }

    public ulong SkillId { get; set; }

    public bool? IsMandatory { get; set; }

    public virtual Job Job { get; set; } = null!;

    public virtual Skill Skill { get; set; } = null!;
}
