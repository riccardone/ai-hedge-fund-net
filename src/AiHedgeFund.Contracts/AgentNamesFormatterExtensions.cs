using System.Text.RegularExpressions;

namespace AiHedgeFund.Contracts;

public static class AgentNamesFormatterExtensions
{
    public static string ToDisplayName(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        // Remove the "Agent" suffix if it exists
        if (input.EndsWith("Agent"))
            input = input.Substring(0, input.Length - "Agent".Length);

        // Insert spaces before uppercase letters (excluding the first letter)
        var spaced = Regex.Replace(input, "(?<!^)([A-Z])", " $1");

        return spaced.Trim();
    }

    public static string ToSnakeCase(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        const string suffixToRemove = "Agent";
        if (input.EndsWith(suffixToRemove))
            input = input.Substring(0, input.Length - suffixToRemove.Length);

        var stringBuilder = new System.Text.StringBuilder();
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            if (char.IsUpper(c))
            {
                if (i > 0)
                    stringBuilder.Append('_');
                stringBuilder.Append(char.ToLowerInvariant(c));
            }
            else
            {
                stringBuilder.Append(c);
            }
        }
        return stringBuilder.ToString();
    }
}