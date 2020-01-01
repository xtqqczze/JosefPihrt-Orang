// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using CommandLine;
using Orang.FileSystem;
using static Orang.CommandLine.ParseHelpers;

namespace Orang.CommandLine
{
    [Verb("move", HelpText = "Searches the file system for files and directories and move them to a destination directory.")]
    internal class MoveCommandLineOptions : CommonCopyCommandLineOptions
    {
        [Option(shortName: OptionShortNames.DryRun, longName: OptionNames.DryRun,
            HelpText = "Display which files or directories should be moved but do not actually move any file or directory.")]
        public bool DryRun { get; set; }

        [Option(longName: OptionNames.Overwrite,
            HelpText = "Defines how to proceed if a file already exists.",
            MetaValue = MetaValues.OverwriteOption)]
        public string Overwrite { get; set; }

        [Option(longName: OptionNames.Target,
            Required = true,
            HelpText = "A directory to move files and directories to.",
            MetaValue = MetaValues.DirectoryPath)]
        public string Target { get; set; }

        public bool TryParse(ref MoveCommandOptions options)
        {
            var baseOptions = (CommonCopyCommandOptions)options;

            if (!TryParse(ref baseOptions))
                return false;

            options = (MoveCommandOptions)baseOptions;

            if (!TryEnsureFullPath(Target, out string target))
                return false;

            if (!TryParseAsEnum(Overwrite, OptionNames.Overwrite, out OverwriteOption overwriteOption, defaultValue: OverwriteOption.Ask, provider: OptionValueProviders.OverwriteOptionProvider))
                return false;

            options.DryRun = DryRun;
            options.OverwriteOption = overwriteOption;
            options.Target = target;

            return true;
        }
    }
}
