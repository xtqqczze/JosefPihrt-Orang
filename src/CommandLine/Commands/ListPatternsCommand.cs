// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Orang.Syntax;
using static Orang.Logger;

namespace Orang.CommandLine
{
    internal class ListPatternsCommand : AbstractCommand<ListPatternsCommandOptions>
    {
        public ListPatternsCommand(ListPatternsCommandOptions options) : base(options)
        {
        }

        protected override CommandResult ExecuteCore(CancellationToken cancellationToken = default)
        {
            char ch = Options.Value;

            var rows = new List<(string name, string description)>();

            if (ch >= 0 && ch <= 0x7F)
                rows.Add(("Name", TextHelpers.SplitCamelCase(((AsciiChar)ch).ToString())));

            int charCode = ch;

            rows.Add(("Decimal", charCode.ToString(CultureInfo.InvariantCulture)));
            rows.Add(("Hexadecimal", charCode.ToString("X", CultureInfo.InvariantCulture)));

            List<PatternInfo> patterns = GetPatterns(ch, inCharGroup: Options.InCharGroup, options: Options.RegexOptions).ToList();

            int width = Math.Max(rows.Max(f => f.name.Length), patterns.Max(f => f.Pattern.Length));

            WriteLine();

            foreach (var (name, description) in rows)
                WriteRow(name, description);

            WriteLine();

            WriteRow("PATTERN", "DESCRIPTION");

            foreach (PatternInfo item in patterns)
                WriteRow(item.Pattern, item.Description);

            return CommandResult.Success;

            void WriteRow(string value1, string value2, in ConsoleColors colors1 = default, in ConsoleColors colors2 = default)
            {
                Write(value1, colors1);
                Write(' ', width - value1.Length + 1);
                WriteLine(value2 ?? "-", colors2);
            }
        }

