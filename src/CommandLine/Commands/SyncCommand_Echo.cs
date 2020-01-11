// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Orang.FileSystem;

namespace Orang.CommandLine
{
    internal class SyncCommand_Echo : SyncCommand_Contribute
    {
        public SyncCommand_Echo(SyncCommandOptions options) : base(options)
        {
        }

        protected override void ExecuteDirectory(string directoryPath, SearchContext context)
        {
            _destinationPaths = new HashSet<string>(FileSystemHelpers.Comparer);

            base.ExecuteDirectory(directoryPath, context);

            string indent = GetPathIndent(directoryPath);

            foreach (string path in FileSystemHelpers.EnumerateAllDirectories(Target))
            {
                if (_destinationPaths.Contains(path))
                    continue;

                string relativePath = path.Substring(Target.Length + 1);

                string sourcePath = Path.Combine(directoryPath, relativePath);

                if (!Directory.Exists(sourcePath))
                {
                    try
                    {
                        if (!DryRun)
                        {
                            Directory.Delete(path, recursive: true);
                            context.Telemetry.DeletedCount++;
                        }

                        if (!Options.OmitPath)
                            WritePath(path, OperationKind.Delete, indent);
                    }
                    catch (Exception ex) when (ex is IOException
                        || ex is UnauthorizedAccessException)
                    {
                        WriteError(ex, path, indent: indent);
                    }
                }
            }

            foreach (string path in FileSystemHelpers.EnumerateAllFiles(Target))
            {
                if (_destinationPaths.Contains(path))
                    continue;

                string relativePath = path.Substring(Target.Length + 1);

                string sourcePath = Path.Combine(directoryPath, relativePath);

                if (!File.Exists(sourcePath))
                {
                    try
                    {
                        if (!DryRun)
                        {
                            File.Delete(path);
                            context.Telemetry.DeletedCount++;
                        }

                        if (!Options.OmitPath)
                            WritePath(path, OperationKind.Delete, indent);
                    }
                    catch (Exception ex) when (ex is IOException
                        || ex is UnauthorizedAccessException)
                    {
                        WriteError(ex, path, indent: indent);
                    }
                }
            }

            _destinationPaths = null;
        }
    }
}
