// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Orang.CommandLine
{
    internal class MoveOperation : CommonCopyOperation
    {
        public MoveOperation(MoveCommandOptions options)
        {
            Options = options;
        }

        protected override CommonCopyCommandOptions CommonOptions => Options;

        new public MoveCommandOptions Options { get; }

        public override OperationKind Kind => OperationKind.Move;

        protected override void ExecuteOperation(string sourcePath, string destinationPath)
        {
            File.Move(sourcePath, destinationPath);
        }
    }
}
