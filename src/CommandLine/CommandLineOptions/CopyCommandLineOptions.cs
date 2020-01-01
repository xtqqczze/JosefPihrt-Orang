// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using CommandLine;

namespace Orang.CommandLine
{
    [Verb("copy", HelpText = "Searches the file system for files and directories and copy them to a destionation directory.")]
    internal class CopyCommandLineOptions : CommonCopyCommandLineOptions
    {
        [Option(shortName: OptionShortNames.DryRun, longName: OptionNames.DryRun,
            HelpText = "Display which files or directories should be copied but do not actually copy any file or directory.")]
        public bool DryRun { get; set; }

        public bool TryParse(ref CopyCommandOptions options)
        {
            var baseOptions = (CommonCopyCommandOptions)options;

            if (!TryParse(ref baseOptions))
                return false;

            options = (CopyCommandOptions)baseOptions;

            options.DryRun = DryRun;

            return true;
        }
    }
}
