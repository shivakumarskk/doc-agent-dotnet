using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using DocAgent.Core.Interfaces;

namespace DocAgent.Infrastructure.Services;

/// <summary>
/// Ollama LLM client implementation
/// </summary>
public class OllamaClient : ILlmClient
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

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        var sb = new StringBuilder();
        bool sawAnyJson = false;

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                sawAnyJson = true;
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;

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
                        sb.Append(root.ToString());
                    }
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
