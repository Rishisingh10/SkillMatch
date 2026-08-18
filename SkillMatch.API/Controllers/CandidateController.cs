using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillMatch.API.Models;
using SkillMatch.API.Services;

namespace SkillMatch.API.Controllers;

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

        var candidateSkillNames = candidate.CandidateSkills
            .Select(cs => cs.Skill.Name)
            .ToList();

        var requiredSkillNames = job.JobSkills
            .Select(js => js.Skill.Name)
            .ToList();

        var report = _matcher.Evaluate(
            candidateSkillNames,
            requiredSkillNames,
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
                ExperienceFitScore = report.ExperienceScore
            },

            SkillBreakdown = new
            {
                MatchedSkills = report.MatchedSkills,
                MissingSkills = report.MissingSkills
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
            .FindAsync(candidateId);

        if (candidate == null)
        {
            return NotFound("Candidate profile not found.");
        }

        // 2. Check file
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file was uploaded.");
        }

        // 3. Check extension
        var extension = Path
            .GetExtension(file.FileName)
            .ToLowerInvariant();

        var allowedExtensions = new[] { ".pdf", ".docx" };

        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest(
                "Invalid file format. Only PDF and DOCX files are allowed.");
        }

        // 4. Create uploads folder
        var uploadsFolder = Path.Combine(
            _environment.ContentRootPath,
            "wwwroot",
            "uploads");

        Directory.CreateDirectory(uploadsFolder);

        // 5. Generate safe unique filename
        var uniqueFileName =
            $"{Guid.NewGuid()}{extension}";

        var filePath = Path.Combine(
            uploadsFolder,
            uniqueFileName);

        // 6. Save file
        string extractedText;

        using (var stream = new FileStream(
            filePath,
            FileMode.Create,
            FileAccess.Write))
        {
            await file.CopyToAsync(stream);
        }

        // 7. Open saved file and parse it
        using (var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read))
        {
            if (extension == ".pdf")
            {
                extractedText =
                    _parser.ExtractTextFromPdf(stream);
            }
            else
            {
                extractedText =
                    _parser.ExtractTextFromDocx(stream);
            }
        }

        // 8. Save metadata to database
        var resumeRecord = new Resume
        {
            CandidateId = candidateId,
            FileName = file.FileName,
            FilePath = filePath,
            FileType = extension == ".pdf"
                ? "PDF"
                : "DOCX",
            FileSizeKb = (uint)Math.Ceiling(
                file.Length / 1024.0),
            ParsedRawText = extractedText,
            ParsingStatus = "COMPLETED"
        };

        _context.Resumes.Add(resumeRecord);

        await _context.SaveChangesAsync();

        // 9. Return result
        var previewLength = Math.Min(
            extractedText.Length,
            200);

        return Ok(new
        {
            Message = "Resume uploaded and parsed successfully.",
            ResumeId = resumeRecord.Id,
            FileName = resumeRecord.FileName,
            FileType = resumeRecord.FileType,
            ExtractedTextPreview =
                extractedText.Substring(0, previewLength)
                + (extractedText.Length > 200 ? "..." : "")
        });
    }
}