// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Orang.FileSystem;
using static Orang.CommandLine.LogHelpers;
using static Orang.Logger;

namespace Orang.CommandLine
{
    internal class SyncOperation : CommonCopyOperation
    {
        public SyncOperation(SyncCommandOptions options)
        {
            Options = options;
        }

        protected override CommonCopyCommandOptions CommonOptions => Options;

        new public SyncCommandOptions Options { get; }

        public override OperationKind Kind => OperationKind.Sync;

        protected override void ExecuteOperation(string sourcePath, string destinationPath)
        {
            File.Copy(sourcePath, destinationPath);
        }

        public void DeleteFilesAndDirectoriesInTarget(string directoryPath, string indent)
        {
            if (Options.TwoWay)
                return;

            foreach (string path in FileSystemHelpers.EnumerateAllDirectories(Target))
            {
                string relativePath = path.Substring(Target.Length + 1);

                string sourcePath = Path.Combine(directoryPath, relativePath);

                if (!Directory.Exists(sourcePath))
                {
                    try
                    {
                        Directory.Delete(path, recursive: true);

                        if (!Options.OmitPath)
                        {
                            Write("DELETE ", Verbosity.Minimal);
                            WritePath(path, indent: indent, verbosity: Verbosity.Minimal);
                            WriteLine(Verbosity.Minimal);
                        }
                    }
                    catch (Exception ex) when (ex is IOException
                        || ex is UnauthorizedAccessException)
                    {
                        WriteFileError(ex, path, indent: indent);
                    }
                }
            }

            foreach (string path in FileSystemHelpers.EnumerateAllFiles(Target))
            {
                string relativePath = path.Substring(Target.Length + 1);

                string sourcePath = Path.Combine(directoryPath, relativePath);

                if (!File.Exists(sourcePath))
                {
                    try
                    {
                        File.Delete(path);

                        if (!Options.OmitPath)
                        {
                            Write("DELETE ", Verbosity.Minimal);
                            WritePath(path, indent: indent, verbosity: Verbosity.Minimal);
                            WriteLine(Verbosity.Minimal);
                        }
                    }
                    catch (Exception ex) when (ex is IOException
                        || ex is UnauthorizedAccessException)
                    {
                        WriteFileError(ex, path, indent: indent);
                    }
                }
            }
        }
    }
}
