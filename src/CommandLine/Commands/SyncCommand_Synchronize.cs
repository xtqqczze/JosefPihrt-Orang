// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using Orang.FileSystem;

namespace Orang.CommandLine
{
    internal class SyncCommand_Synchronize : CommonSyncCommand
    {
        private bool _isSourceToTarget;

        public SyncCommand_Synchronize(SyncCommandOptions options) : base(options)
        {
        }

        public SyncAction SyncAction
        {
            get { return Options.SyncAction; }
            private set { Options.SyncAction = value; }
        }

        protected override void ExecuteOperation(SearchContext context, string sourcePath, string destinationPath, bool isDirectory, string indent)
        {
            bool fileExists = File.Exists(destinationPath);
            bool directoryExists = !fileExists && Directory.Exists(destinationPath);

            bool? preferTarget = null;

            if (isDirectory)
            {
                if (directoryExists)
                {
                    if (!_isSourceToTarget)
                        return;

                    if (File.GetAttributes(sourcePath) == File.GetAttributes(destinationPath))
                        return;
                }
            }
            else if (fileExists)
            {
                if (!_isSourceToTarget)
                    return;

                int diff = File.GetLastWriteTimeUtc(sourcePath).CompareTo(File.GetLastWriteTimeUtc(destinationPath));

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
                if (!isDirectory
                    && fileExists
                    && Options.CompareOptions != FileCompareOptions.None
                    && FileSystemHelpers.FileEquals(sourcePath, destinationPath, Options.CompareOptions))
                {
                    return;
                }

                if (SyncAction == SyncAction.Ask)
                {
                    WritePathPrefix(sourcePath, "SRC", default, indent);
                    WritePathPrefix(destinationPath, "TRG", default, indent);

                    DialogResult dialogResult = ConsoleHelpers.QuestionWithResult("Prefer target directory?", indent);

                    switch (dialogResult)
                    {
                        case DialogResult.Yes:
                            {
                                preferTarget = true;
                                break;
                            }
                        case DialogResult.YesToAll:
                            {
                                preferTarget = true;
                                SyncAction = SyncAction.PreferTarget;
                                break;
                            }
                        case DialogResult.No:
                        case DialogResult.None:
                            {
                                preferTarget = false;
                                break;
                            }
                        case DialogResult.NoToAll:
                            {
                                preferTarget = false;
                                SyncAction = SyncAction.PreferSource;
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
                else if (SyncAction == SyncAction.PreferSource)
                {
                    preferTarget = false;
                }
                else if (SyncAction == SyncAction.PreferTarget)
                {
                    preferTarget = true;
                }
                else
                {
                    throw new InvalidOperationException($"Unknown enum value '{SyncAction}'.");
                }
            }

            ExecuteOperations(context, sourcePath, destinationPath, isDirectory, fileExists, directoryExists, preferTarget ?? false, indent);
        }

        protected override void ExecuteDirectory(string directoryPath, SearchContext context)
        {
            _destinationPaths = new HashSet<string>(FileSystemHelpers.Comparer);

            _isSourceToTarget = true;
            base.ExecuteDirectory(directoryPath, context);
            _isSourceToTarget = false;

            IgnoredPaths = _destinationPaths;
            _destinationPaths = null;

            string target = directoryPath;
            directoryPath = Target;

            Options.Paths = ImmutableArray.Create(new PathInfo(directoryPath, PathOrigin.None));
            Options.Target = target;

            if (SyncAction == SyncAction.PreferSource)
            {
                SyncAction = SyncAction.PreferTarget;
            }
            else if (SyncAction == SyncAction.PreferTarget)
            {
                SyncAction = SyncAction.PreferSource;
            }

            base.ExecuteDirectory(directoryPath, context);

            IgnoredPaths = null;
        }
    }
}
