using System;
using System.Collections.Generic;
using System.Text;

namespace DotNet.Extensions
{
    internal static class CommandLineExtensions
    {
        public static List<string> SplitCommandLineIntoArguments(string commandLine, bool removeHashComments)
        {
            return SplitCommandLineIntoArguments(commandLine, removeHashComments, out _);
        }

        private static List<string> SplitCommandLineIntoArguments(string commandLine, bool removeHashComments, out char? illegalChar)
        {
            var list = new List<string>();
            SplitCommandLineIntoArguments(commandLine.AsSpan(), removeHashComments, new StringBuilder(), list, out illegalChar);
            return list;
        }

        private static void SplitCommandLineIntoArguments(ReadOnlySpan<char> commandLine, bool removeHashComments, StringBuilder builder, List<string> list, out char? illegalChar)
        {
            var i = 0;

            builder.Length = 0;
            illegalChar = null;
            while (i < commandLine.Length)
            {
                while (i < commandLine.Length && char.IsWhiteSpace(commandLine[i]))
                {
                    i++;
                }

                if (i == commandLine.Length)
                {
                    break;
                }

                if (commandLine[i] == '#' && removeHashComments)
                {
                    break;
                }

                var quoteCount = 0;
                builder.Length = 0;
                while (i < commandLine.Length && (!char.IsWhiteSpace(commandLine[i]) || (quoteCount % 2 != 0)))
                {
                    var current = commandLine[i];
                    switch (current)
                    {
                        case '\\':
                            {
                                var slashCount = 0;
                                do
                                {
                                    builder.Append(commandLine[i]);
                                    i++;
                                    slashCount++;
                                } while (i < commandLine.Length && commandLine[i] == '\\');

                                // Slashes not followed by a quote character can be ignored for now
                                if (i >= commandLine.Length || commandLine[i] != '"')
                                {
                                    break;
                                }

                                // If there is an odd number of slashes then it is escaping the quote
                                // otherwise it is just a quote.
                                if (slashCount % 2 == 0)
                                {
                                    quoteCount++;
                                }

                                builder.Append('"');
                                i++;
                                break;
                            }

                        case '"':
                            builder.Append(current);
                            quoteCount++;
                            i++;
                            break;

                        default:
                            if ((current >= 0x1 && current <= 0x1f) || current == '|')
                            {
                                if (illegalChar == null)
                                {
                                    illegalChar = current;
                                }
                            }
                            else
                            {
                                builder.Append(current);
                            }

                            i++;
                            break;
                    }
                }

                // If the quote string is surrounded by quotes with no interior quotes then
                // remove the quotes here.
                if (quoteCount == 2 && builder[0] == '"' && builder[builder.Length - 1] == '"')
                {
                    builder.Remove(0, length: 1);
                    builder.Remove(builder.Length - 1, length: 1);
                }

                if (builder.Length > 0)
                {
                    list.Add(builder.ToString());
                }
            }
        }
    }
}
