// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using Orang.FileSystem;

namespace Orang.CommandLine
{
    internal abstract class CommonCopyCommand<TOptions> : FindCommand<TOptions> where TOptions : CommonCopyCommandOptions
    {
        protected CommonCopyCommand(TOptions options) : base(options)
        {
        }

        protected override void ExecuteResult(
            FileSystemFinderResult result,
            SearchContext context,
            string baseDirectoryPath,
            string indent)
        {
            string sourcePath = result.Path;

            if (result.IsDirectory)
            {
                foreach (string filePath in FileSystemHelpers.EnumerateAllFiles(sourcePath))
                    Execute(filePath);
            }
            else if (baseDirectoryPath != null)
            {
                Execute(sourcePath);
            }
            else
            {
                string fileName = Path.GetFileName(sourcePath);

                string destinationPath = Path.Combine(Options.Target, fileName);

                ExecuteOperationAndCatchIfThrows(context, sourcePath, destinationPath, indent);
            }

            void Execute(string path)
            {
                Debug.Assert(path.StartsWith(baseDirectoryPath, StringComparison.OrdinalIgnoreCase));

                string relativePath = path.Substring(baseDirectoryPath.Length + 1);

                string destinationPath = Path.Combine(Options.Target, relativePath);

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

            switch (Options.ConflictOption)
            {
                case ConflictOption.Ask:
                    {
                        if (FileSystemHelpers.FileOrDirectoryExists(destinationPath))
                        {
                            DialogResult dialogResult = ConsoleHelpers.QuestionWithResult("Copy file", indent);
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
                                        Options.ConflictOption = ConflictOption.Overwrite;
                                        break;
                                    }
                                case DialogResult.No:
                                case DialogResult.None:
                                    {
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
                case ConflictOption.Overwrite:
                    {
                        if (FileSystemHelpers.FileOrDirectoryExists(destinationPath))
                            overwrite = true;

                        break;
                    }
                case ConflictOption.Skip:
                    {
                        if (FileSystemHelpers.FileOrDirectoryExists(destinationPath))
                            return;

                        break;
                    }
                default:
                    {
                        throw new InvalidOperationException($"Unknown enum value '{Options.ConflictOption}'.");
                    }
            }

            if (overwrite)
            {
                File.Delete(destinationPath);
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
            }

            if (!Options.OmitPath)
            {
                LogHelpers.WritePath(destinationPath, indent: indent, verbosity: Verbosity.Minimal);
                Logger.WriteLine(Verbosity.Minimal);
            }

            ExecuteOperation(sourcePath, destinationPath, indent);
        }

        protected abstract void ExecuteOperation(string sourcePath, string destinationPath, string indent);
    }
}
