// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using Orang.FileSystem;
using static Orang.CommandLine.LogHelpers;
using static Orang.Logger;

namespace Orang.CommandLine
{
    internal class SyncCommand : CommonCopyCommand<SyncCommandOptions>
    {
        public SyncCommand(SyncCommandOptions options) : base(options)
        {
        }

        public override bool CanWriteContent => false;

        protected override bool CanExecuteOperation(string sourcePath, string destinationPath)
        {
            return !FileSystemHelpers.FileEquals(sourcePath, destinationPath, Options.CompareOptions);
        }

        protected override void ExecuteOperation(string sourcePath, string destinationPath)
        {
            File.Copy(sourcePath, destinationPath);
        }

        protected override void ExecuteFile(string filePath, SearchContext context)
        {
            throw new InvalidOperationException("File cannot be synchronized.");
        }

        protected override void ExecuteDirectory(string directoryPath, SearchContext context)
        {
            base.ExecuteDirectory(directoryPath, context);

            if (Options.TwoWay)
            {
                string target = directoryPath;
                directoryPath = Target;

                Options.Paths = ImmutableArray.Create(directoryPath);
                Options.Target = target;

                base.ExecuteDirectory(directoryPath, context);
            }
            else
            {
                string indent = GetPathIndent(directoryPath);

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
                                LogHelpers.WritePath(path, indent: indent, verbosity: Verbosity.Minimal);
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
                                LogHelpers.WritePath(path, indent: indent, verbosity: Verbosity.Minimal);
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

        protected override void WritePath(string sourcePath, string destinationPath, string indent)
        {
            Write("COPY   ", Verbosity.Minimal);
            LogHelpers.WritePath(sourcePath, indent: indent, verbosity: Verbosity.Minimal);
            WriteLine(Verbosity.Minimal);
            Write("       ", Verbosity.Minimal);
            LogHelpers.WritePath(destinationPath, indent: indent, verbosity: Verbosity.Minimal);
            WriteLine(Verbosity.Minimal);
        }

        protected override void WritePath(SearchContext context, FileSystemFinderResult result, string baseDirectoryPath, string indent, ColumnWidths columnWidths)
        {
        }

        protected override void WriteSummary(SearchTelemetry telemetry, Verbosity verbosity)
        {
            //TODO: SyncCommand.WriteSummary
        }
    }
}
