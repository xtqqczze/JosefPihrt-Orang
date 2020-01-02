﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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

            Execute(result, context, baseDirectoryPath, GetPathIndent(baseDirectoryPath));
        }

        protected override void ExecuteResult(FileSystemFinderResult result, SearchContext context, string baseDirectoryPath = null, ColumnWidths columnWidths = null)
        {
            base.ExecuteResult(result, context, baseDirectoryPath, columnWidths);

            Execute(result, context, baseDirectoryPath, GetPathIndent(baseDirectoryPath));
        }

        public void Execute(
            FileSystemFinderResult result,
            SearchContext context,
            string baseDirectoryPath,
            string indent)
        {
            string sourcePath = result.Path;

            if (result.IsDirectory)
            {
                foreach (string filePath in FileSystemHelpers.EnumerateAllFiles(sourcePath))
                {
                    Execute(filePath);

                    if (context.State == SearchState.Canceled)
                        return;
                }
            }
            else if (baseDirectoryPath != null)
            {
                Execute(sourcePath);
            }
            else
            {
                string fileName = Path.GetFileName(sourcePath);

                string destinationPath = Path.Combine(Target, fileName);

                ExecuteOperationAndCatchIfThrows(sourcePath, destinationPath);
            }

            void Execute(string path)
            {
                Debug.Assert(path.StartsWith(baseDirectoryPath, StringComparison.OrdinalIgnoreCase));

                string relativePath = path.Substring(baseDirectoryPath.Length + 1);

                string destinationPath = Path.Combine(Target, relativePath);

                ExecuteOperationAndCatchIfThrows(path, destinationPath);
            }

            void ExecuteOperationAndCatchIfThrows(string sourcePath, string destinationPath)
            {
                try
                {
                    ExecuteOperation(context, sourcePath, destinationPath, indent);
                }
                catch (Exception ex) when (ex is IOException
                    || ex is UnauthorizedAccessException)
                {
                    LogHelpers.WriteFileError(ex, sourcePath, relativePath: Options.DisplayRelativePath, indent: indent);
                }
            }
        }

        private void ExecuteOperation(SearchContext context, string sourcePath, string destinationPath, string indent)
        {
            bool overwrite = false;
            bool pathWritten = false;
            bool exists = FileSystemHelpers.FileOrDirectoryExists(destinationPath);

            switch (OverwriteOption)
            {
                case OverwriteOption.Ask:
                    {
                        if (exists)
                        {
                            if (!Options.OmitPath)
                            {
                                WritePath(sourcePath, destinationPath, indent);
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
                        if (exists)
                            overwrite = true;

                        break;
                    }

                case OverwriteOption.No:
                    {
                        if (exists)
                            return;

                        break;
                    }

                default:
                    {
                        throw new InvalidOperationException($"Unknown enum value '{OverwriteOption}'.");
                    }
            }

            if (Options.DryRun)
            {
                if (overwrite)
                {
                    File.Delete(destinationPath);
                }
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                }
            }

            if (!exists
                || CanExecuteOperation(sourcePath, destinationPath))
            {
                if (!Options.OmitPath
                    && !pathWritten)
                {
                    WritePath(sourcePath, destinationPath, indent);
                }

                if (!Options.DryRun)
                {
                    ExecuteOperation(sourcePath, destinationPath);

                    context.Telemetry.ProcessedFileCount++;
                }
            }
        }

        protected virtual void WritePath(string sourcePath, string destinationPath, string indent)
        {
            LogHelpers.WritePath(destinationPath, indent: indent, verbosity: Verbosity.Minimal);
            WriteLine(Verbosity.Minimal);
        }
    }
}
