﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using CommandLine;

namespace Orang.CommandLine
{
    internal abstract class CommonCopyCommandLineOptions : FindCommandLineOptions
    {
        [Option(longName: OptionNames.Compare,
            HelpText = "File properties to be compared.",
            MetaValue = MetaValues.CompareOptions)]
        public IEnumerable<string> Compare { get; set; }

        [Option(longName: OptionNames.TargetAction,
            HelpText = "Action to choose if a file already exists.",
            MetaValue = MetaValues.TargetAction)]
        public string TargetAction { get; set; }

        public bool TryParse(ref CommonCopyCommandOptions options)
        {
            var baseOptions = (FindCommandOptions)options;

            if (!TryParse(ref baseOptions))
                return false;

            options = (CommonCopyCommandOptions)baseOptions;

            return true;
        }
    }
}
