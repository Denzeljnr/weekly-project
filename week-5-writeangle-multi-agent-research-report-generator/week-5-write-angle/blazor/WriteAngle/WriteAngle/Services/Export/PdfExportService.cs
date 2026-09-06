using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WriteAngle.Interfaces;
using WriteAngle.Models;

namespace WriteAngle.Services.Export;

public class PdfExportService : IPdfExportService
{
    public byte[] Export(Report report)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.Header().Text(report.Topic).FontSize(20).Bold();
                page.Content().Column(col =>
                {
                    RenderMarkdown(col, report.FinalText);
                });
                page.Footer().AlignCenter().Text($"Generated {report.CreatedAt:d} — {report.RevisionCount} revision(s)");
            });
        }).GeneratePdf();
    }

    // Minimal Markdown-to-QuestPDF mapping: handles #, ##, ### headings and plain paragraphs.
    // Not a full Markdown parser — just enough to stop literal "#" characters showing up as text.
    private void RenderMarkdown(ColumnDescriptor col, string markdown)
    {
        var lines = markdown.Split('\n');

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();

            if (string.IsNullOrWhiteSpace(line))
            {
                col.Item().PaddingTop(4);
                continue;
            }

            if (line.StartsWith("### "))
            {
                col.Item().PaddingTop(10).Text(line[4..]).FontSize(13).Bold();
            }
            else if (line.StartsWith("## "))
            {
                col.Item().PaddingTop(12).Text(line[3..]).FontSize(15).Bold();
            }
            else if (line.StartsWith("# "))
            {
                col.Item().PaddingTop(14).Text(line[2..]).FontSize(18).Bold();
            }
            else
            {
                col.Item().PaddingTop(4).Text(line).FontSize(11).LineHeight(1.4f);
            }
        }
    }
}