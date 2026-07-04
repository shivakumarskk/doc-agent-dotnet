using System.Text.RegularExpressions;
using DocAgent.Core.Interfaces;

namespace DocAgent.Infrastructure.Skills;

/// <summary>
/// Calculator skill for arithmetic operations
/// </summary>
public class CalculatorSkill : ISkill
{
    public string Name => "calculator";

    public Task<string> ExecuteAsync(string input)
    {
        var result = Add(input);
        return Task.FromResult(result);
    }

    public string Add(string input)
    {
        var matches = Regex.Matches(input, @"-?\d+(\.\d+)?");
        var nums = matches.Select(m => double.Parse(m.Value, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        if (nums.Length < 2) return "Invalid input";
        return nums.Sum().ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
