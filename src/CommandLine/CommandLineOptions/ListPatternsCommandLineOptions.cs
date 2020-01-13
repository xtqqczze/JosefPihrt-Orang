// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Text.RegularExpressions;
using CommandLine;
using static Orang.CommandLine.ParseHelpers;

namespace Orang.CommandLine
{
    [Verb("list-patterns", HelpText = "Lists all basic patterns that will match specified character.")]
    internal sealed class ListPatternsCommandLineOptions
    {
        [Value(index: 0,
            HelpText = "Character or a decimal number that represents the character. For a number literal use escape like \\1.",
            MetaName = ArgumentMetaNames.Char)]
        public string Value { get; set; }

        [Option(shortName: OptionShortNames.Options, longName: OptionNames.Options,
            HelpText = "Regex options that should be used. Relevant values are [e]cma-[s]cript or [i]gnore-case.",
            MetaValue = MetaValues.RegexOptions)]
        public IEnumerable<string> Options { get; set; }

        [Option(longName: OptionNames.CharGroup,
            HelpText = "Treat character as if it is in the character group.")]
        public bool CharGroup { get; set; }

        public bool TryParse(ListPatternsCommandOptions options)
        {
            if (!TryParseChar(Value, out char value))
                return false;

            if (!TryParseAsEnumFlags(Options, OptionNames.Options, out RegexOptions regexOptions, provider: OptionValueProviders.RegexOptionsProvider))
                return false;

            options.Value = value;
            options.RegexOptions = regexOptions;
            options.InCharGroup = CharGroup;

            return true;
        }
    }
}
