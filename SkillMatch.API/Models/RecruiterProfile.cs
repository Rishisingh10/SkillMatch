using System;
using System.Collections.Generic;

namespace SkillMatch.API.Models;

public partial class RecruiterProfile
{
    public ulong Id { get; set; }

    public ulong UserId { get; set; }

    public string CompanyName { get; set; } = null!;

    public string? CompanyWebsite { get; set; }

    public string? CompanySize { get; set; }

    public string? Designation { get; set; }

    public bool? IsApprovedByAdmin { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Job> Jobs { get; set; } = new List<Job>();

    public virtual User User { get; set; } = null!;
}
