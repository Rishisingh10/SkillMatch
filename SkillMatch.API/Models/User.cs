using System;
using System.Collections.Generic;

namespace SkillMatch.API.Models;

public partial class User
{
    public ulong Id { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Role { get; set; } = null!;

    public bool? IsActive { get; set; }

    public bool IsVerified { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual CandidateProfile? CandidateProfile { get; set; }

    public virtual RecruiterProfile? RecruiterProfile { get; set; }
}
