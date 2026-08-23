using System.Text;
using UglyToad.PdfPig;
using SemanticSearch.Interfaces;
using SemanticSearch.Models;

namespace SemanticSearch.Services;

public class PdfTextExtractor : IPdfTextExtractor
{
    public string ExtractText(Stream pdfStream, IProgress<PdfExtractionProgress>? progress = null)
    {
        using var document = PdfDocument.Open(pdfStream);
        var sb = new StringBuilder();
        var totalPages = document.NumberOfPages;
        int processed = 0;

        foreach (var page in document.GetPages())
        {
            sb.AppendLine(page.Text);
            processed++;

            // Report every 5 pages (not every single one) so a 500-page PDF
            // doesn't flood the UI with hundreds of re-renders per second.
            if (processed % 5 == 0 || processed == totalPages)
                progress?.Report(new PdfExtractionProgress(processed, totalPages));
        }

        return sb.ToString();
    }
}