// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using CommandLine;

namespace Orang.CommandLine
{
    [Verb("move", HelpText = "Searches the file system for files and directories and move them to a destionation directory.")]
    internal class MoveCommandLineOptions : CommonCopyCommandLineOptions
    {
        public bool TryParse(ref MoveCommandOptions options)
        {
            var baseOptions = (CommonCopyCommandOptions)options;

            if (!TryParse(ref baseOptions))
                return false;

            options = (MoveCommandOptions)baseOptions;

            return true;
        }
    }
}
