using DocAgent.Core.Models;

namespace DocAgent.Core.Interfaces;

/// <summary>
/// Interface for LLM skill base
/// </summary>
public interface ISkill
{
    string Name { get; }
    Task<string> ExecuteAsync(string input);
}

/// <summary>
/// Interface for skill service
/// </summary>
public interface ISkillService
{
    Task<string> InvokeAsync(string skillName, string input);
}
