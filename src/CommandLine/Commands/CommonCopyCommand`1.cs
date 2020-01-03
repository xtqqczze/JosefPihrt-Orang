// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Orang.FileSystem;
using static Orang.Logger;

namespace Orang.CommandLine
{
    internal abstract class CommonCopyCommand<TOptions> : FindCommand<TOptions> where TOptions : CommonCopyCommandOptions
    {
        protected CommonCopyCommand(TOptions options) : base(options)
        {
        }

        public string Target => Options.Target;

        public TargetExistsAction TargetAction
        {
            get { return Options.TargetAction; }
            private set { Options.TargetAction = value; }
        }

        protected abstract void ExecuteOperation(string sourcePath, string destinationPath);

        protected override void ExecuteDirectory(string directoryPath, SearchContext context)
        {
            if (Options.TargetNormalized == null)
                Options.TargetNormalized = Target.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            string pathNormalized = directoryPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            if (Options.TargetNormalized.StartsWith(pathNormalized, StringComparison.OrdinalIgnoreCase)
                || pathNormalized.StartsWith(Options.TargetNormalized, StringComparison.OrdinalIgnoreCase))
            {
                WriteWarning("Source directory cannot be subdirectory of a destination directory or vice versa.");
                return;
            }

            base.ExecuteDirectory(directoryPath, context);
        }

        protected override void ExecuteResult(
            FileSystemFinderResult result,
            SearchContext context,
            ContentWriterOptions writerOptions,
            Match match,
            string input,
            Encoding encoding,
            string baseDirectoryPath = null,
            ColumnWidths columnWidths = null)
        {
            base.ExecuteResult(result, context, writerOptions, match, input, encoding, baseDirectoryPath, columnWidths);

            ExecuteOperation(result, context, baseDirectoryPath, GetPathIndent(baseDirectoryPath));
        }

        protected override void ExecuteResult(FileSystemFinderResult result, SearchContext context, string baseDirectoryPath = null, ColumnWidths columnWidths = null)
        {
            base.ExecuteResult(result, context, baseDirectoryPath, columnWidths);

            ExecuteOperation(result, context, baseDirectoryPath, GetPathIndent(baseDirectoryPath));
        }

        private void ExecuteOperation(
            FileSystemFinderResult result,
            SearchContext context,
            string baseDirectoryPath,
            string indent)
        {
            string sourcePath = result.Path;

            if (result.IsDirectory
                || baseDirectoryPath != null)
            {
                Debug.Assert(sourcePath.StartsWith(baseDirectoryPath, StringComparison.OrdinalIgnoreCase));

                string relativePath = sourcePath.Substring(baseDirectoryPath.Length + 1);

                string destinationPath = Path.Combine(Target, relativePath);

                ExecuteOperationAndCatchIfThrows(sourcePath, destinationPath);
            }
            else
            {
                string fileName = Path.GetFileName(sourcePath);

                string destinationPath = Path.Combine(Target, fileName);

                ExecuteOperationAndCatchIfThrows(sourcePath, destinationPath);
            }

            void ExecuteOperationAndCatchIfThrows(string sourcePath, string destinationPath)
            {
                try
                {
                    ExecuteOperation(context, sourcePath, destinationPath, result.IsDirectory, indent);
                }
                catch (Exception ex) when (ex is IOException
                    || ex is UnauthorizedAccessException)
                {
                    LogHelpers.WriteFileError(ex, sourcePath, relativePath: Options.DisplayRelativePath, indent: indent);
                }
            }
        }

        private void ExecuteOperation(SearchContext context, string sourcePath, string destinationPath, bool isDirectory, string indent)
        {
            bool overwrite = false;
            bool pathWritten = false;
            bool fileExists = File.Exists(destinationPath);

            switch (TargetAction)
            {
                case TargetExistsAction.Ask:
                    {
                        if (fileExists
                            && IsOperationRequired())
                        {
                            if (!Options.OmitPath)
                            {
                                WritePath(destinationPath, (isDirectory) ? OperationKind.Add : OperationKind.Update, indent);
                                pathWritten = true;
                            }

                            DialogResult dialogResult = ConsoleHelpers.QuestionWithResult("Overwrite file?", indent);
                            switch (dialogResult)
                            {
                                case DialogResult.Yes:
                                    {
                                        overwrite = true;
                                        break;
                                    }
                                case DialogResult.YesToAll:
                                    {
                                        overwrite = true;
                                        TargetAction = TargetExistsAction.Overwrite;
                                        break;
                                    }
                                case DialogResult.No:
                                case DialogResult.None:
                                    {
                                        return;
                                    }
                                case DialogResult.NoToAll:
                                    {
                                        TargetAction = TargetExistsAction.Skip;
                                        return;
                                    }
                                case DialogResult.Cancel:
                                    {
                                        context.State = SearchState.Canceled;
                                        return;
                                    }
                                default:
                                    {
                                        throw new InvalidOperationException($"Unknown enum value '{dialogResult}'.");
                                    }
                            }
                        }

                        break;
                    }
                case TargetExistsAction.Overwrite:
                    {
                        if (fileExists
                            && IsOperationRequired())
                        {
                            overwrite = true;
                        }

                        break;
                    }
                case TargetExistsAction.Skip:
                    {
                        if (fileExists)
                            return;

                        break;
                    }
                default:
                    {
                        throw new InvalidOperationException($"Unknown enum value '{TargetAction}'.");
                    }
            }

            if (isDirectory)
            {
                bool directoryExists = Directory.Exists(destinationPath);

                if (overwrite
                    || !directoryExists)
                {
                    if (!Options.OmitPath
                        && !pathWritten)
                    {
                        WritePath(destinationPath, OperationKind.Add, indent);
                    }

                    if (!Options.DryRun)
                    {
                        if (overwrite)
                            File.Delete(destinationPath);

                        if (!directoryExists)
                            Directory.CreateDirectory(destinationPath);

                        context.Telemetry.ProcessedDirectoryCount++;
                    }
                }
            }
            else
            {
                if (!Options.OmitPath
                    && !pathWritten)
                {
                    WritePath(destinationPath, (fileExists) ? OperationKind.Update : OperationKind.Add, indent);
                }

                if (!Options.DryRun)
                {
                    if (overwrite)
                    {
                        File.Delete(destinationPath);
                    }
                    else if (!Directory.Exists(destinationPath))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                    }

                    ExecuteOperation(sourcePath, destinationPath);

                    context.Telemetry.ProcessedFileCount++;
                }
            }

            bool IsOperationRequired()
            {
                return isDirectory
                    || Options.CompareOptions == FileCompareOptions.None
                    || !FileEquals(sourcePath, destinationPath);
            }
        }

        protected virtual void WritePath(string path, OperationKind kind, string indent)
        {
            LogHelpers.WritePath(path, indent: indent, verbosity: Verbosity.Minimal);
            WriteLine(Verbosity.Minimal);
        }

        private bool FileEquals(string path1, string path2)
        {
            if (Options.CompareModifiedTime
                && File.GetLastWriteTimeUtc(path1) != File.GetLastWriteTimeUtc(path2))
            {
                return false;
            }

            if (Options.CompareAttributes
                && File.GetAttributes(path1) != File.GetAttributes(path2))
            {
                return false;
            }

            if (Options.CompareSize || Options.CompareContent)
            {
                using (var fs1 = new FileStream(path1, FileMode.Open, FileAccess.Read))
                using (var fs2 = new FileStream(path2, FileMode.Open, FileAccess.Read))
                {
                    if (Options.CompareSize
                        && fs1.Length != fs2.Length)
                    {
                        return false;
                    }

                    if (Options.CompareContent
                        && !StreamComparer.Default.ByteEquals(fs1, fs2))
                    {
                        return false;
                    }
                }
            }

            return true;
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
