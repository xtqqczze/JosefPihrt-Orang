// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Orang.CommandLine
{
    internal class CopyCommand : CommonCopyCommand<CopyCommandOptions>
    {
        public CopyCommand(CopyCommandOptions options) : base(options)
        {
        }

        protected override void ExecuteOperation(string sourcePath, string destinationPath, string indent)
        {
            File.Copy(sourcePath, destinationPath);
        }
    }
}
