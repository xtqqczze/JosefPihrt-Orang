﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Orang.FileSystem;
using static Orang.CommandLine.LogHelpers;
using static Orang.Logger;

namespace Orang.CommandLine
{
    internal class FindCommand : CommonFindCommand<FindCommandOptions>
    {
        private bool _ask;

        public FindCommand(FindCommandOptions options, CommonCopyOperation operation = null) : base(options)
        {
            Operation = operation;
        }

        public CommonCopyOperation Operation { get; }

        protected override void ExecuteCore(SearchContext context)
        {
            if (Options.AskMode == AskMode.File
                && ConsoleOut.Verbosity >= Verbosity.Minimal)
            {
                _ask = true;
            }

            base.ExecuteCore(context);
        }

        protected override void ExecuteFile(string filePath, SearchContext context)
        {
            context.Telemetry.FileCount++;

            FileSystemFinderResult? maybeResult = MatchFile(filePath);

            if (maybeResult != null)
                ExecuteOrAddResult(maybeResult.Value, context, null);
        }

        protected override void ExecuteDirectory(string directoryPath, SearchContext context)
        {
            if (Operation?.CanExecute(directoryPath) == false)
                return;

            foreach (FileSystemFinderResult result in Find(directoryPath, context))
            {
                ExecuteOrAddResult(result, context, directoryPath);

                if (context.State == SearchState.Canceled)
                    break;

                if (context.State == SearchState.MaxReached)
                    break;
            }
        }

        protected override void ExecuteResult(SearchResult result, SearchContext context, ColumnWidths columnWidths)
        {
            ExecuteResult(result.Result, context, result.BaseDirectoryPath, columnWidths);
        }

        protected override void ExecuteResult(FileSystemFinderResult result, SearchContext context, string baseDirectoryPath = null, ColumnWidths columnWidths = null)
        {
            string indent = GetPathIndent(baseDirectoryPath);

            if (!Options.OmitPath)
                WritePath(context, result, baseDirectoryPath, indent, columnWidths);

            if (_ask)
            {
                try
                {
                    if (ConsoleHelpers.Question("Continue without asking?", indent))
                        _ask = false;
                }
                catch (OperationCanceledException)
                {
                    context.State = SearchState.Canceled;
                }
            }

            Operation?.Execute(result, context, baseDirectoryPath, indent);
        }

        protected override void WriteSummary(SearchTelemetry telemetry, Verbosity verbosity)
        {
            WriteSearchedFilesAndDirectories(telemetry, Options.SearchTarget, verbosity);

            if (!ShouldLog(verbosity))
                return;

            WriteLine(verbosity);

            if (Options.SearchTarget != SearchTarget.Directories)
            {
                WriteCount("Matching files", telemetry.MatchingFileCount, Colors.Message_OK, verbosity);
                WriteLine(verbosity);
            }

            if (Options.SearchTarget != SearchTarget.Files)
            {
                WriteCount("Matching directories", telemetry.MatchingDirectoryCount, Colors.Message_OK, verbosity);
                WriteLine(verbosity);
            }
        }
    }
}
