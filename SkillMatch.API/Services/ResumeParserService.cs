using System.Text;
using DocumentFormat.OpenXml.Packaging;
using UglyToad.PdfPig;

namespace SkillMatch.API.Services;

public class ResumeParserService
{
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
}