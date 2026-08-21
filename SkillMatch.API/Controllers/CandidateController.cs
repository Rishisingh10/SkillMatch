using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillMatch.API.Models;
using SkillMatch.API.Services;

namespace SkillMatch.API.Controllers;

[Authorize(Roles = "CANDIDATE,ADMIN")]
[Route("api/[controller]")]
[ApiController]
public class CandidateController : ControllerBase
{
    private readonly SkillMatchDbContext _context;
    private readonly MatchingEngine _matcher;
    private readonly SemanticMatchingEngine _semanticEngine;
    private readonly ResumeParserService _parser;
    private readonly AIService _aiService;
    private readonly IWebHostEnvironment _environment;

    public CandidateController(
        SkillMatchDbContext context,
        MatchingEngine matcher,
        SemanticMatchingEngine semanticEngine,
        ResumeParserService parser,
        AIService aiService,
        IWebHostEnvironment environment)
    {
        _context = context;
        _matcher = matcher;
        _semanticEngine = semanticEngine;
        _parser = parser;
        _aiService = aiService;
        _environment = environment;
    }

    // GET: api/Candidate/jobs
    [HttpGet("jobs")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllJobs()
    {
        var jobs = await _context.Jobs
            .Include(j => j.JobSkills)
            .ThenInclude(js => js.Skill)
            .Where(j => j.Status == "ACTIVE")
            .Select(j => new {
                j.Id,
                j.Title,
                j.Description,
                j.MinExperienceYears,
                j.CreatedAt,
                Skills = j.JobSkills.Select(js => js.Skill.Name).ToList()
            })
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync();
        
        return Ok(jobs);
    }

    // GET: api/Candidate/{candidateId}/gap-analysis/{jobId}
    [HttpGet("{candidateId}/gap-analysis/{jobId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetGapAnalysis(
        ulong candidateId,
        ulong jobId)
    {
        var candidate = await _context.CandidateProfiles
            .Include(c => c.CandidateSkills)
            .ThenInclude(cs => cs.Skill)
            .FirstOrDefaultAsync(c => c.Id == candidateId);

        var job = await _context.Jobs
            .Include(j => j.JobSkills)
            .ThenInclude(js => js.Skill)
            .FirstOrDefaultAsync(j => j.Id == jobId);

        if (candidate == null)
        {
            var user = await _context.Users.FindAsync(1ul);
            if (user == null) {
                user = new User { Id = 1, Email = "test@test.com", PasswordHash = "hash", Role = "CANDIDATE" };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }
            // Auto-create for testing
            candidate = new CandidateProfile { Id = candidateId, UserId = 1, FullName = "Test Candidate", Headline = "Software Developer", TotalExperienceYears = 2 };
            _context.CandidateProfiles.Add(candidate);
            
            var s1 = await _context.Skills.FirstOrDefaultAsync(s => s.Name == "React") ?? new Skill { Name = "React", Category = "Technical" };
            var s2 = await _context.Skills.FirstOrDefaultAsync(s => s.Name == "Node.js") ?? new Skill { Name = "Node.js", Category = "Technical" };
            var s3 = await _context.Skills.FirstOrDefaultAsync(s => s.Name == "MongoDB") ?? new Skill { Name = "MongoDB", Category = "Technical" };
            
            _context.CandidateSkills.AddRange(
                new CandidateSkill { Candidate = candidate, Skill = s1, YearsExperience = 2 },
                new CandidateSkill { Candidate = candidate, Skill = s2, YearsExperience = 1 },
                new CandidateSkill { Candidate = candidate, Skill = s3, YearsExperience = 2 }
            );
            
            await _context.SaveChangesAsync();
        }
        
        if (job == null)
        {
            var recUser = await _context.Users.FindAsync(2ul);
            if (recUser == null) {
                recUser = new User { Id = 2, Email = "recruiter@test.com", PasswordHash = "hash", Role = "RECRUITER" };
                _context.Users.Add(recUser);
                await _context.SaveChangesAsync();
            }
            var recruiter = await _context.RecruiterProfiles.FindAsync(1ul);
            if (recruiter == null) {
                recruiter = new RecruiterProfile { Id = 1, UserId = 2, CompanyName = "Test Company" };
                _context.RecruiterProfiles.Add(recruiter);
                await _context.SaveChangesAsync();
            }
            // Auto-create for testing
            job = new Job { Id = jobId, RecruiterId = 1, Title = "Senior Frontend Engineer", Description = "Looking for a React expert with SQL backend knowledge.", MinExperienceYears = 4 };
            _context.Jobs.Add(job);
            
            var s4 = await _context.Skills.FirstOrDefaultAsync(s => s.Name == "React") ?? new Skill { Name = "React", Category = "Technical" };
            var s5 = await _context.Skills.FirstOrDefaultAsync(s => s.Name == "SQL") ?? new Skill { Name = "SQL", Category = "Technical" };
            var s6 = await _context.Skills.FirstOrDefaultAsync(s => s.Name == "TypeScript") ?? new Skill { Name = "TypeScript", Category = "Technical" };
            var s7 = await _context.Skills.FirstOrDefaultAsync(s => s.Name == "Azure") ?? new Skill { Name = "Azure", Category = "Technical" };

            _context.JobSkills.AddRange(
                new JobSkill { Job = job, Skill = s4, IsMandatory = true },
                new JobSkill { Job = job, Skill = s5, IsMandatory = true },
                new JobSkill { Job = job, Skill = s6, IsMandatory = false },
                new JobSkill { Job = job, Skill = s7, IsMandatory = false }
            );
            
            await _context.SaveChangesAsync();
        }

        // Retrieve candidate's latest uploaded resume for semantic similarity calculation
        var latestResume = await _context.Resumes
            .Where(r => r.CandidateId == candidateId)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();

        // 1. Calculate AI Semantic Cosine Similarity Score
        decimal semanticFitScore = await _semanticEngine.ComputeSemanticFitScoreAsync(
            candidate,
            job,
            latestResume?.ParsedRawText
        );

        var candidateSkills = candidate.CandidateSkills
            .Select(cs => new CandidateSkillInput
            {
                SkillName = cs.Skill.Name,
                YearsExperience = cs.YearsExperience ?? candidate.TotalExperienceYears ?? 0
            })
            .ToList();

        var requiredJobSkills = job.JobSkills
            .Select(js => new JobSkillInput
            {
                SkillName = js.Skill.Name,
                IsMandatory = js.IsMandatory ?? true
            })
            .ToList();

        // 2. Evaluate Candidate Match Report
        var report = _matcher.EvaluateDetailed(
            candidateSkills,
            requiredJobSkills,
            candidate.TotalExperienceYears ?? 0,
            job.MinExperienceYears ?? 0,
            semanticFitScore
        );

        // 3. Persist / Update MatchResult in Database
        var existingResult = await _context.MatchResults
            .FirstOrDefaultAsync(m => m.CandidateId == candidateId && m.JobId == jobId);

        if (existingResult == null)
        {
            _context.MatchResults.Add(new MatchResult
            {
                CandidateId = candidateId,
                JobId = jobId,
                OverallMatchScore = report.OverallScore,
                SkillMatchScore = report.SkillScore,
                ExperienceFitScore = report.ExperienceScore,
                SemanticFitScore = report.SemanticFitScore,
                MatchedSkillsJson = JsonSerializer.Serialize(report.MatchedSkills),
                MissingSkillsJson = JsonSerializer.Serialize(report.MissingSkills),
                ExplanationNotes = report.Explanation,
                ComputedAt = DateTime.UtcNow
            });
        }
        else
        {
            existingResult.OverallMatchScore = report.OverallScore;
            existingResult.SkillMatchScore = report.SkillScore;
            existingResult.ExperienceFitScore = report.ExperienceScore;
            existingResult.SemanticFitScore = report.SemanticFitScore;
            existingResult.MatchedSkillsJson = JsonSerializer.Serialize(report.MatchedSkills);
            existingResult.MissingSkillsJson = JsonSerializer.Serialize(report.MissingSkills);
            existingResult.ExplanationNotes = report.Explanation;
            existingResult.ComputedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            JobId = jobId,
            JobTitle = job.Title,

            MatchSummary = new
            {
                OverallScore = report.OverallScore,
                SkillMatchScore = report.SkillScore,
                ExperienceFitScore = report.ExperienceScore,
                SkillProficiencyScore = report.SkillProficiencyScore,
                SemanticFitScore = report.SemanticFitScore,
                HasAllMandatorySkills = report.HasAllMandatorySkills
            },

            SkillBreakdown = new
            {
                MatchedSkills = report.MatchedSkills,
                MissingSkills = report.MissingSkills,
                MissingMandatorySkills = report.MissingMandatorySkills
            },

            Explanation = report.Explanation
        });
    }

    // POST: api/Candidate/{candidateId}/resume/upload
    [HttpPost("{candidateId}/resume/upload")]
    [Consumes("multipart/form-data")]
    [AllowAnonymous]
    public async Task<IActionResult> UploadResume(
        ulong candidateId,
        IFormFile file,
        [FromForm] string? targetJobTitle,
        [FromForm] ulong? targetJobId)
    {
        // 1. Check candidate
        var candidate = await _context.CandidateProfiles
            .Include(c => c.CandidateSkills)
            .FirstOrDefaultAsync(c => c.Id == candidateId);

        if (candidate == null)
        {
            // Auto-create dummy user and candidate for testing
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == 1);
            if (user == null) {
                user = new User { Id = 1, Email = "test@test.com", PasswordHash = "hash", Role = "CANDIDATE" };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }
            candidate = new CandidateProfile { Id = candidateId, UserId = 1, FullName = "Test Candidate", Headline = "Software Developer", TotalExperienceYears = 2 };
            _context.CandidateProfiles.Add(candidate);
            await _context.SaveChangesAsync();
        }

        // 2. Check file presence and 10MB size limit
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file was uploaded.");
        }

