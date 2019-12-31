// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Orang.FileSystem;

namespace Orang.CommandLine
{
    internal class CommonCopyCommandOptions : FindCommandOptions
    {
        internal CommonCopyCommandOptions()
        {
        }

        public string Target { get; internal set; }

        public string TargetNormalized { get; internal set; }

        public OverwriteOption OverwriteOption { get; internal set; }
    }
}
