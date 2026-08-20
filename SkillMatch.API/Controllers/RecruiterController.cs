using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillMatch.API.Models;
using SkillMatch.API.Services;

namespace SkillMatch.API.Controllers;

[Authorize(Roles = "RECRUITER,ADMIN")]
[Route("api/[controller]")]
[ApiController]
public class RecruiterController : ControllerBase
{
    private readonly SkillMatchDbContext _context;
    private readonly MatchingEngine _matcher;

    public RecruiterController(SkillMatchDbContext context, MatchingEngine matcher)
    {
        _context = context;
        _matcher = matcher;
    }

    // 1. CREATE JOB POSTING (FR-12, FR-13)
    [HttpPost("{recruiterId}/jobs")]
    public async Task<IActionResult> CreateJobPosting(ulong recruiterId, [FromBody] JobCreationDto request)
    {
        var recruiter = await _context.RecruiterProfiles.FindAsync(recruiterId);
        if (recruiter == null) return NotFound("Recruiter profile not found.");

        var job = new Job
        {
            RecruiterId = recruiterId,
            Title = request.Title,
            Description = request.Description,
            MinExperienceYears = request.MinExperienceYears,
            JobType = request.JobType,
            Status = "ACTIVE",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();

        // Map required skills
        foreach (var reqSkill in request.RequiredSkills)
        {
            var skillName = reqSkill.Name;
            var isMandatory = reqSkill.IsMandatory;

            var skill = await _context.Skills.FirstOrDefaultAsync(s => s.Name.ToLower() == skillName.ToLower())
                        ?? new Skill { Name = skillName, Category = "Technical" };

            if (skill.Id == 0)
            {
                _context.Skills.Add(skill);
                await _context.SaveChangesAsync();
            }

            _context.JobSkills.Add(new JobSkill
            {
                JobId = job.Id,
                SkillId = skill.Id,
                IsMandatory = isMandatory
            });
        }
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Job created successfully.", JobId = job.Id });
    }

    // 2. GET RANKED CANDIDATES FOR A JOB (FR-14, FR-15)
    [HttpGet("jobs/{jobId}/ranked-candidates")]
    public async Task<IActionResult> GetRankedCandidates(ulong jobId)
    {
        var job = await _context.Jobs
            .Include(j => j.JobSkills)
            .ThenInclude(js => js.Skill)
            .FirstOrDefaultAsync(j => j.Id == jobId);

        if (job == null) return NotFound("Job not found.");

        var requiredSkillInputs = job.JobSkills.Select(s => new JobSkillInput
        {
            SkillName = s.Skill.Name,
            IsMandatory = s.IsMandatory ?? true
        }).ToList();

        // Get all candidates who applied to this job
        var applications = await _context.Applications
            .Include(a => a.Candidate)
            .ThenInclude(c => c.CandidateSkills)
            .ThenInclude(cs => cs.Skill)
            .Where(a => a.JobId == jobId)
            .ToListAsync();

        var rankedList = new List<RankedCandidateDto>();

        foreach (var app in applications)
        {
            if (app.Candidate == null) continue;

            var candidateSkillInputs = app.Candidate.CandidateSkills.Select(s => new CandidateSkillInput
            {
                SkillName = s.Skill.Name,
                YearsExperience = s.YearsExperience ?? app.Candidate.TotalExperienceYears ?? 0
            }).ToList();

            var report = _matcher.EvaluateDetailed(
                candidateSkillInputs,
                requiredSkillInputs,
                app.Candidate.TotalExperienceYears ?? 0,
                job.MinExperienceYears ?? 0);

            rankedList.Add(new RankedCandidateDto
            {
                ApplicationId = app.Id,
                CandidateId = app.Candidate.Id,
                Name = app.Candidate?.FullName ?? string.Empty,
                Status = app.Status ?? "APPLIED",
                OverallMatchScore = report.OverallScore,
                SkillScore = report.SkillScore,
                ExperienceScore = report.ExperienceScore,
                HasAllMandatorySkills = report.HasAllMandatorySkills,
                MatchedSkillsCount = report.MatchedSkills.Count,
                MissingSkills = report.MissingSkills,
                MissingMandatorySkills = report.MissingMandatorySkills,
                Explanation = report.Explanation
            });
        }

        // Rank highest score first
        var sortedRanking = rankedList.OrderByDescending(r => r.OverallMatchScore).ToList();

        return Ok(sortedRanking);
    }

    // 3. UPDATE APPLICATION STATUS (SHORTLIST/REJECT) (FR-18)
    [HttpPatch("applications/{applicationId}/status")]
    public async Task<IActionResult> UpdateApplicationStatus(ulong applicationId, [FromBody] string newStatus)
    {
        var validStatuses = new[] { "APPLIED", "IN_REVIEW", "SHORTLISTED", "REJECTED" };
        if (!validStatuses.Contains(newStatus.ToUpper()))
            return BadRequest("Invalid status.");

        var application = await _context.Applications.FindAsync(applicationId);
        if (application == null) return NotFound("Application not found.");

        application.Status = newStatus.ToUpper();
        await _context.SaveChangesAsync();

        return Ok(new { Message = $"Application status updated to {application.Status}." });
    }

    // 4. SIDE-BY-SIDE CANDIDATE COMPARISON (FR-16)
    [HttpPost("candidates/compare")]
    public async Task<IActionResult> CompareCandidates([FromBody] CompareRequestDto request)
    {
        var job = await _context.Jobs
            .Include(j => j.JobSkills)
            .ThenInclude(js => js.Skill)
            .FirstOrDefaultAsync(j => j.Id == request.JobId);

        if (job == null) return NotFound("Job not found.");

        var requiredSkillInputs = job.JobSkills.Select(s => new JobSkillInput
        {
            SkillName = s.Skill.Name,
            IsMandatory = s.IsMandatory ?? true
        }).ToList();

        var candidates = await _context.CandidateProfiles
            .Include(c => c.CandidateSkills)
            .ThenInclude(cs => cs.Skill)
            .Where(c => request.CandidateIds.Contains(c.Id))
            .ToListAsync();

        var comparisonResult = candidates.Select(c =>
        {
            var cSkillInputs = c.CandidateSkills.Select(s => new CandidateSkillInput
            {
                SkillName = s.Skill.Name,
                YearsExperience = s.YearsExperience ?? c.TotalExperienceYears ?? 0
            }).ToList();

            var report = _matcher.EvaluateDetailed(
                cSkillInputs,
                requiredSkillInputs,
                c.TotalExperienceYears ?? 0,
                job.MinExperienceYears ?? 0);

            return new
            {
                CandidateId = c.Id,
                Name = c.FullName,
                Experience = c.TotalExperienceYears,
                OverallScore = report.OverallScore,
                SkillScore = report.SkillScore,
                ExperienceScore = report.ExperienceScore,
                HasAllMandatorySkills = report.HasAllMandatorySkills,
                MissingSkills = report.MissingSkills,
                MissingMandatorySkills = report.MissingMandatorySkills,
                Explanation = report.Explanation
            };
        });

        return Ok(comparisonResult);
    }
}

public class JobSkillRequirementDto
{
    public string Name { get; set; } = string.Empty;
    public bool IsMandatory { get; set; } = true;
}

public class JobCreationDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal MinExperienceYears { get; set; }
    public string JobType { get; set; } = "FULL_TIME";
    public List<JobSkillRequirementDto> RequiredSkills { get; set; } = new();
}

public class CompareRequestDto
{
    public ulong JobId { get; set; }
    public List<ulong> CandidateIds { get; set; } = new();
}

public class RankedCandidateDto
{
    public ulong ApplicationId { get; set; }
    public ulong CandidateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal OverallMatchScore { get; set; }
    public decimal SkillScore { get; set; }
    public decimal ExperienceScore { get; set; }
    public bool HasAllMandatorySkills { get; set; }
    public int MatchedSkillsCount { get; set; }
    public List<string> MissingSkills { get; set; } = new();
    public List<string> MissingMandatorySkills { get; set; } = new();
    public string Explanation { get; set; } = string.Empty;
}