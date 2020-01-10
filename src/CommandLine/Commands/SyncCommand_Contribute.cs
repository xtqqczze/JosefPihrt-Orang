// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Orang.FileSystem;

namespace Orang.CommandLine
{
    internal class SyncCommand_Contribute : CommonSyncCommand
    {
        public SyncCommand_Contribute(SyncCommandOptions options) : base(options)
        {
        }

        protected override void ExecuteOperation(SearchContext context, string sourcePath, string destinationPath, bool isDirectory, string indent)
        {
            bool fileExists = File.Exists(destinationPath);
            bool directoryExists = !fileExists && Directory.Exists(destinationPath);

            if (isDirectory)
            {
                if (directoryExists
                    && File.GetAttributes(sourcePath) == File.GetAttributes(destinationPath))
                {
                    return;
                }
            }
            else if (fileExists
                && Options.CompareOptions != FileCompareOptions.None
                && FileSystemHelpers.FileEquals(sourcePath, destinationPath, Options.CompareOptions))
            {
                return;
            }

            ExecuteOperations(context, sourcePath, destinationPath, isDirectory, fileExists, directoryExists, preferTarget: false, indent);
        }
    }
}
