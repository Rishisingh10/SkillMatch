using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SkillMatch.API.Services;

public class ExtractedSkillItem
{
    public string SkillName { get; set; } = string.Empty;
    public decimal YearsExperience { get; set; } = 1.0m;
}

public class ParsedResumeResult
{
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Headline { get; set; } = string.Empty;
    public string EducationLevel { get; set; } = string.Empty;
    public decimal TotalExperienceYears { get; set; }
    public List<ExtractedSkillItem> ExtractedSkills { get; set; } = new();
}

public class AIService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly SkillAliasNormalizer _normalizer;

    public AIService(HttpClient httpClient, IConfiguration configuration, SkillAliasNormalizer normalizer)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _normalizer = normalizer;
    }

    public async Task<ParsedResumeResult> ExtractStructuredResumeDataAsync(string rawText)
    {
        var apiKey = _configuration["AI:ApiKey"];
        var provider = _configuration["AI:Provider"] ?? "BuiltIn";

        if (!string.IsNullOrWhiteSpace(apiKey) && provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var llmResult = await CallOpenAiForStructuredResumeAsync(rawText, apiKey);
                if (llmResult != null) return llmResult;
            }
            catch
            {
                // Fallback to built-in NLP parser on any network/API failure
            }
        }

        return FallbackNlpResumeExtraction(rawText);
    }

    public async Task<decimal> ComputeCosineSimilarityAsync(string text1, string text2)
    {
        if (string.IsNullOrWhiteSpace(text1) || string.IsNullOrWhiteSpace(text2))
            return 0m;

        var apiKey = _configuration["AI:ApiKey"];
        var provider = _configuration["AI:Provider"] ?? "BuiltIn";

        if (!string.IsNullOrWhiteSpace(apiKey) && provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var vec1 = await GetTextEmbeddingAsync(text1, apiKey);
                var vec2 = await GetTextEmbeddingAsync(text2, apiKey);

                if (vec1 != null && vec2 != null && vec1.Length == vec2.Length)
                {
                    double sim = CalculateVectorCosineSimilarity(vec1, vec2);
                    return Math.Round((decimal)(sim * 100.0), 2);
                }
            }
            catch
            {
                // Fallback to TF-IDF vector cosine similarity
            }
        }

        // Built-in TF-IDF Vector Cosine Similarity
        double tfIdfSim = CalculateTfIdfCosineSimilarity(text1, text2);
        return Math.Round((decimal)(tfIdfSim * 100.0), 2);
    }

    private async Task<ParsedResumeResult?> CallOpenAiForStructuredResumeAsync(string rawText, string apiKey)
    {
        var model = _configuration["AI:Model"] ?? "gpt-4o-mini";
        var endpoint = _configuration["AI:Endpoint"] ?? "https://api.openai.com/v1/chat/completions";

        var prompt = @"Extract structured candidate resume information in strictly valid JSON format matching this schema:
{
  ""fullName"": ""string"",
  ""phone"": ""string"",
  ""location"": ""string"",
  ""headline"": ""string"",
  ""educationLevel"": ""string"",
  ""totalExperienceYears"": number,
  ""extractedSkills"": [
    { ""skillName"": ""string"", ""yearsExperience"": number }
  ]
}
Resume Text:
" + (rawText.Length > 4000 ? rawText.Substring(0, 4000) : rawText);

        var requestBody = new
        {
            model = model,
            messages = new[]
            {
                new { role = "system", content = "You are an expert AI HR assistant extracting structured JSON data from resumes." },
                new { role = "user", content = prompt }
            },
            response_format = new { type = "json_object" },
            temperature = 0.1
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;

        var jsonString = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(jsonString);
        var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

        if (string.IsNullOrWhiteSpace(content)) return null;

        using var resultDoc = JsonDocument.Parse(content);
        var root = resultDoc.RootElement;

        var result = new ParsedResumeResult
        {
            FullName = root.TryGetProperty("fullName", out var fn) ? fn.GetString() ?? "" : "",
            Phone = root.TryGetProperty("phone", out var ph) ? ph.GetString() ?? "" : "",
            Location = root.TryGetProperty("location", out var loc) ? loc.GetString() ?? "" : "",
            Headline = root.TryGetProperty("headline", out var hl) ? hl.GetString() ?? "" : "",
            EducationLevel = root.TryGetProperty("educationLevel", out var ed) ? ed.GetString() ?? "" : "",
            TotalExperienceYears = root.TryGetProperty("totalExperienceYears", out var exp) && exp.TryGetDecimal(out var expVal) ? expVal : 0m
        };

        if (root.TryGetProperty("extractedSkills", out var skillsArr) && skillsArr.ValueKind == JsonValueKind.Array)
        {
            foreach (var sk in skillsArr.EnumerateArray())
            {
                string sName = sk.TryGetProperty("skillName", out var sn) ? sn.GetString() ?? "" : "";
                decimal sExp = sk.TryGetProperty("yearsExperience", out var se) && se.TryGetDecimal(out var seVal) ? seVal : 1m;
                if (!string.IsNullOrWhiteSpace(sName))
                {
                    result.ExtractedSkills.Add(new ExtractedSkillItem
                    {
                        SkillName = _normalizer.Normalize(sName),
                        YearsExperience = sExp
                    });
                }
            }
        }

        return result;
    }

    private async Task<float[]?> GetTextEmbeddingAsync(string text, string apiKey)
    {
        var model = _configuration["AI:EmbeddingModel"] ?? "text-embedding-3-small";
        var endpoint = _configuration["AI:Endpoint"]?.Replace("chat/completions", "embeddings") ?? "https://api.openai.com/v1/embeddings";

        var requestBody = new
        {
            model = model,
            input = text.Length > 2000 ? text.Substring(0, 2000) : text
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;

        var jsonString = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(jsonString);
        var dataArr = doc.RootElement.GetProperty("data")[0].GetProperty("embedding");

        var floatList = new List<float>();
        foreach (var elem in dataArr.EnumerateArray())
        {
            floatList.Add(elem.GetSingle());
        }

        return floatList.ToArray();
    }

    private ParsedResumeResult FallbackNlpResumeExtraction(string text)
    {
        var result = new ParsedResumeResult();

        // 1. Phone extraction regex
        var phoneMatch = Regex.Match(text, @"(?:\+?\d{1,3}[-.\s]?)?\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4}");
        if (phoneMatch.Success) result.Phone = phoneMatch.Value.Trim();

        // 2. Experience years extraction
        var expMatch = Regex.Match(text, @"(\d+(?:\.\d+)?)\s*(?:\+|\s*years?|\s*yrs?)\s+(?:of\s+)?experience", RegexOptions.IgnoreCase);
        if (expMatch.Success && decimal.TryParse(expMatch.Groups[1].Value, out var years))
        {
            result.TotalExperienceYears = years;
        }
        else
        {
            result.TotalExperienceYears = 2.0m; // Default reasonable baseline
        }

        // 3. Education extraction
        if (Regex.IsMatch(text, @"master|m\.s\.|m\.tech|mca|mba", RegexOptions.IgnoreCase))
            result.EducationLevel = "Master's Degree";
        else if (Regex.IsMatch(text, @"bachelor|b\.s\.|b\.tech|bca|b\.e\.", RegexOptions.IgnoreCase))
            result.EducationLevel = "Bachelor's Degree";
        else if (Regex.IsMatch(text, @"phd|doctorate", RegexOptions.IgnoreCase))
            result.EducationLevel = "Ph.D.";

        // 4. Headline & Location extraction heuristics
        var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length > 0) result.FullName = lines[0].Trim();
        if (lines.Length > 1) result.Headline = lines[1].Trim();

        // 5. Taxonomy skill detection
        var commonSkills = new[]
        {
            "C#", ".NET", "ASP.NET", "Java", "Python", "JavaScript", "TypeScript", "React",
            "Angular", "Vue", "Node.js", "SQL", "MySQL", "PostgreSQL", "MongoDB", "Docker",
            "Kubernetes", "AWS", "Azure", "GCP", "Git", "REST API", "GraphQL", "Microservices"
        };

        foreach (var skill in commonSkills)
        {
            string escaped = Regex.Escape(skill);
            if (Regex.IsMatch(text, $@"(?:\b|\s|^){escaped}(?:\b|\s|$)", RegexOptions.IgnoreCase))
            {
                result.ExtractedSkills.Add(new ExtractedSkillItem
                {
                    SkillName = _normalizer.Normalize(skill),
                    YearsExperience = result.TotalExperienceYears
                });
            }
        }

        return result;
    }

    private double CalculateVectorCosineSimilarity(float[] vec1, float[] vec2)
    {
        double dotProduct = 0.0;
        double normA = 0.0;
        double normB = 0.0;

        for (int i = 0; i < vec1.Length; i++)
        {
            dotProduct += vec1[i] * vec2[i];
            normA += vec1[i] * vec1[i];
            normB += vec2[i] * vec2[i];
        }

        if (normA == 0.0 || normB == 0.0) return 0.0;
        return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }

    private double CalculateTfIdfCosineSimilarity(string text1, string text2)
    {
        var words1 = TokenizeAndStem(text1);
        var words2 = TokenizeAndStem(text2);

        var vocabulary = words1.Concat(words2).Distinct().ToList();
        if (vocabulary.Count == 0) return 0.0;

        var freq1 = vocabulary.ToDictionary(w => w, w => words1.Count(x => x == w));
        var freq2 = vocabulary.ToDictionary(w => w, w => words2.Count(x => x == w));

        double dotProduct = 0.0;
        double norm1 = 0.0;
        double norm2 = 0.0;

        foreach (var word in vocabulary)
        {
            double v1 = freq1[word];
            double v2 = freq2[word];

            dotProduct += v1 * v2;
            norm1 += v1 * v1;
            norm2 += v2 * v2;
        }

        if (norm1 == 0.0 || norm2 == 0.0) return 0.0;
        return dotProduct / (Math.Sqrt(norm1) * Math.Sqrt(norm2));
    }

    private List<string> TokenizeAndStem(string text)
    {
        var clean = Regex.Replace(text.ToLowerInvariant(), @"[^\w\s]", " ");
        var stopWords = new HashSet<string> { "a", "an", "the", "in", "on", "at", "to", "for", "of", "and", "or", "is", "are", "with", "this", "that" };

        return clean.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(w => w.Length > 2 && !stopWords.Contains(w))
                    .ToList();
    }
}
