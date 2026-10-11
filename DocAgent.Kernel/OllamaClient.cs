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
        var payload = new { model, prompt, max_tokens = maxTokens };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/generate")
        {
            Content = JsonContent.Create(payload)
        };

        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        var sb = new StringBuilder();
        string? line;

        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("response", out var respProp))
                {
                    sb.Append(respProp.ValueKind == JsonValueKind.String ? respProp.GetString() : respProp.ToString());
                }
                else if (root.TryGetProperty("output", out var outProp))
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
                    sb.Append(root.ToString());
                }

                if (root.TryGetProperty("done", out var doneProp) && doneProp.ValueKind == JsonValueKind.True)
                {
                    break;
                }
            }
            catch (JsonException)
            {
                sb.Append(line);
            }
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

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            using var doc = JsonDocument.Parse(line);
            if (doc.RootElement.TryGetProperty("response", out var resp))
            {
                yield return resp.GetString() ?? string.Empty;
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
