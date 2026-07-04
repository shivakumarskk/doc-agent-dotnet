namespace DocAgent.Core.Interfaces;

/// <summary>
/// Interface for text processing
/// </summary>
public interface ITextProcessor
{
    /// <summary>
    /// Extract text from PDF file
    /// </summary>
    string ExtractTextFromPdf(string path);

    /// <summary>
    /// Split text into chunks
    /// </summary>
    IEnumerable<(string Id, string Text)> ChunkText(string text, int chunkSize = 1000, int overlap = 200);
}
