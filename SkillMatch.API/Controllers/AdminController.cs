using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillMatch.API.Models;

namespace SkillMatch.API.Controllers;

[Authorize(Roles = "ADMIN")]
[Route("api/[controller]")]
[ApiController]
public class AdminController : ControllerBase
{
    private readonly SkillMatchDbContext _context;

    public AdminController(SkillMatchDbContext context)
    {
        _context = context;
    }

    // 1. GET ALL USERS
    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _context.Users
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.Role,
                u.IsActive,
                u.CreatedAt
            })
            .ToListAsync();

        return Ok(users);
    }

    // 2. APPROVE RECRUITER
    [HttpPatch("recruiters/{recruiterId}/approve")]
    public async Task<IActionResult> ApproveRecruiter(ulong recruiterId)
    {
        var recruiter = await _context.RecruiterProfiles
            .FindAsync(recruiterId);

        if (recruiter == null)
            return NotFound("Recruiter profile not found.");

        recruiter.IsApprovedByAdmin = true;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Message = $"Recruiter account for {recruiter.CompanyName} has been approved."
        });
    }

    // 3. ADD SKILL TO GLOBAL TAXONOMY
    [HttpPost("skills")]
    public async Task<IActionResult> AddSkillToTaxonomy(
        [FromBody] SkillDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Skill name is required.");

        var existingSkill = await _context.Skills
            .FirstOrDefaultAsync(s =>
                s.Name.ToLower() == request.Name.ToLower());

        if (existingSkill != null)
            return BadRequest(
                "Skill already exists in the system taxonomy.");

        var skill = new Skill
        {
            Name = request.Name,
            Category = request.Category
        };

        _context.Skills.Add(skill);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Message = "Skill successfully added to global taxonomy.",
            SkillId = skill.Id
        });
    }

    // 4. SYSTEM ANALYTICS
    [HttpGet("analytics")]
    public async Task<IActionResult> GetSystemAnalytics()
    {
        var totalCandidates =
            await _context.CandidateProfiles.CountAsync();

        var totalJobs =
            await _context.Jobs.CountAsync();

        var activeJobs =
            await _context.Jobs.CountAsync(j => j.Status == "ACTIVE");

        var totalApplications =
            await _context.Applications.CountAsync();

        return Ok(new
        {
            Metrics = new
            {
                TotalCandidates = totalCandidates,
                TotalJobPostings = totalJobs,
                ActiveJobs = activeJobs,
                TotalApplicationsProcessed = totalApplications
            },

            PlatformHealth = new
            {
                Status = "Operational",
                UptimeTarget = "99.9%",
                LastAuditTimestamp = DateTime.UtcNow
            }
        });
    }
}


// DTO
public class SkillDto
{
    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = "Technical";
}