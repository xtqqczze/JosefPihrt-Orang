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

        public OverwriteOption OverwriteOption
        {
            get { return Options.OverwriteOption; }
            private set { Options.OverwriteOption = value; }
        }

        protected abstract void ExecuteOperation(string sourcePath, string destinationPath);

        protected virtual bool CanExecuteOperation(string sourcePath, string destinationPath) => true;

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

            switch (OverwriteOption)
            {
                case OverwriteOption.Ask:
                    {
                        if (fileExists)
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
                                        OverwriteOption = OverwriteOption.Yes;
                                        break;
                                    }
                                case DialogResult.No:
                                case DialogResult.None:
                                    {
                                        return;
                                    }
                                case DialogResult.NoToAll:
                                    {
                                        OverwriteOption = OverwriteOption.No;
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
                case OverwriteOption.Yes:
                    {
                        if (fileExists)
                            overwrite = true;

                        break;
                    }
                case OverwriteOption.No:
                    {
                        if (fileExists)
                            return;

                        break;
                    }
                default:
                    {
                        throw new InvalidOperationException($"Unknown enum value '{OverwriteOption}'.");
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
            else if (!fileExists
                || CanExecuteOperation(sourcePath, destinationPath))
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
        }

        protected virtual void WritePath(string path, OperationKind kind, string indent)
        {
            LogHelpers.WritePath(path, indent: indent, verbosity: Verbosity.Minimal);
            WriteLine(Verbosity.Minimal);
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
