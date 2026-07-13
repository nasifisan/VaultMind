using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using UglyToad.PdfPig;
using VaultMind.API.Interfaces;

namespace VaultMind.API.Services;

public class DocumentParserService : IDocumentParserService
{
    private readonly ILogger<DocumentParserService> _logger;

    public DocumentParserService(ILogger<DocumentParserService> logger)
    {
        _logger = logger;
    }

    public async Task<string> ExtractTextAsync(Stream fileStream, string contentType, string fileName)
    {
        if (fileStream == null)
        {
            _logger.LogWarning("Null file stream provided for document: {FileName}", fileName);
            return string.Empty;
        }

        // Check length only if the stream supports it (MemoryStream does, network streams may not)
        try
        {
            if (fileStream.CanSeek && fileStream.Length == 0)
            {
                _logger.LogWarning("Empty file stream provided for document: {FileName}", fileName);
                return string.Empty;
            }
        }
        catch (NotSupportedException)
        {
            // Stream doesn't support Length — that's fine, proceed with reading
        }

        try
        {
            // Normalize content type
            var normalizedContentType = contentType.Trim().ToLowerInvariant();

            switch (normalizedContentType)
            {
                case "application/pdf":
                    return await ExtractTextFromPdfAsync(fileStream);

                case "application/vnd.openxmlformats-officedocument.wordprocessingml.document":
                    return await ExtractTextFromDocxAsync(fileStream);

                case "text/plain":
                case "text/markdown":
                case "text/html":
                case "application/json":
                    return await ExtractTextFromPlainStreamAsync(fileStream);

                default:
                    // If content type is unrecognized but extension suggests plain text
                    var extension = Path.GetExtension(fileName).ToLowerInvariant();
                    if (extension == ".txt" || extension == ".md" || extension == ".json" || extension == ".html")
                    {
                        return await ExtractTextFromPlainStreamAsync(fileStream);
                    }

                    _logger.LogWarning("Unsupported content type '{ContentType}' for file '{FileName}'", contentType, fileName);
                    return string.Empty;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while extracting text from file '{FileName}' with Content-Type '{ContentType}'", fileName, contentType);
            return string.Empty;
        }
    }

    public bool CanParse(string contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType)) return false;

        var normalized = contentType.Trim().ToLowerInvariant();
        return normalized == "application/pdf" ||
               normalized == "application/vnd.openxmlformats-officedocument.wordprocessingml.document" ||
               normalized == "text/plain" ||
               normalized == "text/markdown" ||
               normalized == "text/html" ||
               normalized == "application/json";
    }

    private async Task<string> ExtractTextFromPdfAsync(Stream fileStream)
    {
        // Copy to MemoryStream to ensure seekability required by PdfPig
        using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        using var pdfDocument = PdfDocument.Open(memoryStream);
        var textBuilder = new StringBuilder();

        foreach (var page in pdfDocument.GetPages())
        {
            if (page.Text != null)
            {
                textBuilder.AppendLine(page.Text);
            }
        }

        return textBuilder.ToString();
    }

    private async Task<string> ExtractTextFromDocxAsync(Stream fileStream)
    {
        // Copy to MemoryStream to ensure seekability required by OpenXml Packaging
        using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        using var wordDocument = WordprocessingDocument.Open(memoryStream, false);
        var body = wordDocument.MainDocumentPart?.Document.Body;
        if (body == null)
        {
            return string.Empty;
        }

        var textBuilder = new StringBuilder();
        foreach (var paragraph in body.Descendants<Paragraph>())
        {
            var text = paragraph.InnerText;
            if (!string.IsNullOrEmpty(text))
            {
                textBuilder.AppendLine(text);
            }
        }

        return textBuilder.ToString();
    }

    private async Task<string> ExtractTextFromPlainStreamAsync(Stream fileStream)
    {
        using var reader = new StreamReader(fileStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }
}
