// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using CommandLine;
using Orang.FileSystem;
using static Orang.CommandLine.ParseHelpers;

namespace Orang.CommandLine
{
    internal abstract class CommonCopyCommandLineOptions : FindCommandLineOptions
    {
        [Option(longName: OptionNames.Conflict,
            HelpText = "Defines how to resolve conflict during copy/move operation.",
            MetaValue = MetaValues.ConflictOption)]
        public string Conflict { get; set; }

        [Option(longName: OptionNames.Target,
            Required = true,
            HelpText = "The directory to copy files and directories to.",
            MetaValue = MetaValues.DirectoryPath)]
        public string Target { get; set; }

        public bool TryParse(ref CommonCopyCommandOptions options)
        {
            var baseOptions = (FindCommandOptions)options;

            if (!TryParse(ref baseOptions))
                return false;

            options = (CommonCopyCommandOptions)baseOptions;

            if (!TryEnsureFullPath(Target, out string target))
                return false;

            if (!TryParseAsEnum(Conflict, OptionNames.Conflict, out ConflictOption conflictOption, defaultValue: ConflictOption.Ask, provider: OptionValueProviders.ConflictOptionProvider))
                return false;

            options.ConflictOption = conflictOption;
            options.Target = target;

            return true;
        }
    }
}
