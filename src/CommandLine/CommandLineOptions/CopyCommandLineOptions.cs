// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using CommandLine;
using Orang.FileSystem;
using static Orang.CommandLine.ParseHelpers;

namespace Orang.CommandLine
{
    [Verb("copy", HelpText = "Searches the file system for files and directories and copy them to a destination directory.")]
    internal class CopyCommandLineOptions : CommonCopyCommandLineOptions
    {
        [Option(shortName: OptionShortNames.DryRun, longName: OptionNames.DryRun,
            HelpText = "Display which files or directories should be copied but do not actually copy any file or directory.")]
        public bool DryRun { get; set; }

        [Option(longName: OptionNames.Overwrite,
            HelpText = "Defines how to proceed if a file already exists.",
            MetaValue = MetaValues.OverwriteOption)]
        public string Overwrite { get; set; }

        [Option(longName: OptionNames.Target,
            Required = true,
            HelpText = "A directory to copy files and directories to.",
            MetaValue = MetaValues.DirectoryPath)]
        public string Target { get; set; }

        public bool TryParse(ref CopyCommandOptions options)
        {
            var baseOptions = (CommonCopyCommandOptions)options;

            if (!TryParse(ref baseOptions))
                return false;

            options = (CopyCommandOptions)baseOptions;

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
