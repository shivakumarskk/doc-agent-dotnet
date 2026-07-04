namespace DocAgent.Core.DTOs;

/// <summary>
/// DTO for generate text requests
/// </summary>
public class GenerateRequestDto
{
    public string Model { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public int MaxTokens { get; set; } = 512;
}
