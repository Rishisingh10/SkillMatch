using System;
using System.Collections.Generic;

namespace SkillMatch.API.Models;

public partial class Resume
{
    public ulong Id { get; set; }

    public ulong CandidateId { get; set; }

    public string FileName { get; set; } = null!;

    public string FilePath { get; set; } = null!;

    public string FileType { get; set; } = null!;

    public uint FileSizeKb { get; set; }

    public string? ParsedRawText { get; set; }

    public string? ParsingStatus { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual CandidateProfile Candidate { get; set; } = null!;
}
