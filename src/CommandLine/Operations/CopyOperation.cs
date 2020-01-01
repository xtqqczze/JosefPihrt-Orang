// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Orang.CommandLine
{
    internal class CopyOperation : CommonCopyOperation
    {
        public CopyOperation(CopyCommandOptions options)
        {
            Options = options;
        }

        protected override CommonCopyCommandOptions CommonOptions => Options;

        new public CopyCommandOptions Options { get; }

        public override OperationKind Kind => OperationKind.Copy;

        protected override void ExecuteOperation(string sourcePath, string destinationPath)
        {
            File.Copy(sourcePath, destinationPath);
        }
    }
}
