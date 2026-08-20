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
    private readonly ResumeParserService _parser;
    private readonly IWebHostEnvironment _environment;

    public CandidateController(
        SkillMatchDbContext context,
        MatchingEngine matcher,
        ResumeParserService parser,
        IWebHostEnvironment environment)
    {
        _context = context;
        _matcher = matcher;
        _parser = parser;
        _environment = environment;
    }

    // GET: api/Candidate/{candidateId}/gap-analysis/{jobId}
    [HttpGet("{candidateId}/gap-analysis/{jobId}")]
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

        if (candidate == null || job == null)
        {
            return NotFound("Candidate or Job record not found.");
        }

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

        var report = _matcher.EvaluateDetailed(
            candidateSkills,
            requiredJobSkills,
            candidate.TotalExperienceYears ?? 0,
            job.MinExperienceYears ?? 0
        );

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
    public async Task<IActionResult> UploadResume(
        ulong candidateId,
        IFormFile file)
    {
        // 1. Check candidate
        var candidate = await _context.CandidateProfiles
            .Include(c => c.CandidateSkills)
            .FirstOrDefaultAsync(c => c.Id == candidateId);

        if (candidate == null)
        {
            return NotFound("Candidate profile not found.");
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

        // 7. Parse text
        using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            extractedText = extension == ".pdf"
                ? _parser.ExtractTextFromPdf(stream)
                : _parser.ExtractTextFromDocx(stream);
        }

        // 8. Extract skills automatically from parsed text against global taxonomy
        var knownSkillEntities = await _context.Skills.ToListAsync();
        var knownSkillNames = knownSkillEntities.Select(s => s.Name).ToList();

        var extractedSkills = _parser.ExtractSkillsFromText(extractedText, knownSkillNames);

        // Add auto-extracted skills to CandidateSkills if not already present
        var existingSkillIds = candidate.CandidateSkills.Select(cs => cs.SkillId).ToHashSet();
        int newSkillsAdded = 0;

        foreach (var extractedSkillName in extractedSkills)
        {
            var skillEntity = knownSkillEntities.FirstOrDefault(s =>
                s.Name.Equals(extractedSkillName, StringComparison.OrdinalIgnoreCase));

            if (skillEntity != null && !existingSkillIds.Contains(skillEntity.Id))
            {
                _context.CandidateSkills.Add(new CandidateSkill
                {
                    CandidateId = candidateId,
                    SkillId = skillEntity.Id,
                    YearsExperience = candidate.TotalExperienceYears ?? 1.0m,
                    IsVerifiedByUser = false
                });
                newSkillsAdded++;
            }
        }

        // 9. Save resume record & newly extracted skills
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

        // 10. Return result
        var previewLength = Math.Min(extractedText.Length, 200);

        return Ok(new
        {
            Message = "Resume uploaded, verified, and parsed successfully.",
            ResumeId = resumeRecord.Id,
            FileName = resumeRecord.FileName,
            FileType = resumeRecord.FileType,
            ExtractedTextPreview = extractedText.Substring(0, previewLength) + (extractedText.Length > 200 ? "..." : ""),
            ExtractedSkills = extractedSkills,
            NewSkillsAddedToProfile = newSkillsAdded
        });
    }
}