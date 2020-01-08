// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using Orang.FileSystem;
using static Orang.CommandLine.LogHelpers;
using static Orang.Logger;

namespace Orang.CommandLine
{
    internal class SyncCommand : CommonCopyCommand<SyncCommandOptions>
    {
        private HashSet<string> _destinationPaths;

        public SyncCommand(SyncCommandOptions options) : base(options)
        {
        }

        public override bool CanWriteContent => false;

        protected override void ExecuteOperation(SearchContext context, string sourcePath, string destinationPath, bool isDirectory, string indent)
        {
            base.ExecuteOperation(context, sourcePath, destinationPath, isDirectory, indent);

            _destinationPaths?.Add(destinationPath);
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
            _destinationPaths = new HashSet<string>(FileSystemHelpers.Comparer);

            base.ExecuteDirectory(directoryPath, context);

            if (Options.TwoWay)
            {
                IgnoredPaths = _destinationPaths;
                _destinationPaths = null;

                string target = directoryPath;
                directoryPath = Target;

                Options.Paths = ImmutableArray.Create(directoryPath);
                Options.Target = target;

                base.ExecuteDirectory(directoryPath, context);

                IgnoredPaths = null;
            }
            else
            {
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
                            if (!Options.DryRun)
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
                            if (!Options.DryRun)
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

        protected override void WritePath(string path, OperationKind kind, string indent)
        {
            if (!ShouldLog(Verbosity.Minimal))
                return;

            switch (kind)
            {
                case OperationKind.None:
                    {
                        Debug.Fail("");

                        LogHelpers.WritePath(path, indent: indent, verbosity: Verbosity.Minimal);
                        WriteLine(Verbosity.Minimal);
                        break;
                    }
                case OperationKind.Add:
                    {
                        WritePathPrefix(path, "ADD", Colors.Sync_Add, indent);
                        break;
                    }
                case OperationKind.Update:
                    {
                        WritePathPrefix(path, "UPD", Colors.Sync_Update, indent);
                        break;
                    }
                case OperationKind.Delete:
                    {
                        WritePathPrefix(path, "DEL", Colors.Sync_Delete, indent);
                        break;
                    }
                default:
                    {
                        throw new ArgumentException($"Unkonwn enum value '{kind}'.", nameof(kind));
                    }
            }
        }

        private void WritePathPrefix(string path, string prefix, ConsoleColors colors, string indent)
        {
            Write(indent, Verbosity.Minimal);
            Write(prefix, colors, Verbosity.Minimal);
            Write(" ", Verbosity.Minimal);
            LogHelpers.WritePath(path, verbosity: Verbosity.Minimal);
            WriteLine(Verbosity.Minimal);
        }

        protected override void WritePath(SearchContext context, FileSystemFinderResult result, string baseDirectoryPath, string indent, ColumnWidths columnWidths)
        {
        }

        protected override void WriteError(Exception ex, string path, string indent)
        {
            Write(indent, Verbosity.Minimal);
            Write("ERR", Colors.Sync_Error, Verbosity.Minimal);
            Write(" ", Verbosity.Minimal);
            Write(ex.Message, verbosity: Verbosity.Minimal);
            WriteLine(Verbosity.Minimal);
#if DEBUG
            WriteLine($"{indent}STACK TRACE:");
            WriteLine(ex.StackTrace);
#endif
        }

        protected override void WriteSummary(SearchTelemetry telemetry, Verbosity verbosity)
        {
            base.WriteSummary(telemetry, verbosity);

            WriteCount("Added", telemetry.AddedCount, verbosity: verbosity);
            Write("  ", verbosity);
            WriteCount("Updated", telemetry.UpdatedCount, verbosity: verbosity);
            Write("  ", verbosity);
            WriteCount("Deleted", telemetry.DeletedCount, verbosity: verbosity);
            Write("  ", verbosity);

            WriteLine(verbosity);
        }
    }
}
