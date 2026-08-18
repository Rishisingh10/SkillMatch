namespace SkillMatch.API.Services;

public class MatchReport
{
    public decimal OverallScore { get; set; }
    public decimal SkillScore { get; set; }
    public decimal ExperienceScore { get; set; }

    public List<string> MatchedSkills { get; set; } = new();

    public List<string> MissingSkills { get; set; } = new();

    public string Explanation { get; set; } = string.Empty;
}

public class MatchingEngine
{
    public MatchReport Evaluate(
        List<string> candidateSkills,
        List<string> requiredJobSkills,
        decimal candidateExp,
        decimal minJobExp)
    {
        // Find matched skills
        var matched = requiredJobSkills
            .Where(required =>
                candidateSkills.Any(candidate =>
                    candidate.Equals(
                        required,
                        StringComparison.OrdinalIgnoreCase)))
            .ToList();

        // Find missing skills
        var missing = requiredJobSkills
            .Where(required =>
                !candidateSkills.Any(candidate =>
                    candidate.Equals(
                        required,
                        StringComparison.OrdinalIgnoreCase)))
            .ToList();

        // Calculate skill score
        decimal skillScore = requiredJobSkills.Count > 0
            ? (decimal)matched.Count / requiredJobSkills.Count * 100
            : 100;

        // Calculate experience score
        decimal experienceScore = minJobExp > 0
            ? Math.Min((candidateExp / minJobExp) * 100, 100)
            : 100;

        // Overall score: 70% skills + 30% experience
        decimal overallScore =
            (skillScore * 0.70m) +
            (experienceScore * 0.30m);

        overallScore = Math.Round(overallScore, 2);

        string explanation =
            $"Candidate matches {matched.Count}/{requiredJobSkills.Count} " +
            $"required skills and fulfills {candidateExp:0.0} of " +
            $"{minJobExp:0.0} required years of experience.";

        return new MatchReport
        {
            OverallScore = overallScore,
            SkillScore = Math.Round(skillScore, 2),
            ExperienceScore = Math.Round(experienceScore, 2),
            MatchedSkills = matched,
            MissingSkills = missing,
            Explanation = explanation
        };
    }
}