using System;
using System.Collections.Generic;

namespace SkillMatch.API.Models;

public partial class AuditLog
{
    public ulong Id { get; set; }

    public ulong? UserId { get; set; }

    public string Action { get; set; } = null!;

    public string? Details { get; set; }

    public string? IpAddress { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? User { get; set; }
}
