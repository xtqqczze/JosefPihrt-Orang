// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
            protected set { Options.TargetAction = value; }
        }

        protected HashSet<string> IgnoredPaths { get; set; }

        protected abstract void ExecuteOperation(string sourcePath, string destinationPath);

        protected override void ExecuteDirectory(string directoryPath, SearchContext context)
        {
            if (Options.TargetNormalized == null)
                Options.TargetNormalized = Target.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            string pathNormalized = directoryPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            if (FileSystemHelpers.IsSubdirectory(Options.TargetNormalized, pathNormalized)
                || FileSystemHelpers.IsSubdirectory(pathNormalized, Options.TargetNormalized))
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
                Debug.Assert(sourcePath.StartsWith(baseDirectoryPath, FileSystemHelpers.Comparison));

                string relativePath = sourcePath.Substring(baseDirectoryPath.Length + 1);

                destinationPath = Path.Combine(Target, relativePath);
            }
            else
            {
                string fileName = Path.GetFileName(sourcePath);

                destinationPath = Path.Combine(Target, fileName);
            }

            if (IgnoredPaths?.Contains(sourcePath) == true)
                return;

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

        protected virtual void ExecuteOperation(SearchContext context, string sourcePath, string destinationPath, bool isDirectory, string indent)
        {
            bool fileExists = File.Exists(destinationPath);
            bool directoryExists = !fileExists && Directory.Exists(destinationPath);
            bool ask = false;

            if (isDirectory)
            {
                if (fileExists)
                {
                    ask = true;
                }
                else if (directoryExists)
                {
                    if (File.GetAttributes(sourcePath) == File.GetAttributes(destinationPath))
                        return;

                    ask = true;
                }
            }
            else if (fileExists)
            {
                if (Options.CompareOptions != FileCompareOptions.None
                    && FileSystemHelpers.FileEquals(sourcePath, destinationPath, Options.CompareOptions))
                {
                    return;
                }

                ask = true;
            }
            else if (directoryExists)
            {
                ask = true;
            }

            if (ask
                && TargetAction == TargetExistsAction.Skip)
            {
                return;
            }

            if (!Options.OmitPath)
                WritePath(destinationPath, indent);

            if (ask
                && TargetAction == TargetExistsAction.Ask)
            {
                string question;
                if (directoryExists)
                {
                    question = (isDirectory) ? "Update directory attributes?" : "Overwrite directory?";
                }
                else
                {
                    question = "Overwrite file?";
                }

                DialogResult dialogResult = ConsoleHelpers.QuestionWithResult(question, indent);
                switch (dialogResult)
                {
                    case DialogResult.Yes:
                        {
                            break;
                        }
                    case DialogResult.YesToAll:
                        {
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

            if (!isDirectory
                && fileExists
                && TargetAction == TargetExistsAction.Rename)
            {
                destinationPath = CreateNewFile(destinationPath);
            }

            if (isDirectory)
            {
                if (directoryExists)
                {
                    UpdateAttributes(sourcePath, destinationPath);
                }
                else
                {
                    if (fileExists)
                        DeleteFile(destinationPath);

                    CreateDirectory(destinationPath);
                }

                context.Telemetry.ProcessedDirectoryCount++;
            }
            else
            {
                if (fileExists)
                {
                    DeleteFile(destinationPath);
                }
                else if (directoryExists)
                {
                    DeleteDirectory(destinationPath);
                }
                else if (!Options.DryRun)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                }

                if (!Options.DryRun)
                    ExecuteOperation(sourcePath, destinationPath);

                context.Telemetry.ProcessedFileCount++;
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

            void UpdateAttributes(string sourcePath, string destinationPath)
            {
                if (!Options.DryRun)
                    FileSystemHelpers.UpdateAttributes(sourcePath, destinationPath);
            }

            static string CreateNewFile(string path)
            {
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

        private void WritePath(string path, string indent)
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
    }
}
