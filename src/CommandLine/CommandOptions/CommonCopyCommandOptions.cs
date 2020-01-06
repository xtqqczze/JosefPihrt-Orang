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

        public FileCompareOptions CompareOptions { get; internal set; }

        public bool CompareAttributes => (CompareOptions & FileCompareOptions.Attributes) != 0;

        public bool CompareContent => (CompareOptions & FileCompareOptions.Content) != 0;

        public bool CompareModifiedTime => (CompareOptions & FileCompareOptions.ModifiedTime) != 0;

        public bool CompareSize => (CompareOptions & FileCompareOptions.Size) != 0;

        public bool DryRun { get; internal set; }

        public bool Flat { get; internal set; }

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

        public TargetExistsAction TargetAction { get; internal set; }
    }
}
