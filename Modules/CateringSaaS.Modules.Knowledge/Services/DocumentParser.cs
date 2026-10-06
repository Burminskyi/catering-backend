using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using UglyToad.PdfPig;

namespace CateringSaaS.Modules.Knowledge.Services;

public sealed class DocumentParser : IDocumentParser
{
    public bool CanParse(string fileName, string? contentType)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (ext is ".pdf" or ".docx")
        {
            return true;
        }

        var type = (contentType ?? string.Empty).ToLowerInvariant();
        return type.Contains("pdf", StringComparison.Ordinal)
               || type.Contains("wordprocessingml", StringComparison.Ordinal)
               || type.Contains("msword", StringComparison.Ordinal);
    }

    public Task<string> ExtractTextAsync(
        Stream stream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name is required.", nameof(fileName));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var text = ext switch
        {
            ".pdf" => ExtractPdf(stream),
            ".docx" => ExtractDocx(stream),
            _ => throw new NotSupportedException(
                $"Unsupported file type '{ext}'. Only PDF and DOCX are supported.")
        };

        return Task.FromResult(text);
    }

    private static string ExtractPdf(Stream stream)
    {
        using var memory = CopyToMemory(stream);
        using var document = PdfDocument.Open(memory);
        var builder = new StringBuilder();

        foreach (var page in document.GetPages())
        {
            if (!string.IsNullOrWhiteSpace(page.Text))
            {
                builder.AppendLine(page.Text.Trim());
                builder.AppendLine();
            }
        }

        return NormalizeWhitespace(builder.ToString());
    }

    private static string ExtractDocx(Stream stream)
    {
        using var memory = CopyToMemory(stream);
        using var document = WordprocessingDocument.Open(memory, false);
        var body = document.MainDocumentPart?.Document?.Body;
        if (body is null)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        foreach (var paragraph in body.Descendants<Paragraph>())
        {
            var text = string.Concat(paragraph.Descendants<Text>().Select(t => t.Text));
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            builder.AppendLine(text.Trim());
        }

        return NormalizeWhitespace(builder.ToString());
    }

    private static MemoryStream CopyToMemory(Stream stream)
    {
        var memory = new MemoryStream();
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        stream.CopyTo(memory);
        memory.Position = 0;
        return memory;
    }

    private static string NormalizeWhitespace(string text)
    {
        var lines = text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.Length > 0);

        return string.Join("\n", lines).Trim();
    }
}
