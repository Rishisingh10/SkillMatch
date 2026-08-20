namespace SkillMatch.API.Services;

public class CandidateSkillInput
{
    public string SkillName { get; set; } = string.Empty;
    public decimal YearsExperience { get; set; }
}

public class JobSkillInput
{
    public string SkillName { get; set; } = string.Empty;
    public bool IsMandatory { get; set; } = true;
}

public class MatchReport
{
    public decimal OverallScore { get; set; }
    public decimal SkillScore { get; set; }
    public decimal ExperienceScore { get; set; }
    public decimal SkillProficiencyScore { get; set; }
    public bool HasAllMandatorySkills { get; set; } = true;
    public List<string> MatchedSkills { get; set; } = new();
    public List<string> MissingSkills { get; set; } = new();
    public List<string> MissingMandatorySkills { get; set; } = new();
    public string Explanation { get; set; } = string.Empty;
}

public class MatchingEngine
{
    private readonly SkillAliasNormalizer _normalizer;

    public MatchingEngine(SkillAliasNormalizer normalizer)
    {
        _normalizer = normalizer;
    }

    public MatchingEngine()
    {
        _normalizer = new SkillAliasNormalizer();
    }

    public MatchReport Evaluate(
        List<string> candidateSkills,
        List<string> requiredJobSkills,
        decimal candidateExp,
        decimal minJobExp)
    {
        var jobSkillInputs = requiredJobSkills
            .Select(s => new JobSkillInput { SkillName = s, IsMandatory = true })
            .ToList();

        var candidateSkillInputs = candidateSkills
            .Select(s => new CandidateSkillInput { SkillName = s, YearsExperience = candidateExp })
            .ToList();

        return EvaluateDetailed(candidateSkillInputs, jobSkillInputs, candidateExp, minJobExp);
    }

    public MatchReport EvaluateDetailed(
        List<CandidateSkillInput> candidateSkills,
        List<JobSkillInput> requiredJobSkills,
        decimal candidateExp,
        decimal minJobExp)
    {
        var matched = new List<string>();
        var missing = new List<string>();
        var missingMandatory = new List<string>();

        decimal totalSkillExpScore = 0;
        int matchedCount = 0;

        foreach (var jobSkill in requiredJobSkills)
        {
            var match = candidateSkills.FirstOrDefault(cs =>
                _normalizer.AreEquivalent(cs.SkillName, jobSkill.SkillName));

            if (match != null)
            {
                matched.Add(jobSkill.SkillName);
                matchedCount++;

                // Calculate skill experience ratio vs minJobExp
                decimal skillExpRatio = minJobExp > 0
                    ? Math.Min((match.YearsExperience / minJobExp) * 100, 100)
                    : 100;
                totalSkillExpScore += skillExpRatio;
            }
            else
            {
                missing.Add(jobSkill.SkillName);
                if (jobSkill.IsMandatory)
                {
                    missingMandatory.Add(jobSkill.SkillName);
                }
            }
        }

        bool hasAllMandatory = missingMandatory.Count == 0;

        // Raw skill match score
        decimal skillScore = requiredJobSkills.Count > 0
            ? (decimal)matchedCount / requiredJobSkills.Count * 100
            : 100;

        // Overall experience score vs job minimum
        decimal overallExpScore = minJobExp > 0
            ? Math.Min((candidateExp / minJobExp) * 100, 100)
            : 100;

        // Average proficiency score for matched skills
        decimal skillProficiencyScore = matchedCount > 0
            ? totalSkillExpScore / matchedCount
            : 0;

        // Base overall score: 60% skill match + 25% overall experience + 15% skill proficiency
        decimal rawOverallScore =
            (skillScore * 0.60m) +
            (overallExpScore * 0.25m) +
            (skillProficiencyScore * 0.15m);

        // Penalty for missing mandatory skills: Apply 50% penalty if mandatory skills missing
        decimal finalOverallScore = hasAllMandatory
            ? rawOverallScore
            : rawOverallScore * 0.50m;

        finalOverallScore = Math.Round(finalOverallScore, 2);

        string explanation =
            $"Candidate matched {matchedCount}/{requiredJobSkills.Count} required skills " +
            $"(Normalized alias matching enabled). " +
            $"Fulfills {candidateExp:0.0} of {minJobExp:0.0} required experience years. ";

        if (!hasAllMandatory)
        {
            explanation += $"[WARNING: Lacks {missingMandatory.Count} mandatory skill(s): {string.Join(", ", missingMandatory)}. 50% score penalty applied.]";
        }
        else
        {
            explanation += "Candidate meets all mandatory skill prerequisites.";
        }

        return new MatchReport
        {
            OverallScore = finalOverallScore,
            SkillScore = Math.Round(skillScore, 2),
            ExperienceScore = Math.Round(overallExpScore, 2),
            SkillProficiencyScore = Math.Round(skillProficiencyScore, 2),
            HasAllMandatorySkills = hasAllMandatory,
            MatchedSkills = matched,
            MissingSkills = missing,
            MissingMandatorySkills = missingMandatory,
            Explanation = explanation
        };
    }
}