using System;

namespace Pustalorc.MySql.Data.Wrapper
{
    /// <summary>
    /// Utilities to deal with special things in MySql
    /// </summary>
    public static class MySqlUtilities
    {
        /// <summary>
        /// Transforms the input into a valid encapsulated value that can be safely used in a MySql connection string.
        /// </summary>
        /// <param name="input">The value to encapsulate.</param>
        /// <returns>The input but encapsulated.</returns>
        public static string ToSafeConnectionStringValue(string input)
        {
            if (!input.Contains(";") && !input.Contains("'") && !input.Contains("\"")) return input;

            if (!input.Contains("'") && !input.Contains("\"")) return $"\"{input}\"";

            if (!input.Contains("'")) return $"'{input}'";

            if (!input.Contains("\"")) return $"\"{input}\"";

            return input.StartsWith("\"", StringComparison.Ordinal)
                ? $"'{RepeatChar(input, '\'')}'"
                : $"\"{RepeatChar(input, '\"')}\"";
        }

        /// <summary>
        /// Repeats a specific character in the string if found.
        /// </summary>
        /// <param name="input">The input string to cycle through.</param>
        /// <param name="character">The character to repeat.</param>
        /// <returns>A new string with the selected character repeated.</returns>
        public static string RepeatChar(string input, char character)
        {
            var output = "";

            foreach (var c in input)
            {
                if (c == character)
                    output += c;

                output += c;
            }

            return output;
        }
    }
}