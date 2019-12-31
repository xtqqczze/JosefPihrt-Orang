// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using Orang.FileSystem;

namespace Orang.CommandLine
{
    internal abstract class CommonCopyOperation
    {
        protected CommonCopyOperation()
        {
        }

        protected abstract CommonCopyCommandOptions CommonOptions { get; }

        public CommonCopyCommandOptions Options => CommonOptions;

        public string Target => Options.Target;

        public OverwriteOption OverwriteOption
        {
            get { return Options.OverwriteOption; }
            private set { Options.OverwriteOption = value; }
        }

        protected abstract void ExecuteOperation(string sourcePath, string destinationPath);

        public bool CanExecute(string directoryPath)
        {
            if (Options.TargetNormalized == null)
                Options.TargetNormalized = Target.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            string pathNormalized = directoryPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            if (Options.TargetNormalized.StartsWith(pathNormalized, StringComparison.OrdinalIgnoreCase)
                || pathNormalized.StartsWith(Options.TargetNormalized, StringComparison.OrdinalIgnoreCase))
            {
                Logger.WriteWarning("Source directory cannot be subdirectory of a destination directory or vice versa.");
                return false;
            }

            return true;
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

                ExecuteOperationAndCatchIfThrows(context, sourcePath, destinationPath, indent);
            }

            void Execute(string path)
            {
                Debug.Assert(path.StartsWith(baseDirectoryPath, StringComparison.OrdinalIgnoreCase));

                string relativePath = path.Substring(baseDirectoryPath.Length + 1);

                string destinationPath = Path.Combine(Target, relativePath);

                ExecuteOperationAndCatchIfThrows(context, path, destinationPath, indent);
            }
        }

        private void ExecuteOperationAndCatchIfThrows(SearchContext context, string sourcePath, string destinationPath, string indent)
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

        private void ExecuteOperation(SearchContext context, string sourcePath, string destinationPath, string indent)
        {
            bool overwrite = false;

            if (OverwriteOption == OverwriteOption.No
                && FileSystemHelpers.FileOrDirectoryExists(destinationPath))
            {
                return;
            }

            if (!Options.OmitPath)
            {
                LogHelpers.WritePath(destinationPath, indent: indent, verbosity: Verbosity.Minimal);
                Logger.WriteLine(Verbosity.Minimal);
            }

            if (OverwriteOption == OverwriteOption.Ask)
            {
                if (FileSystemHelpers.FileOrDirectoryExists(destinationPath))
                {
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
            }
            else if (OverwriteOption == OverwriteOption.Yes)
            {
                if (FileSystemHelpers.FileOrDirectoryExists(destinationPath))
                    overwrite = true;
            }
            else
            {
                throw new InvalidOperationException($"Unknown enum value '{OverwriteOption}'.");
            }

            if (overwrite)
            {
                File.Delete(destinationPath);
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
            }

            ExecuteOperation(sourcePath, destinationPath);
        }
    }
}
