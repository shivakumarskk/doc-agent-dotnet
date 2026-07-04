using System.Linq;
using System.Text.RegularExpressions;

public class CalculatorSkill
{
    public string Add(string input)
    {
        var matches = Regex.Matches(input, @"-?\d+(\.\d+)?");
        var nums = matches.Select(m => double.Parse(m.Value, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        if (nums.Length < 2) return "Invalid input";
        return nums.Sum().ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
