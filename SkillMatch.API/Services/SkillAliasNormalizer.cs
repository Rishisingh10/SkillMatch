using System.Text.RegularExpressions;

namespace SkillMatch.API.Services;

public class SkillAliasNormalizer
{
    private static readonly Dictionary<string, string> AliasMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "react", "React" },
        { "reactjs", "React" },
        { "react.js", "React" },
        { "c#", "C#" },
        { "c sharp", "C#" },
        { "csharp", "C#" },
        { ".net", ".NET" },
        { "dotnet", ".NET" },
        { ".net core", ".NET" },
        { "asp.net", ".NET" },
        { "asp.net core", ".NET" },
        { "node", "Node.js" },
        { "nodejs", "Node.js" },
        { "node.js", "Node.js" },
        { "js", "JavaScript" },
        { "javascript", "JavaScript" },
        { "ts", "TypeScript" },
        { "typescript", "TypeScript" },
        { "python", "Python" },
        { "python3", "Python" },
        { "postgres", "PostgreSQL" },
        { "postgresql", "PostgreSQL" },
        { "postgres sql", "PostgreSQL" },
        { "mysql", "MySQL" },
        { "mongo", "MongoDB" },
        { "mongodb", "MongoDB" },
        { "docker", "Docker" },
        { "containerization", "Docker" },
        { "k8s", "Kubernetes" },
        { "kubernetes", "Kubernetes" },
        { "aws", "AWS" },
        { "amazon web services", "AWS" },
        { "azure", "Azure" },
        { "microsoft azure", "Azure" },
        { "gcp", "GCP" },
        { "google cloud", "GCP" },
        { "google cloud platform", "GCP" },
        { "html", "HTML" },
        { "html5", "HTML" },
        { "css", "CSS" },
        { "css3", "CSS" },
        { "git", "Git" },
        { "github", "Git" },
        { "sql", "SQL" }
    };

    public string Normalize(string skillName)
    {
        if (string.IsNullOrWhiteSpace(skillName))
            return string.Empty;

        var trimmed = skillName.Trim();
        return AliasMap.TryGetValue(trimmed, out var canonical) ? canonical : trimmed;
    }

    public bool AreEquivalent(string skill1, string skill2)
    {
        if (string.IsNullOrWhiteSpace(skill1) || string.IsNullOrWhiteSpace(skill2))
            return false;

        return string.Equals(Normalize(skill1), Normalize(skill2), StringComparison.OrdinalIgnoreCase);
    }
}
