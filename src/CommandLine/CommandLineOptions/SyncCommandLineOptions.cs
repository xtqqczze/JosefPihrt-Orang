// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using CommandLine;
using Orang.FileSystem;
using static Orang.CommandLine.ParseHelpers;

namespace Orang.CommandLine
{
    [Verb("sync", HelpText = "Synchronizes content of one directory with another directory.")]
    internal sealed class SyncCommandLineOptions : CommonCopyCommandLineOptions
    {
        [Option(shortName: OptionShortNames.DryRun, longName: OptionNames.DryRun,
            HelpText = "Display which files or directories should be copied/deleted but do not actually copy/delete any file or directory.")]
        public bool DryRun { get; set; }

        [Option(longName: OptionNames.Target,
            Required = true,
            HelpText = "A directory to be synchronized.",
            MetaValue = MetaValues.DirectoryPath)]
        public string Target { get; set; }

        [Option(longName: OptionNames.TwoWay,
            HelpText = "Synchronize directories in both directions.")]
        public bool TwoWay { get; set; }

        public bool TryParse(ref SyncCommandOptions options)
        {
            var baseOptions = (CommonCopyCommandOptions)options;

            if (!TryParse(ref baseOptions))
                return false;

            options = (SyncCommandOptions)baseOptions;

            if (options.Paths.Length > 1)
            {
                Logger.WriteError("More than one source directory cannot be synchronized.");
                return false;
            }

            if (!TryParseAsEnumFlags(Compare, OptionNames.Compare, out FileCompareOptions compareOptions, FileCompareOptions.Attributes | FileCompareOptions.Content | FileCompareOptions.ModifiedTime |  FileCompareOptions.Size, OptionValueProviders.FileCompareOptionsProvider))
                return false;

            if (!TryEnsureFullPath(Target, out string target))
                return false;

            if (!TryParseAsEnum(TargetAction, OptionNames.TargetAction, out TargetExistsAction targetAction, defaultValue: TargetExistsAction.Overwrite, provider: OptionValueProviders.TargetExistsActionProvider))
                return false;

            options.SearchTarget = SearchTarget.All;

            options.CompareOptions = compareOptions;
            options.DryRun = DryRun;
            options.Target = target;
            options.TargetAction = targetAction;
            options.TwoWay = TwoWay;

            return true;
        }
    }
}