        const long maxSizeBytes = 10 * 1024 * 1024; // 10 MB
        if (file.Length > maxSizeBytes)
        {
            return BadRequest("File size exceeds maximum limit of 10MB.");
        }

        // 3. Check extension
        var extension = Path
            .GetExtension(file.FileName)
            .ToLowerInvariant();

        var allowedExtensions = new[] { ".pdf", ".docx" };

        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest("Invalid file format. Only PDF and DOCX files are allowed.");
        }

        // 4. Validate header / magic bytes
        using (var checkStream = file.OpenReadStream())
        {
            if (extension == ".pdf" && !_parser.ValidatePdfHeader(checkStream))
            {
                return BadRequest("Invalid PDF file header signature detected.");
            }
            if (extension == ".docx" && !_parser.ValidateDocxHeader(checkStream))
            {
                return BadRequest("Invalid DOCX file header signature detected.");
            }
        }

        // 5. Create uploads folder
        var uploadsFolder = Path.Combine(
            _environment.ContentRootPath,
            "wwwroot",
            "uploads");

        Directory.CreateDirectory(uploadsFolder);

        // 6. Save file
        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        string extractedText;

        using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
        {
            await file.CopyToAsync(stream);
        }

        // 7. Parse raw text
        using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            extractedText = extension == ".pdf"
                ? _parser.ExtractTextFromPdf(stream)
                : _parser.ExtractTextFromDocx(stream);
        }

        // 8. Run AI-Powered Structured Resume Extraction
        var aiExtractedData = await _aiService.ExtractStructuredResumeDataAsync(extractedText);

        // Auto-update Candidate Profile with AI parsed fields if empty or baseline
        if (!string.IsNullOrWhiteSpace(aiExtractedData.Phone)) candidate.Phone = aiExtractedData.Phone;
        if (!string.IsNullOrWhiteSpace(aiExtractedData.Location)) candidate.Location = aiExtractedData.Location;
        if (!string.IsNullOrWhiteSpace(aiExtractedData.Headline)) candidate.Headline = aiExtractedData.Headline;
        if (!string.IsNullOrWhiteSpace(aiExtractedData.EducationLevel)) candidate.EducationLevel = aiExtractedData.EducationLevel;
        if (aiExtractedData.TotalExperienceYears > 0) candidate.TotalExperienceYears = aiExtractedData.TotalExperienceYears;
        candidate.UpdatedAt = DateTime.UtcNow;

        // 9. Auto-populate Candidate Skills from AI Extraction & Taxonomy
        var knownSkillEntities = await _context.Skills.ToListAsync();
        var existingSkillIds = candidate.CandidateSkills.Select(cs => cs.SkillId).ToHashSet();
        int newSkillsAdded = 0;

        foreach (var item in aiExtractedData.ExtractedSkills)
        {
            var skillEntity = knownSkillEntities.FirstOrDefault(s =>
                s.Name.Equals(item.SkillName, StringComparison.OrdinalIgnoreCase))
                ?? new Skill { Name = item.SkillName, Category = "Technical" };

            if (skillEntity.Id == 0)
            {
                _context.Skills.Add(skillEntity);
                await _context.SaveChangesAsync();
                knownSkillEntities.Add(skillEntity);
            }

            if (!existingSkillIds.Contains(skillEntity.Id))
            {
                _context.CandidateSkills.Add(new CandidateSkill
                {
                    CandidateId = candidateId,
                    SkillId = skillEntity.Id,
                    YearsExperience = item.YearsExperience > 0 ? item.YearsExperience : candidate.TotalExperienceYears ?? 1.0m,
                    IsVerifiedByUser = false
                });
                existingSkillIds.Add(skillEntity.Id);
                newSkillsAdded++;
            }
        }

        // 10. Save resume record & newly extracted profile data
        var resumeRecord = new Resume
        {
            CandidateId = candidateId,
            FileName = file.FileName,
            FilePath = filePath,
            FileType = extension == ".pdf" ? "PDF" : "DOCX",
            FileSizeKb = (uint)Math.Ceiling(file.Length / 1024.0),
            ParsedRawText = extractedText,
            ParsingStatus = "COMPLETED"
        };

        _context.Resumes.Add(resumeRecord);
        await _context.SaveChangesAsync();

        // 11. Dynamic Job Generation for Role Analysis OR Link to Existing Job
        ulong finalJobId = 1; // Default
        
        if (targetJobId.HasValue && targetJobId.Value > 0)
        {
            finalJobId = targetJobId.Value;
        }
        else if (!string.IsNullOrWhiteSpace(targetJobTitle))
        {
            var recruiter = await _context.RecruiterProfiles.FindAsync(1ul);
            if (recruiter == null) {
                var recUser = await _context.Users.FindAsync(2ul);
                if (recUser == null) {
                    recUser = new User { Id = 2, Email = "recruiter@test.com", PasswordHash = "hash", Role = "RECRUITER" };
                    _context.Users.Add(recUser);
                    await _context.SaveChangesAsync();
                }
                recruiter = new RecruiterProfile { Id = 1, UserId = 2, CompanyName = "SkillMatch Corp" };
                _context.RecruiterProfiles.Add(recruiter);
                await _context.SaveChangesAsync();
            }

            var newJob = new Job 
            { 
                RecruiterId = 1, 
                Title = targetJobTitle, 
                Description = $"Dynamically generated requirements for {targetJobTitle}", 
                MinExperienceYears = 2 
            };
            _context.Jobs.Add(newJob);
            await _context.SaveChangesAsync();
            finalJobId = newJob.Id;

            // Generate some dummy skills based on keyword
            var titleLower = targetJobTitle.ToLower();
            List<string> reqSkills = new List<string>();
            if (titleLower.Contains("data") || titleLower.Contains("ai") || titleLower.Contains("machine"))
                reqSkills.AddRange(new[] { "Python", "SQL", "Machine Learning" });
            else if (titleLower.Contains("front") || titleLower.Contains("ui") || titleLower.Contains("react"))
                reqSkills.AddRange(new[] { "React", "JavaScript", "CSS" });
            else if (titleLower.Contains("back") || titleLower.Contains("api") || titleLower.Contains("node") || titleLower.Contains("engineer"))
                reqSkills.AddRange(new[] { "C#", "SQL", "Node.js" });
            else
                reqSkills.AddRange(new[] { "Communication", "Problem Solving", "Project Management" });

            foreach(var rs in reqSkills) 
            {
                var skillEntity = await _context.Skills.FirstOrDefaultAsync(s => s.Name == rs) ?? new Skill { Name = rs, Category = "Technical" };
                if (skillEntity.Id == 0) {
                    _context.Skills.Add(skillEntity);
                    await _context.SaveChangesAsync();
                }
                _context.JobSkills.Add(new JobSkill { JobId = newJob.Id, SkillId = skillEntity.Id, IsMandatory = true });
            }
            await _context.SaveChangesAsync();
        }

        // Create Application record if it doesn't exist
        var existingApp = await _context.Applications.FirstOrDefaultAsync(a => a.CandidateId == candidateId && a.JobId == finalJobId);
        if (existingApp == null)
        {
            _context.Applications.Add(new Application
            {
                CandidateId = candidateId,
                JobId = finalJobId,
                Status = "APPLIED"
            });
            await _context.SaveChangesAsync();
        }

        // 12. Return result
        var previewLength = Math.Min(extractedText.Length, 200);

        return Ok(new
        {
            Message = "Resume uploaded, verified, and AI-parsed successfully.",
            ResumeId = resumeRecord.Id,
            JobId = finalJobId,
            FileName = resumeRecord.FileName,
            FileType = resumeRecord.FileType,
            ExtractedTextPreview = extractedText.Substring(0, previewLength) + (extractedText.Length > 200 ? "..." : ""),
            AiExtractedProfile = new
            {
                Headline = candidate.Headline,
                Phone = candidate.Phone,
                Location = candidate.Location,
                EducationLevel = candidate.EducationLevel,
                TotalExperienceYears = candidate.TotalExperienceYears
            },
            ExtractedSkillsCount = aiExtractedData.ExtractedSkills.Count,
            NewSkillsAddedToProfile = newSkillsAdded
        });
    }
}