using SemanticSearch.Models;

namespace SemanticSearch.Interfaces;

public interface IPdfTextExtractor
{
    string ExtractText(Stream pdfStream, IProgress<PdfExtractionProgress>? progress = null);
}