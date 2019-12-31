// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using static Orang.Logger;

namespace Orang.CommandLine
{
    internal static class ConsoleHelpers
    {
        private static readonly ImmutableDictionary<string, DialogResult> _dialogResultMap = CreateDialogResultMap();

        private static ImmutableDictionary<string, DialogResult> CreateDialogResultMap()
        {
            ImmutableDictionary<string, DialogResult>.Builder builder = ImmutableDictionary.CreateBuilder<string, DialogResult>();

            builder.Add("y", DialogResult.Yes);
            builder.Add("ya", DialogResult.YesToAll);
            builder.Add("n", DialogResult.No);
            builder.Add("na", DialogResult.NoToAll);
            builder.Add("c", DialogResult.Cancel);

            return builder.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);
        }

        public static string ReadRedirectedInput()
        {
            if (Console.IsInputRedirected)
            {
                using (Stream stream = Console.OpenStandardInput())
                using (var streamReader = new StreamReader(stream, Console.InputEncoding))
                {
                    return streamReader.ReadToEnd();
                }
            }

            return null;
        }

        public static IEnumerable<string> ReadRedirectedInputAsLines()
        {
            if (Console.IsInputRedirected)
            {
                using (Stream stream = Console.OpenStandardInput())
                using (var streamReader = new StreamReader(stream, Console.InputEncoding))
                {
                    string line;

                    while ((line = streamReader.ReadLine()) != null)
                        yield return line;
                }
            }
        }

        public static bool Question(string question, string indent = null)
        {
            return QuestionIf(true, question, indent);
        }

        public static bool QuestionIf(bool condition, string question, string indent = null)
        {
            if (!condition)
                return true;

            ConsoleOut.Write(indent);
            ConsoleOut.Write(question);
            ConsoleOut.Write(" (Y/N/C): ");

            switch (Console.ReadLine()?.Trim())
            {
                case "y":
                case "Y":
                    {
                        return true;
                    }
                case "c":
                case "C":
                    {
                        throw new OperationCanceledException();
                    }
                case null:
                    {
                        ConsoleOut.WriteLine();
                        break;
                    }
            }

            return false;
        }

        public static DialogResult QuestionWithResult(string question, string indent = null)
        {
            ConsoleOut.Write(indent);
            ConsoleOut.Write(question);
            ConsoleOut.Write(" (Y/YA/N/NA/C): ");

            string s = Console.ReadLine()?.Trim();

            if (s != null)
            {
                if (_dialogResultMap.TryGetValue(s, out DialogResult dialogResult))
                    return dialogResult;
            }
            else
            {
                ConsoleOut.WriteLine();
            }

            return DialogResult.None;
        }
    }
}
