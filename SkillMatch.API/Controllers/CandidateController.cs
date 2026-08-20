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

        // 11. Return result
        var previewLength = Math.Min(extractedText.Length, 200);

        return Ok(new
        {
            Message = "Resume uploaded, verified, and AI-parsed successfully.",
            ResumeId = resumeRecord.Id,
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