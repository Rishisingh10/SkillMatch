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
    public decimal SemanticFitScore { get; set; }
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
        decimal minJobExp,
        decimal semanticFitScore = 0m)
    {
        var jobSkillInputs = requiredJobSkills
            .Select(s => new JobSkillInput { SkillName = s, IsMandatory = true })
            .ToList();

        var candidateSkillInputs = candidateSkills
            .Select(s => new CandidateSkillInput { SkillName = s, YearsExperience = candidateExp })
            .ToList();

        return EvaluateDetailed(candidateSkillInputs, jobSkillInputs, candidateExp, minJobExp, semanticFitScore);
    }

    public MatchReport EvaluateDetailed(
        List<CandidateSkillInput> candidateSkills,
        List<JobSkillInput> requiredJobSkills,
        decimal candidateExp,
        decimal minJobExp,
        decimal semanticFitScore = 0m)
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

        // Overall score weighting: 50% skill match + 25% experience + 15% skill level proficiency + 10% AI Cosine Similarity
        decimal rawOverallScore =
            (skillScore * 0.50m) +
            (overallExpScore * 0.25m) +
            (skillProficiencyScore * 0.15m) +
            (semanticFitScore * 0.10m);

        // Penalty for missing mandatory skills: Apply 50% penalty if mandatory skills missing
        decimal finalOverallScore = hasAllMandatory
            ? rawOverallScore
            : rawOverallScore * 0.50m;

        finalOverallScore = Math.Round(finalOverallScore, 2);

        // Generate LLM-like explanation
        var expAnalysis = minJobExp > 0 
            ? (candidateExp >= minJobExp ? $"strong {candidateExp:0.0} years of experience (exceeding the {minJobExp} year requirement)" : $"only {candidateExp:0.0} years of experience (falling short of the {minJobExp} year requirement)")
            : $"solid {candidateExp:0.0} years of experience";

        var explanation = $"Based on our AI semantic analysis, this candidate shows potential but requires review. ";
        
        if (matched.Count > 0)
        {
            explanation += $"Strengths: They perfectly match technical requirements like {string.Join(", ", matched)} and bring {expAnalysis}. ";
        }
        else
        {
            explanation += $"Strengths: They bring {expAnalysis}, though they lack direct overlap with the core technical stack. ";
        }

        if (missing.Count > 0)
        {
            var missingStr = string.Join(", ", missing);
            explanation += $"Weaknesses: The most critical gap is their lack of experience with {missingStr}. ";
            if (missingMandatory.Count > 0)
            {
                explanation += $"Specifically, {string.Join(" and ", missingMandatory)} are flagged as mandatory for this position, resulting in a significant match penalty. ";
            }
        }

        explanation += "Suggestions: ";
        if (missing.Count > 0)
        {
            explanation += $"For this specific role, we highly recommend upskilling in {missing.First()}. Alternatively, given their current skill set, they might be a better fit for a slightly different role in the organization.";
        }
        else
        {
            explanation += $"They are a phenomenal fit for this role! We highly recommend moving them forward to the interview stage.";
        }

        return new MatchReport
        {
            OverallScore = finalOverallScore,
            SkillScore = Math.Round(skillScore, 2),
            ExperienceScore = Math.Round(overallExpScore, 2),
            SkillProficiencyScore = Math.Round(skillProficiencyScore, 2),
            SemanticFitScore = Math.Round(semanticFitScore, 2),
            HasAllMandatorySkills = hasAllMandatory,
            MatchedSkills = matched,
            MissingSkills = missing,
            MissingMandatorySkills = missingMandatory,
            Explanation = explanation
        };
    }
}