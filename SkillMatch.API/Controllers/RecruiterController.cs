using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillMatch.API.Models;
using SkillMatch.API.Services;

namespace SkillMatch.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RecruiterController : ControllerBase
{
    private readonly SkillMatchDbContext _context;
    private readonly MatchingEngine _matcher = new();

    public RecruiterController(SkillMatchDbContext context)
    {
        _context = context;
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
            Status = "ACTIVE"
        };

        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();

        // Map required skills
        foreach (var skillName in request.RequiredSkills)
        {
            var skill = await _context.Skills.FirstOrDefaultAsync(s => s.Name == skillName)
                        ?? new Skill { Name = skillName, Category = "Technical" };

            if (skill.Id == 0) _context.Skills.Add(skill);
            await _context.SaveChangesAsync();

            _context.JobSkills.Add(new JobSkill
            {
                JobId = job.Id,
                SkillId = skill.Id,
                IsMandatory = true
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

        var requiredSkills = job.JobSkills.Select(s => s.Skill.Name).ToList();

        // Get all candidates who applied to this job
        var applications = await _context.Applications
            .Include(a => a.Candidate)
            .ThenInclude(c => c.CandidateSkills)
            .ThenInclude(cs => cs.Skill)
            .Where(a => a.JobId == jobId)
            .ToListAsync();

        var rankedList = new List<object>();

        foreach (var app in applications)
        {
            var candidateSkills = app.Candidate.CandidateSkills.Select(s => s.Skill.Name).ToList();

            // Run the matching algorithm dynamically
            var report = _matcher.Evaluate(
                candidateSkills,
                requiredSkills,
                app.Candidate.TotalExperienceYears ?? 0,
                job.MinExperienceYears ?? 0);

            rankedList.Add(new
            {
                ApplicationId = app.Id,
                CandidateId = app.Candidate.Id,
                Name = app.Candidate.FullName,
                Status = app.Status,
                OverallMatchScore = report.OverallScore,
                MatchedSkillsCount = report.MatchedSkills.Count,
                MissingSkills = report.MissingSkills
            });
        }

        // Rank highest score first
        var sortedRanking = rankedList.OrderByDescending(r => (decimal)((dynamic)r).OverallMatchScore).ToList();

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
        var requiredSkills = job.JobSkills.Select(s => s.Skill.Name).ToList();

        var candidates = await _context.CandidateProfiles
            .Include(c => c.CandidateSkills)
            .ThenInclude(cs => cs.Skill)
            .Where(c => request.CandidateIds.Contains(c.Id))
            .ToListAsync();

        var comparisonResult = candidates.Select(c =>
        {
            var cSkills = c.CandidateSkills.Select(s => s.Skill.Name).ToList();
            var report = _matcher.Evaluate(cSkills, requiredSkills, c.TotalExperienceYears ?? 0, job.MinExperienceYears ?? 0);

            return new
            {
                CandidateId = c.Id,
                Name = c.FullName,
                Experience = c.TotalExperienceYears,
                OverallScore = report.OverallScore,
                MissingSkills = report.MissingSkills,
                Explanation = report.Explanation
            };
        });

        return Ok(comparisonResult);
    }
}

// DTOs for the incoming JSON requests
public class JobCreationDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal MinExperienceYears { get; set; }
    public string JobType { get; set; } = "FULL_TIME";
    public List<string> RequiredSkills { get; set; } = new();
}

public class CompareRequestDto
{
    public ulong JobId { get; set; }
    public List<ulong> CandidateIds { get; set; } = new();
}