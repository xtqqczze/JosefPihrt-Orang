// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using CommandLine;
using Orang.FileSystem;
using static Orang.CommandLine.ParseHelpers;

namespace Orang.CommandLine
{
    internal abstract class CommonCopyCommandLineOptions : FindCommandLineOptions
    {
        [Option(longName: OptionNames.Overwrite,
            HelpText = "Defines how to proceed if a file already exists.",
            MetaValue = MetaValues.OverwriteOption)]
        public string Overwrite { get; set; }

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

            if (!TryParseAsEnum(Overwrite, OptionNames.Overwrite, out OverwriteOption overwriteOption, defaultValue: OverwriteOption.Ask, provider: OptionValueProviders.OverwriteOptionProvider))
                return false;

            options.OverwriteOption = overwriteOption;
            options.Target = target;
            options.RecurseSubdirectories = false;

            return true;
        }
    }
}