        private static IEnumerable<PatternInfo> GetPatterns(int charCode, bool inCharGroup = false, RegexOptions options = RegexOptions.None)
        {
            string s = ((char)charCode).ToString();

            if (charCode <= 0xFF)
            {
                switch (RegexEscape.GetEscapeMode((char)charCode, inCharGroup))
                {
                    case CharEscapeMode.Backslash:
                        {
                            yield return new PatternInfo(@"\" + ((char)charCode).ToString(), "Escaped character");
                            break;
                        }
                    case CharEscapeMode.Bell:
                        {
                            yield return new PatternInfo(@"\a");
                            break;
                        }
                    case CharEscapeMode.CarriageReturn:
                        {
                            yield return new PatternInfo(@"\r");
                            break;
                        }
                    case CharEscapeMode.Escape:
                        {
                            yield return new PatternInfo(@"\e");
                            break;
                        }
                    case CharEscapeMode.FormFeed:
                        {
                            yield return new PatternInfo(@"\f");
                            break;
                        }
                    case CharEscapeMode.Linefeed:
                        {
                            yield return new PatternInfo(@"\n");
                            break;
                        }
                    case CharEscapeMode.Tab:
                        {
                            yield return new PatternInfo(@"\t");
                            break;
                        }
                    case CharEscapeMode.VerticalTab:
                        {
                            yield return new PatternInfo(@"\v");
                            break;
                        }
                    case CharEscapeMode.None:
                        {
                            yield return new PatternInfo(((char)charCode).ToString());
                            break;
                        }
                }

                if (inCharGroup && charCode == 8)
                    yield return new PatternInfo(@"\b", "Escaped character");
            }

            if (Regex.IsMatch(s, @"\d", options))
            {
                yield return new PatternInfo(@"\d", "Digit character");
            }
            else
            {
                yield return new PatternInfo(@"\D", "Non-digit character");
            }

            if (Regex.IsMatch(s, @"\s", options))
            {
                yield return new PatternInfo(@"\s", "Whitespace character");
            }
            else
            {
                yield return new PatternInfo(@"\S", "Non-whitespace character");
            }

            if (Regex.IsMatch(s, @"\w", options))
            {
                yield return new PatternInfo(@"\w", "Word character");
            }
            else
            {
                yield return new PatternInfo(@"\W", "Non-word character");
            }

            foreach (SyntaxItem item in SyntaxItems.Values)
            {
                if (item.Section == SyntaxSection.GeneralCategories)
                {
                    string pattern = @"\p{" + item.Text + "}";

                    if (Regex.IsMatch(s, pattern, options))
                    {
                        yield return new PatternInfo(pattern, $"Unicode category: {item.Description}");
                    }
                }
            }

            foreach (SyntaxItem item in SyntaxItems.Values)
            {
                if (item.Section == SyntaxSection.NamedBlocks)
                {
                    string pattern = @"\p{" + item.Text + "}";

                    if (Regex.IsMatch(s, pattern, options))
                    {
                        yield return new PatternInfo(pattern, $"Unicode block: {item.Description}");
                        break;
                    }
                }
            }

            if (charCode <= 0xFF)
            {
                yield return new PatternInfo(@"\u" + charCode.ToString("X4", CultureInfo.InvariantCulture), "Unicode character (four hexadecimal digits)");

                yield return new PatternInfo(@"\x" + charCode.ToString("X2", CultureInfo.InvariantCulture), "ASCII character (two hexadecimal digits)");

                yield return new PatternInfo(@"\" + Convert.ToString(charCode, 8).PadLeft(2, '0'), "ASCII character (two or three octal digits)");

                if (charCode > 0 && charCode <= 0x1A)
                {
                    yield return new PatternInfo(@"\c" + Convert.ToChar('a' + charCode - 1), "ASCII control character");
                    yield return new PatternInfo(@"\c" + Convert.ToChar('A' + charCode - 1), "ASCII control character");
                }
            }
        }

        private class PatternInfo
        {
            public PatternInfo(string pattern, string description = null)
            {
                Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
                Description = description;
            }

            public string Pattern { get; }

            public string Description { get; }
        }

        private enum AsciiChar
        {
            Null = 0,
            StartOfHeading = 1,
            StartOfText = 2,
            EndOfText = 3,
            EndOfTransmission = 4,
            Enquiry = 5,
            Acknowledge = 6,
            Bell = 7,
            Backspace = 8,
            Tab = 9,
            Linefeed = 10,
            VerticalTab = 11,
            FormFeed = 12,
            CarriageReturn = 13,
            ShiftOut = 14,
            ShiftIn = 15,
            DataLinkEscape = 16,
            DeviceControlOne = 17,
            DeviceControlTwo = 18,
            DeviceControlThree = 19,
            DeviceControlFour = 20,
            NegativeAcknowledge = 21,
            SynchronousIdle = 22,
            EndOfTransmissionBlock = 23,
            Cancel = 24,
            EndOfMedium = 25,
            Substitute = 26,
            Escape = 27,
            InformationSeparatorFour = 28,
            InformationSeparatorThree = 29,
            InformationSeparatorTwo = 30,
            InformationSeparatorOne = 31,
            Space = 32,
            ExclamationMark = 33,
            QuoteMark = 34,
            NumberSign = 35,
            Dollar = 36,
            Percent = 37,
            Ampersand = 38,
            Apostrophe = 39,
            LeftParenthesis = 40,
            RightParenthesis = 41,
            Asterisk = 42,
            Plus = 43,
            Comma = 44,
            Hyphen = 45,
            Dot = 46,
            Slash = 47,
            DigitZero = 48,
            DigitOne = 49,
            DigitTwo = 50,
            DigitThree = 51,
            DigitFour = 52,
            DigitFive = 53,
            DigitSix = 54,
            DigitSeven = 55,
            DigitEight = 56,
            DigitNine = 57,
            Colon = 58,
            Semicolon = 59,
            LeftAngleBracket = 60,
            EqualsSign = 61,
            RightAngleBracket = 62,
            QuestionMark = 63,
            AtSign = 64,
            CapitalLetterA = 65,
            CapitalLetterB = 66,
            CapitalLetterC = 67,
            CapitalLetterD = 68,
            CapitalLetterE = 69,
            CapitalLetterF = 70,
            CapitalLetterG = 71,
            CapitalLetterH = 72,
            CapitalLetterI = 73,
            CapitalLetterJ = 74,
            CapitalLetterK = 75,
            CapitalLetterL = 76,
            CapitalLetterM = 77,
            CapitalLetterN = 78,
            CapitalLetterO = 79,
            CapitalLetterP = 80,
            CapitalLetterQ = 81,
            CapitalLetterR = 82,
            CapitalLetterS = 83,
            CapitalLetterT = 84,
            CapitalLetterU = 85,
            CapitalLetterV = 86,
            CapitalLetterW = 87,
            CapitalLetterX = 88,
            CapitalLetterY = 89,
            CapitalLetterZ = 90,
            LeftSquareBracket = 91,
            Backslash = 92,
            RightSquareBracket = 93,
            CircumflexAccent = 94,
            Underscore = 95,
            GraveAccent = 96,
            SmallLetterA = 97,
            SmallLetterB = 98,
            SmallLetterC = 99,
            SmallLetterD = 100,
            SmallLetterE = 101,
            SmallLetterF = 102,
            SmallLetterG = 103,
            SmallLetterH = 104,
            SmallLetterI = 105,
            SmallLetterJ = 106,
            SmallLetterK = 107,
            SmallLetterL = 108,
            SmallLetterM = 109,
            SmallLetterN = 110,
            SmallLetterO = 111,
            SmallLetterP = 112,
            SmallLetterQ = 113,
            SmallLetterR = 114,
            SmallLetterS = 115,
            SmallLetterT = 116,
            SmallLetterU = 117,
            SmallLetterV = 118,
            SmallLetterW = 119,
            SmallLetterX = 120,
            SmallLetterY = 121,
            SmallLetterZ = 122,
            LeftCurlyBracket = 123,
            VerticalBar = 124,
            RightCurlyBracket = 125,
            Tilde = 126,
            Delete = 127,
        }
    }
}
