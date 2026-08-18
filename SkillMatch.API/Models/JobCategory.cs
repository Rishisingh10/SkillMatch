using System;
using System.Collections.Generic;

namespace SkillMatch.API.Models;

public partial class JobCategory
{
    public ulong Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Job> Jobs { get; set; } = new List<Job>();
}
