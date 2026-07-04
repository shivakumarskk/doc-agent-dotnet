using System;
using System.Collections.Generic;

public static class TextChunker
{
    public static IEnumerable<(string Id, string Text)> ChunkText(string text, int chunkSize = 1000, int overlap = 200)
    {
        var chunks = new List<(string, string)>();
        int pos = 0; int id = 0;

        while (pos < text.Length)
        {
            int len = Math.Min(chunkSize, text.Length - pos);
            var chunk = text.Substring(pos, len);
            chunks.Add(($"chunk-{id++}", chunk));

            // Advance position safely: if overlap >= len, move by len (no overlap possible)
            int step = len > overlap ? (len - overlap) : len;
            pos += step;
        }

        return chunks;
    }
}
