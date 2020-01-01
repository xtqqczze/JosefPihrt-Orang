// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using CommandLine;
using Orang.FileSystem;
using static Orang.CommandLine.ParseHelpers;

namespace Orang.CommandLine
{
    [Verb("sync", HelpText = "Synchronizes content of one directory with another directory.")]
    internal class SyncCommandLineOptions : CommonCopyCommandLineOptions
    {
        [Option(longName: OptionNames.Target,
            Required = true,
            HelpText = "A directory to be synchronized.",
            MetaValue = MetaValues.DirectoryPath)]
        public string Target { get; set; }

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

            if (!TryEnsureFullPath(Target, out string target))
                return false;

            options.OverwriteOption = OverwriteOption.Yes;
            options.Target = target;

            return true;
        }
    }
}
