// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Orang.FileSystem;
using static Orang.CommandLine.LogHelpers;
using static Orang.Logger;

namespace Orang.CommandLine
{
    internal abstract class CommonSyncCommand : CommonCopyCommand<SyncCommandOptions>
    {
        protected HashSet<string> _destinationPaths;

        protected CommonSyncCommand(SyncCommandOptions options) : base(options)
        {
        }

        public bool DryRun => Options.DryRun;

        public sealed override bool CanWriteContent => false;

        protected void ExecuteOperations(
            SearchContext context,
            string sourcePath,
            string destinationPath,
            bool isDirectory,
            bool fileExists,
            bool directoryExists,
            bool preferTarget,
            string indent)
        {
            SearchTelemetry telemetry = context.Telemetry;

            if (isDirectory)
            {
                if (preferTarget)
                {
                    if (directoryExists)
                    {
                        WritePath(sourcePath, OperationKind.Update, indent);
                        UpdateAttributes(destinationPath, sourcePath);
                        telemetry.UpdatedCount++;
                    }
                    else
                    {
                        WritePath(sourcePath, OperationKind.Delete, indent);
                        DeleteDirectory(sourcePath);
                        telemetry.DeletedCount++;

                        if (fileExists)
                        {
                            WritePath(sourcePath, OperationKind.Add, indent);
                            CopyFile(destinationPath, sourcePath);
                            telemetry.AddedCount++;
                        }
                    }
                }
                else if (directoryExists)
                {
                    WritePath(destinationPath, OperationKind.Update, indent);
                    UpdateAttributes(sourcePath, destinationPath);
                    telemetry.UpdatedCount++;
                }
                else
                {
                    if (fileExists)
                    {
                        WritePath(destinationPath, OperationKind.Delete, indent);
                        DeleteFile(destinationPath);
                        telemetry.DeletedCount++;
                    }

                    WritePath(destinationPath, OperationKind.Add, indent);
                    CreateDirectory(destinationPath);
                    telemetry.AddedCount++;
                }
            }
            else if (preferTarget)
            {
                WritePath(sourcePath, (fileExists) ? OperationKind.Update : OperationKind.Delete, indent);
                DeleteFile(sourcePath);

                if (!fileExists)
                    telemetry.DeletedCount++;

                if (fileExists)
                {
                    CopyFile(destinationPath, sourcePath);
                    telemetry.UpdatedCount++;
                }
                else if (directoryExists)
                {
                    WritePath(sourcePath, OperationKind.Add, indent);
                    CreateDirectory(sourcePath);
                    telemetry.AddedCount++;
                }
            }
            else
            {
                if (fileExists)
                {
                    WritePath(destinationPath, OperationKind.Update, indent);
                    DeleteFile(destinationPath);
                }
                else if (directoryExists)
                {
                    WritePath(destinationPath, OperationKind.Delete, indent);
                    DeleteDirectory(destinationPath);
                    telemetry.DeletedCount++;
                }

                if (!fileExists)
                    WritePath(destinationPath, OperationKind.Add, indent);

                CopyFile(sourcePath, destinationPath);

                if (fileExists)
                {
                    telemetry.UpdatedCount++;
                }
                else
                {
                    telemetry.AddedCount++;
                }
            }

            _destinationPaths?.Add(destinationPath);

            void DeleteDirectory(string path)
            {
                if (!DryRun)
                    Directory.Delete(path, recursive: true);
            }

            void CreateDirectory(string path)
            {
                if (!DryRun)
                    Directory.CreateDirectory(path);
            }

            void DeleteFile(string path)
            {
                if (!DryRun)
                    File.Delete(path);
            }

            void CopyFile(string sourcePath, string destinationPath)
            {
                if (!DryRun)
                    File.Copy(sourcePath, destinationPath);
            }

            void UpdateAttributes(string sourcePath, string destinationPath)
            {
                if (!DryRun)
                    FileSystemHelpers.UpdateAttributes(sourcePath, destinationPath);
            }
        }

        protected override void ExecuteOperation(string sourcePath, string destinationPath)
        {
            File.Copy(sourcePath, destinationPath);
        }

        protected override void ExecuteFile(string filePath, SearchContext context)
        {
            throw new InvalidOperationException("File cannot be synchronized.");
        }

        protected void WritePath(string path, OperationKind kind, string indent)
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

        protected void WritePathPrefix(string path, string prefix, ConsoleColors colors, string indent)
        {
            Write(indent, Verbosity.Minimal);
            Write(prefix, colors, Verbosity.Minimal);
            Write(" ", Verbosity.Minimal);
            LogHelpers.WritePath(path, verbosity: Verbosity.Minimal);
            WriteLine(Verbosity.Minimal);
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

            ConsoleColors colors = (Options.DryRun) ? Colors.Message_DryRun : default;

            WriteCount("Added", telemetry.AddedCount, colors, verbosity: verbosity);
            Write("  ", verbosity);
            WriteCount("Updated", telemetry.UpdatedCount, colors, verbosity: verbosity);
            Write("  ", verbosity);
            WriteCount("Deleted", telemetry.DeletedCount, colors, verbosity: verbosity);
            Write("  ", verbosity);
            WriteLine(verbosity);
        }

        protected enum OperationKind
        {
            None,
            Add,
            Update,
            Delete
        }
    }
}
