using DocAgent.Core.Interfaces;
using DocAgent.Infrastructure.Skills;

namespace DocAgent.Infrastructure.Services;

/// <summary>
/// Skill service for invoking registered skills
/// </summary>
public class SkillService : ISkillService
{
    private readonly Dictionary<string, ISkill> _skills;

    public SkillService(CalculatorSkill calculatorSkill)
    {
        _skills = new Dictionary<string, ISkill>
        {
            { "calculator", calculatorSkill }
        };
    }

    public async Task<string> InvokeAsync(string skillName, string input)
    {
        if (!_skills.TryGetValue(skillName.ToLower(), out var skill))
        {
            throw new InvalidOperationException($"Skill '{skillName}' not found");
        }

        return await skill.ExecuteAsync(input);
    }
}
