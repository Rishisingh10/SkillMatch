using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using UglyToad.PdfPig;

namespace SkillMatch.API.Services;

public class ResumeParserService
{
    private readonly SkillAliasNormalizer _normalizer;

    public ResumeParserService(SkillAliasNormalizer normalizer)
    {
        _normalizer = normalizer;
    }

    public ResumeParserService()
    {
        _normalizer = new SkillAliasNormalizer();
    }

    public bool ValidatePdfHeader(Stream stream)
    {
        if (stream.Length < 4) return false;
        long originalPosition = stream.Position;
        byte[] buffer = new byte[4];
        stream.Read(buffer, 0, 4);
        stream.Position = originalPosition;

        // PDF magic header %PDF (0x25, 0x50, 0x44, 0x46)
        return buffer[0] == 0x25 && buffer[1] == 0x50 && buffer[2] == 0x44 && buffer[3] == 0x46;
    }

    public bool ValidateDocxHeader(Stream stream)
    {
        if (stream.Length < 4) return false;
        long originalPosition = stream.Position;
        byte[] buffer = new byte[4];
        stream.Read(buffer, 0, 4);
        stream.Position = originalPosition;

        // Zip archive / DOCX magic header PK\x03\x04 (0x50, 0x4B, 0x03, 0x04)
        return buffer[0] == 0x50 && buffer[1] == 0x4B && buffer[2] == 0x03 && buffer[3] == 0x04;
    }

    public string ExtractTextFromPdf(Stream pdfStream)
    {
        var textBuilder = new StringBuilder();

        using (var document = PdfDocument.Open(pdfStream))
        {
            foreach (var page in document.GetPages())
            {
                textBuilder.AppendLine(page.Text);
            }
        }

        return textBuilder.ToString();
    }

    public string ExtractTextFromDocx(Stream docxStream)
    {
        using (var wordDoc = WordprocessingDocument.Open(docxStream, false))
        {
            return wordDoc.MainDocumentPart?
                              .Document?
                              .Body?
                              .InnerText
                           ?? string.Empty;
        }
    }

    public List<string> ExtractSkillsFromText(string rawText, IEnumerable<string> knownSkills)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            return new List<string>();

        var extractedSkills = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var skill in knownSkills)
        {
            var normalizedSkill = _normalizer.Normalize(skill);

            // Escape special regex characters in skill name (e.g., C#, .NET)
            string escapedSkill = Regex.Escape(skill);
            string pattern = $@"(?:\b|\s|^){escapedSkill}(?:\b|\s|$)";

            if (Regex.IsMatch(rawText, pattern, RegexOptions.IgnoreCase))
            {
                extractedSkills.Add(normalizedSkill);
            }
        }

        return extractedSkills.ToList();
    }
}