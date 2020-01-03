// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using CommandLine;
using Orang.FileSystem;
using static Orang.CommandLine.ParseHelpers;

namespace Orang.CommandLine
{
    [Verb("move", HelpText = "Searches the file system for files and directories and move them to a destination directory.")]
    internal class MoveCommandLineOptions : CommonCopyCommandLineOptions
    {
        [Option(longName: OptionNames.Compare,
            HelpText = "File properties to be compared.",
            MetaValue = MetaValues.CompareOptions)]
        public IEnumerable<string> Compare { get; set; }

        [Option(shortName: OptionShortNames.DryRun, longName: OptionNames.DryRun,
            HelpText = "Display which files or directories should be moved but do not actually move any file or directory.")]
        public bool DryRun { get; set; }

        [Option(longName: OptionNames.Target,
            Required = true,
            HelpText = "A directory to move files and directories to.",
            MetaValue = MetaValues.DirectoryPath)]
        public string Target { get; set; }

        [Option(longName: OptionNames.TargetAction,
            HelpText = "Defines how to proceed if a file already exists.",
            MetaValue = MetaValues.TargetAction)]
        public string TargetAction { get; set; }

        public bool TryParse(ref MoveCommandOptions options)
        {
            var baseOptions = (CommonCopyCommandOptions)options;

            if (!TryParse(ref baseOptions))
                return false;

            options = (MoveCommandOptions)baseOptions;

            if (!TryParseAsEnumFlags(Compare, OptionNames.Compare, out FileCompareOptions compareOptions, FileCompareOptions.None, OptionValueProviders.FileCompareOptionsProvider))
                return false;

            if (!TryEnsureFullPath(Target, out string target))
                return false;

            if (!TryParseAsEnum(TargetAction, OptionNames.TargetAction, out TargetExistsAction targetAction, defaultValue: TargetExistsAction.Ask, provider: OptionValueProviders.TargetExistsActionProvider))
                return false;

            options.CompareOptions = compareOptions;
            options.DryRun = DryRun;
            options.TargetAction = targetAction;
            options.Target = target;

            return true;
        }
    }
}
