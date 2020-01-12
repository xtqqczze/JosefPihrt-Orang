// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using CommandLine;
using Orang.FileSystem;
using static Orang.CommandLine.ParseHelpers;

namespace Orang.CommandLine
{
    [Verb("sync", HelpText = "Synchronizes content of two directories.")]
    internal sealed class SyncCommandLineOptions : CommonCopyCommandLineOptions
    {
        [Option(longName: OptionNames.SyncBase,
            HelpText = "Action to choose if a file or directory exists in one directory and it is missing in the second directory.",
            MetaValue = MetaValues.SyncBase)]
        public string SyncBase { get; set; }

        [Option(shortName: OptionShortNames.DryRun, longName: OptionNames.DryRun,
            HelpText = "Display which files or directories should be copied/deleted but do not actually copy/delete any file or directory.")]
        public bool DryRun { get; set; }

        [Option(shortName: OptionShortNames.Target, longName: OptionNames.Target,
            Required = true,
            HelpText = "A directory to be synchronized.",
            MetaValue = MetaValues.DirectoryPath)]
        public string Target { get; set; }

        [Option(longName: OptionNames.SyncMode,
            HelpText = "Synchronization mode to be used.",
            MetaValue = MetaValues.SyncMode)]
        public string SyncMode { get; set; }

        public bool TryParse(SyncCommandOptions options)
        {
            var baseOptions = (CommonCopyCommandOptions)options;

            if (!TryParse(baseOptions))
                return false;

            options = (SyncCommandOptions)baseOptions;

            if (options.Paths.Length > 1)
            {
                Logger.WriteError("More than one source directory cannot be synchronized.");
                return false;
            }

            if (!TryParseAsEnumFlags(Compare, OptionNames.Compare, out FileCompareOptions compareOptions, FileCompareOptions.Attributes | FileCompareOptions.Content | FileCompareOptions.ModifiedTime | FileCompareOptions.Size, OptionValueProviders.FileCompareOptionsProvider))
                return false;

            if (!TryEnsureFullPath(Target, out string target))
                return false;

            if (!TryParseAsEnum(SyncBase, OptionNames.SyncBase, out SyncBase syncBase, defaultValue: Orang.SyncBase.Source, provider: OptionValueProviders.SyncBaseProvider))
                return false;

            if (!TryParseAsEnum(SyncMode, OptionNames.SyncMode, out SyncMode syncMode, defaultValue: Orang.SyncMode.Synchronize, provider: OptionValueProviders.SyncModeProvider))
                return false;

            options.SearchTarget = SearchTarget.All;

            options.CompareOptions = compareOptions;
            options.DryRun = DryRun;
            options.Target = target;
            options.SyncBase = syncBase;
            options.SyncMode = syncMode;

            return true;
        }
    }
}
