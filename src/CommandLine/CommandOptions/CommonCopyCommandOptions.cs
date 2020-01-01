// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Orang.FileSystem;

namespace Orang.CommandLine
{
    internal class CommonCopyCommandOptions : FindCommandOptions
    {
        private string _target;

        internal CommonCopyCommandOptions()
        {
        }

        public bool DryRun { get; internal set; }

        public string Target
        {
            get { return _target; }

            internal set
            {
                _target = value;
                TargetNormalized = null;
            }
        }

        public string TargetNormalized { get; internal set; }

        public OverwriteOption OverwriteOption { get; internal set; }
    }
}
