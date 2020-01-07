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
            string destinationPath;

            if (result.IsDirectory
                || (baseDirectoryPath != null && !Options.Flat))
            {
                Debug.Assert(sourcePath.StartsWith(baseDirectoryPath, StringComparison.OrdinalIgnoreCase));

                string relativePath = sourcePath.Substring(baseDirectoryPath.Length + 1);

                destinationPath = Path.Combine(Target, relativePath);
            }
            else
            {
                string fileName = Path.GetFileName(sourcePath);

                destinationPath = Path.Combine(Target, fileName);
            }

            try
            {
                ExecuteOperation(context, sourcePath, destinationPath, result.IsDirectory, indent);
            }
            catch (Exception ex) when (ex is IOException
                || ex is UnauthorizedAccessException)
            {
                WriteError(ex, sourcePath, indent: indent);
            }
        }

        private void ExecuteOperation(SearchContext context, string sourcePath, string destinationPath, bool isDirectory, string indent)
        {
            bool overwrite = false;
            bool pathWritten = false;
            bool fileExists = File.Exists(destinationPath);
            bool directoryExists = (fileExists) ? false : Directory.Exists(destinationPath);

            switch (TargetAction)
            {
                case TargetExistsAction.Ask:
                    {
                        if (fileExists || (!isDirectory && directoryExists))
                        {
                            if (!IsOperationRequired())
                                return;

                            if (!Options.OmitPath)
                            {
                                WritePath(destinationPath, (isDirectory) ? OperationKind.Add : OperationKind.Update, indent);
                                pathWritten = true;
                            }

                            string question = (!isDirectory && directoryExists) ? "Overwrite directory?" : "Overwrite file?";

                            DialogResult dialogResult = ConsoleHelpers.QuestionWithResult(question, indent);
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
                                        context.TerminationReason = TerminationReason.Canceled;
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
                        if (fileExists || (!isDirectory && directoryExists))
                        {
                            if (IsOperationRequired())
                            {
                                overwrite = true;
                            }
                            else
                            {
                                return;
                            }
                        }

                        break;
                    }
                case TargetExistsAction.Skip:
                    {
                        if (fileExists || (!isDirectory && directoryExists))
                            return;

                        break;
                    }
                case TargetExistsAction.Rename:
                    {
                        break;
                    }
                default:
                    {
                        throw new InvalidOperationException($"Unknown enum value '{TargetAction}'.");
                    }
            }

            if (isDirectory)
            {
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

                        Directory.CreateDirectory(destinationPath);

                        context.Telemetry.AddedCount++;
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
                        if (fileExists)
                        {
                            File.Delete(destinationPath);
                        }
                        else
                        {
                            Directory.Delete(destinationPath, recursive: true);
                        }
                    }
                    else if (!Directory.Exists(destinationPath))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                    }

                    if (Options.TargetAction == TargetExistsAction.Rename)
                        destinationPath = CreateNewFile(destinationPath);

                    ExecuteOperation(sourcePath, destinationPath);

                    if (fileExists)
                    {
                        context.Telemetry.UpdatedCount++;
                    }
                    else
                    {
                        context.Telemetry.AddedCount++;
                    }
                }
            }

            bool IsOperationRequired()
            {
                return isDirectory
                    || Options.CompareOptions == FileCompareOptions.None
                    || !FileEquals(sourcePath, destinationPath);
            }

            static string CreateNewFile(string path)
            {
                if (!File.Exists(path))
                    return path;

                int count = 2;
                int extensionIndex = FileSystemHelpers.GetExtensionIndex(path);

                if (extensionIndex > 0
                    && FileSystemHelpers.IsDirectorySeparator(path[extensionIndex - 1]))
                {
                    extensionIndex = path.Length;
                }

                string newPath;

                do
                {
                    newPath = path.Insert(extensionIndex, count.ToString());

                    count++;

                } while (File.Exists(newPath));

                return newPath;
            }
        }

        protected virtual void WritePath(string path, OperationKind kind, string indent)
        {
            LogHelpers.WritePath(path, indent: indent, verbosity: Verbosity.Minimal);
            WriteLine(Verbosity.Minimal);
        }

        protected virtual void WriteError(
            Exception ex,
            string path,
            string indent)
        {
            LogHelpers.WriteFileError(ex, path, relativePath: Options.DisplayRelativePath, indent: indent);
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
