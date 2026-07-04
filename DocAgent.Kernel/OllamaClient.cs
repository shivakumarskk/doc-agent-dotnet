using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public class OllamaClient
{
    private readonly HttpClient _http;
    public OllamaClient(HttpClient http) => _http = http;

    public async Task<string> GenerateAsync(string model, string prompt, int maxTokens = 512, CancellationToken cancellationToken = default)
    {
        var payload = new { model = model, prompt = prompt, max_tokens = maxTokens };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/generate")
        {
            Content = JsonContent.Create(payload)
        };

        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        // If server streams NDJSON, read line-by-line from the response stream
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        var sb = new StringBuilder();
        bool sawAnyJson = false;

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Try parse the line as JSON; if it fails, append raw line
            try
            {
                sawAnyJson = true;
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;

                // If the object contains "response" fields (Ollama streaming shape), append them
                if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("response", out var respProp))
                {
                    if (respProp.ValueKind == JsonValueKind.String)
                    {
                        sb.Append(respProp.GetString());
                    }
                    else
                    {
                        sb.Append(respProp.ToString());
                    }
                }
                else
                {
                    // Fallback: if object has "output" or "choices", try to extract text
                    if (root.TryGetProperty("output", out var outProp))
                    {
                        sb.Append(outProp.ToString());
                    }
                    else if (root.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in choices.EnumerateArray())
                        {
                            if (item.TryGetProperty("text", out var t) && t.ValueKind == JsonValueKind.String)
                                sb.Append(t.GetString());
                            else
                                sb.Append(item.ToString());
                        }
                    }
                    else
                    {
                        // Unknown object shape: append its string form
                        sb.Append(root.ToString());
                    }
                }

                // Optional: if the chunk contains "done": true, we can break early
                if (root.TryGetProperty("done", out var doneProp) && doneProp.ValueKind == JsonValueKind.True)
                {
                    break;
                }
            }
            catch (JsonException)
            {
                // Not JSON — append raw line (some servers may send plain text)
                sb.Append(line);
            }
        }

        // If we never saw JSON lines, fall back to reading the whole body as text (non-streaming)
        if (!sawAnyJson)
        {
            // Rewind not possible; we already read stream. Instead, try to read content as string directly.
            // (This branch is unlikely because we already consumed the stream; keep for completeness.)
            // If you expect non-streaming responses, consider using ReadAsStringAsync earlier.
        }

        return sb.ToString().Trim();
    }

    public async IAsyncEnumerable<string> StreamGenerateAsync(
    string model,
    string prompt,
    int maxTokens = 512,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var payload = new { model, prompt, stream = true, options = new { num_predict = maxTokens } };
        var response = await _http.PostAsJsonAsync("/api/generate", payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;

            var doc = JsonDocument.Parse(line);
            if (doc.RootElement.TryGetProperty("response", out var resp))
            {
                yield return resp.GetString() ?? "";
            }
        }
    }

    public async Task<string> StreamAndCollectAsync(
    string model,
    string prompt,
    int maxTokens,
    HttpResponse httpResponse,
    CancellationToken cancellationToken)
    {
        var buffer = new StringBuilder();
        httpResponse.ContentType = "text/event-stream";

        await foreach (var token in StreamGenerateAsync(model, prompt, maxTokens, cancellationToken))
        {
            buffer.Append(token);
            await httpResponse.WriteAsync($"data: {token}\n\n", cancellationToken);
            await httpResponse.Body.FlushAsync(cancellationToken);
        }

        return buffer.ToString();
    }


}
