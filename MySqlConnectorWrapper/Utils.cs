using Org.BouncyCastle.Utilities.Encoders;
using Pustalorc.Libraries.MySqlConnectorWrapper.Caching;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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

            return input.StartsWith("\"", StringComparison.Ordinal)
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

        /// <summary>
        ///     Returns the index of the first element which satisfies the match within the enumerable.
        /// </summary>
        /// <typeparam name="TSource">The type of the array.</typeparam>
        /// <param name="source">The instance of the enumerable.</param>
        /// <param name="match">The predicate to match the source with.</param>
        /// <returns>An index based on the rules of List.FindIndex</returns>
        public static int FindFirstIndex<TSource>(this IEnumerable<TSource> source, Predicate<TSource> match)
        {
            return source.ToList().FindIndex(match);
        }

        /// <summary>
        ///     Returns the index of the first element which is null within the array.
        /// </summary>
        /// <typeparam name="TSource">A class that can be nullable and that defines the type of the array.</typeparam>
        /// <param name="source">The instance of the array that the first null case should be found.</param>
        /// <returns>An index based on the rules of List.FindIndex</returns>
        public static int FindFirstIndexNull<TSource>(this TSource[] source)
        {
            return source.FindFirstIndex(k => k == null);
        }

        internal static int IndexOfLeastUse(this CachedQuery[] source)
        {
            var first = source.Where(k => k != null).OrderBy(k => k.AccessCount).FirstOrDefault();

            if (first == null)
                return source.FindFirstIndexNull();

            return source.FindFirstIndex(k => k != null && k.Query.QueryString.Equals(first.Query.QueryString, StringComparison.OrdinalIgnoreCase));
        }
    }
}