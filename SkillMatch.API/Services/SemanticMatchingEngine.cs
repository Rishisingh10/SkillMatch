using SkillMatch.API.Models;

namespace SkillMatch.API.Services;

public class SemanticMatchingEngine
{
    private readonly AIService _aiService;

    public SemanticMatchingEngine(AIService aiService)
    {
        _aiService = aiService;
    }

    public async Task<decimal> ComputeSemanticFitScoreAsync(CandidateProfile candidate, Job job, string? resumeText = null)
    {
        if (candidate == null || job == null)
            return 0m;

        // Build composite string for Job
        var jobSkillsText = string.Join(", ", job.JobSkills.Select(js => js.Skill?.Name ?? ""));
        var jobText = $"Job Title: {job.Title}. Description: {job.Description}. Required Skills: {jobSkillsText}. Min Experience: {job.MinExperienceYears} years.";

        // Build composite string for Candidate Profile
        var candidateSkillsText = string.Join(", ", candidate.CandidateSkills.Select(cs => cs.Skill?.Name ?? ""));
        var candidateProfileText = $"Headline: {candidate.Headline}. Bio: {candidate.Bio}. Experience: {candidate.TotalExperienceYears} years. Education: {candidate.EducationLevel}. Skills: {candidateSkillsText}.";

        var fullCandidateText = string.IsNullOrWhiteSpace(resumeText)
            ? candidateProfileText
            : $"{candidateProfileText} Resume: {(resumeText.Length > 2000 ? resumeText.Substring(0, 2000) : resumeText)}";

        // Compute Cosine Similarity between Job Text Vector and Candidate Vector
        return await _aiService.ComputeCosineSimilarityAsync(jobText, fullCandidateText);
    }
}
