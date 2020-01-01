// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using CommandLine;

namespace Orang.CommandLine
{
    [Verb("move", HelpText = "Searches the file system for files and directories and move them to a destionation directory.")]
    internal class MoveCommandLineOptions : CommonCopyCommandLineOptions
    {
        [Option(shortName: OptionShortNames.DryRun, longName: OptionNames.DryRun,
            HelpText = "Display which files or directories should be moved but do not actually move any file or directory.")]
        public bool DryRun { get; set; }

        public bool TryParse(ref MoveCommandOptions options)
        {
            var baseOptions = (CommonCopyCommandOptions)options;

            if (!TryParse(ref baseOptions))
                return false;

            options = (MoveCommandOptions)baseOptions;

            options.DryRun = DryRun;

            return true;
        }
    }
}
