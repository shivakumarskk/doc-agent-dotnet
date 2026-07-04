using DocAgent.Core.Interfaces;
using UglyToad.PdfPig;

namespace DocAgent.Infrastructure.Services;

/// <summary>
/// Text processing service for PDF extraction and chunking
/// </summary>
public class TextProcessor : ITextProcessor
{
    public string ExtractTextFromPdf(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"File not found: {path}");

        using var doc = PdfDocument.Open(path);
        return string.Join("\n", doc.GetPages().Select(p => p.Text));
    }

    public IEnumerable<(string Id, string Text)> ChunkText(string text, int chunkSize = 1000, int overlap = 200)
    {
        var chunks = new List<(string, string)>();
        int pos = 0;
        int id = 0;

        while (pos < text.Length)
        {
            int len = Math.Min(chunkSize, text.Length - pos);
            var chunk = text.Substring(pos, len);
            chunks.Add(($"chunk-{id++}", chunk));

            int step = len > overlap ? (len - overlap) : len;
            pos += step;
        }

        return chunks;
    }
}
