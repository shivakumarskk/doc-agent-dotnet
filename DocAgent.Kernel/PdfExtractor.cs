using UglyToad.PdfPig;

public static class PdfExtractor
{
    public static string ExtractText(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException(path);

        using var doc = PdfDocument.Open(path);
        return string.Join("\n", doc.GetPages().Select(p => p.Text));
    }
}
