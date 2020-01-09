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

        private static readonly ImmutableDictionary<string, DialogResult> _dialogResultMap = CreateDialogResultMap();

        private static ImmutableDictionary<string, DialogResult> CreateDialogResultMap()
        {
            ImmutableDictionary<string, DialogResult>.Builder builder = ImmutableDictionary.CreateBuilder<string, DialogResult>();

            builder.Add("s", DialogResult.Source);
            builder.Add("sa", DialogResult.AlwaysSource);
            builder.Add("t", DialogResult.Target);
            builder.Add("ta", DialogResult.AlwaysTarget);
            builder.Add("c", DialogResult.Cancel);

            return builder.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);
        }

        public SyncCommand(SyncCommandOptions options) : base(options)
        {
        }

        public override bool CanWriteContent => false;

        public SyncPreference SyncPreference
        {
            get { return Options.SyncPreference; }
            private set { Options.SyncPreference = value; }
        }

        protected override void ExecuteOperation(SearchContext context, string sourcePath, string destinationPath, bool isDirectory, string indent)
        {
            if (Options.SyncMode == SyncMode.Synchronize)
            {
                Synchronize(context, sourcePath, destinationPath, isDirectory, indent);
            }
            else
            {
                base.ExecuteOperation(context, sourcePath, destinationPath, isDirectory, indent);
            }
        }

        private void Synchronize(SearchContext context, string sourcePath, string destinationPath, bool isDirectory, string indent)
        {
            bool fileExists = File.Exists(destinationPath);
            bool directoryExists = !fileExists && Directory.Exists(destinationPath);

            bool? preferTarget = null;

            if (isDirectory)
            {
                if (directoryExists
                    && File.GetAttributes(sourcePath) == File.GetAttributes(destinationPath))
                {
                    return;
                }
            }
            else if (Options.PreferNewer
                && fileExists)
            {
                //TODO: inline
                DateTime dt1 = File.GetLastWriteTimeUtc(sourcePath);
                DateTime dt2 = File.GetLastWriteTimeUtc(destinationPath);

                int diff = dt1.CompareTo(dt2);

                if (diff > 0)
                {
                    preferTarget = false;
                }
                else if (diff < 0)
                {
                    preferTarget = true;
                }
            }


            if (preferTarget == null)
            {
                if (SyncPreference == SyncPreference.Ask)
                {
                    if (isDirectory)
                    {
                        if (directoryExists
                            && File.GetAttributes(sourcePath) == File.GetAttributes(destinationPath))
                        {
                            return;
                        }
                    }
                    else if (fileExists)
                    {
                        if (FileEquals())
                            return;
                    }

                    WritePathPrefix(sourcePath, "S  ", default, indent);
                    WritePathPrefix(destinationPath, "T  ", default, indent);

                    DialogResult dialogResult = GetDialogResult(indent);

                    switch (dialogResult)
                    {
                        case DialogResult.None:
                        case DialogResult.Source:
                            {
                                preferTarget = false;
                                break;
                            }
                        case DialogResult.AlwaysSource:
                            {
                                preferTarget = false;
                                SyncPreference = SyncPreference.Source;
                                break;
                            }
                        case DialogResult.Target:
                            {
                                preferTarget = true;
                                break;
                            }
                        case DialogResult.AlwaysTarget:
                            {
                                preferTarget = true;
                                SyncPreference = SyncPreference.Target;
                                break;
                            }
                        case DialogResult.Cancel:
                            {
                                context.TerminationReason = TerminationReason.Canceled;
                                return;
                            }
                        default:
                            {
                                throw new InvalidOperationException($"Unknown enum value '{dialogResult}'.");
                            }
                    }
                }
                else if (SyncPreference == SyncPreference.Source)
                {
                    preferTarget = false;
                }
                else if (SyncPreference == SyncPreference.Target)
                {
                    preferTarget = true;
                }
                else
                {
                    throw new InvalidOperationException($"Unknown enum value '{Options.SyncPreference}'.");
                }
            }

            if (isDirectory)
            {
                if (preferTarget == true)
                {
                    if (directoryExists)
                    {
                        WritePath(sourcePath, OperationKind.Update, indent);
                        FileSystemHelpers.UpdateAttributes(destinationPath, sourcePath);
                        context.Telemetry.UpdatedCount++;
                    }
                    else
                    {
                        WritePath(sourcePath, OperationKind.Delete, indent);
                        DeleteDirectory(sourcePath);
                        context.Telemetry.DeletedCount++;

                        if (fileExists)
                        {
                            WritePath(sourcePath, OperationKind.Add, indent);
                            CopyFile(destinationPath, sourcePath);
                            context.Telemetry.AddedCount++;
                        }
                    }
                }
                else if (directoryExists)
                {
                    WritePath(destinationPath, OperationKind.Update, indent);
                    FileSystemHelpers.UpdateAttributes(sourcePath, destinationPath);
                    context.Telemetry.UpdatedCount++;
                }
                else
                {
                    if (fileExists)
                    {
                        WritePath(destinationPath, OperationKind.Delete, indent);
                        DeleteFile(destinationPath);
                        context.Telemetry.DeletedCount++;
                    }

                    WritePath(destinationPath, OperationKind.Add, indent);
                    CreateDirectory(destinationPath);
                    context.Telemetry.AddedCount++;
                }
            }
            else if (preferTarget == true)
            {
                WritePath(sourcePath, (fileExists) ? OperationKind.Update : OperationKind.Delete, indent);
                DeleteFile(sourcePath);

                if (!fileExists)
                    context.Telemetry.DeletedCount++;

                if (fileExists)
                {
                    CopyFile(destinationPath, sourcePath);
                    context.Telemetry.UpdatedCount++;
                }
                else if (directoryExists)
                {
                    WritePath(sourcePath, OperationKind.Add, indent);
                    CreateDirectory(sourcePath);
                    context.Telemetry.AddedCount++;
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
                    context.Telemetry.DeletedCount++;
                }

                if (!fileExists)
                    WritePath(destinationPath, OperationKind.Add, indent);

                CopyFile(sourcePath, destinationPath);

                if (fileExists)
                {
                    context.Telemetry.UpdatedCount++;
                }
                else
                {
                    context.Telemetry.AddedCount++;
                }
            }

            _destinationPaths?.Add(destinationPath);

            bool FileEquals()
            {
                return Options.CompareOptions != FileCompareOptions.None
                    && FileSystemHelpers.FileEquals(sourcePath, destinationPath, Options.CompareOptions);
            }

            void DeleteDirectory(string path)
            {
                if (!Options.DryRun)
                    Directory.Delete(path, recursive: true);
            }

            void CreateDirectory(string path)
            {
                if (!Options.DryRun)
                    Directory.CreateDirectory(path);
            }

            void DeleteFile(string path)
            {
                if (!Options.DryRun)
                    File.Delete(path);
            }

            void CopyFile(string sourcePath, string destinationPath)
            {
                if (!Options.DryRun)
                    File.Copy(sourcePath, destinationPath);
            }

            static void UpdateAttributes(string sourcePath, string destinationPath)
            {
                FileSystemHelpers.UpdateAttributes(sourcePath, destinationPath);
            }
        }

        protected override void ExecuteOperation(string sourcePath, string destinationPath)
        {
            Debug.Assert(Options.SyncMode != SyncMode.Synchronize);

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

            if (Options.SyncMode == SyncMode.Synchronize)
            {
                IgnoredPaths = _destinationPaths;
                _destinationPaths = null;

                string target = directoryPath;
                directoryPath = Target;

                Options.Paths = ImmutableArray.Create(directoryPath);
                Options.Target = target;

                //TODO: prohodit SyncPreference
                base.ExecuteDirectory(directoryPath, context);

                IgnoredPaths = null;
            }
            else if (Options.SyncMode == SyncMode.Echo)
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
            else
            {
                Debug.Assert(Options.SyncMode == SyncMode.Contribute, Options.SyncMode.ToString());
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

        private DialogResult GetDialogResult(string indent)
        {
            ConsoleOut.Write(indent);
            ConsoleOut.Write("Prefer source or target?  (S[A]/T[A]/C):");

            string s = Console.ReadLine()?.Trim();

            if (s != null)
            {
                if (_dialogResultMap.TryGetValue(s, out DialogResult dialogResult))
                    return dialogResult;
            }
            else
            {
                ConsoleOut.WriteLine();
            }

            return DialogResult.None;
        }

        private enum DialogResult
        {
            None = 0,
            Source = 1,
            AlwaysSource = 2,
            Target = 3,
            AlwaysTarget = 4,
            Cancel = 5,
        }
    }
}
