using System;

namespace Pustalorc.Libraries.MySqlConnectorWrapper
{
    /// <summary>
    ///     Utilities file. Any global methods are included here.
    /// </summary>
    public static class Utils
    {
        /// <summary>
        ///     Log a message to console.
        /// </summary>
        /// <param name="source">Specific source of the message.</param>
        /// <param name="message">The message to be logged to console.</param>
        /// <param name="consoleColor">The color to be used for the message in console.</param>
        public static void LogConsole(object source, object message, ConsoleColor consoleColor = ConsoleColor.Green)
        {
            Console.ForegroundColor = consoleColor;
            Console.WriteLine($"[{source}]: {message}");
            Console.ResetColor();
        }

        /// <summary>
        ///     Transforms the input into a valid encapsulated value that can be safely used in a MySql query.
        /// </summary>
        /// <param name="input">The value to encapsulate.</param>
        /// <returns>The input but encapsulated.</returns>
        public static string ToSafeValue(string input)
        {
            if (!input.Contains(";") && !input.Contains("'") && !input.Contains("\"")) return input;

            if (!input.Contains("'") && !input.Contains("\"")) return $"\"{input}\"";

            if (!input.Contains("'")) return $"'{input}'";

            if (!input.Contains("\"")) return $"\"{input}\"";

            return input.StartsWith("\"", StringComparison.InvariantCulture)
                ? $"'{RepeatChar(input, '\'')}'"
                : $"\"{RepeatChar(input, '\"')}\"";
        }

        /// <summary>
        ///     Repeats a specific character in the string if found.
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