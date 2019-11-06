// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Orang.FileSystem;
using static Orang.CommandLine.LogHelpers;
using static Orang.Logger;

namespace Orang.CommandLine
{
    internal class FindContentCommand : CommonFindContentCommand<FindCommandOptions>
    {
        private AskMode _askMode;
        private IResultStorage _storage;
        private IResultStorage _fileStorage;
        private List<string> _fileValues;
        private OutputSymbols _symbols;

        public FindContentCommand(FindCommandOptions options) : base(options)
        {
        }

        protected override bool CanExecuteFile => true;

        private OutputSymbols Symbols => _symbols ?? (_symbols = OutputSymbols.Create(Options.HighlightOptions));

        protected override void ExecuteCore(SearchContext context)
        {
            context.Telemetry.MatchingLineCount = -1;

            if (ConsoleOut.Verbosity >= Verbosity.Minimal)
                _askMode = Options.AskMode;

            bool aggregate = Options.ModifyOptions.Aggregate
                && (Options.ModifyOptions.Modify != null
                    || (Options.ModifyOptions.Functions & ModifyFunctions.Enumerable) != 0);

            if (aggregate)
            {
                _storage = new ListResultStorage();
            }
            else if (Options.OutputPath != null)
            {
                _storage = new TextWriterResultStorage(context.Output);
            }

            base.ExecuteCore(context);

            if (aggregate)
                WriteAggregatedValues(context);
        }

        protected override void ExecuteFile(string filePath, SearchContext context)
        {
            context.Telemetry.FileCount++;

            string input = ReadFile(filePath, null, Options.DefaultEncoding);

            if (input != null)
            {
                Match match = Options.ContentFilter.Regex.Match(input);

                if (Options.ContentFilter.IsMatch(match))
                {
                    if (!Options.OmitPath)
                        WritePath(filePath, colors: Colors.Matched_Path, verbosity: Verbosity.Minimal);

                    WriteMatches(input, match, FileWriterOptions, context);

                    if (Options.MaxMatchingFiles == context.Telemetry.MatchingFileCount)
                        context.State = SearchState.MaxReached;
                }
            }
        }

        protected override void ExecuteDirectory(string directoryPath, SearchContext context, FileSystemFinderProgressReporter progress)
        {
            Regex regex = Options.ContentFilter.Regex;
            string basePath = (Options.PathDisplayStyle == PathDisplayStyle.Full) ? null : directoryPath;
            string indent = (Options.PathDisplayStyle == PathDisplayStyle.Relative) ? Options.Indent : "";

            foreach (FileSystemFinderResult result in FileSystemHelpers.Find(directoryPath, Options, progress, context.CancellationToken))
            {
                string input = ReadFile(result.Path, directoryPath, Options.DefaultEncoding, progress, indent);

                if (input == null)
                    continue;

                Match match = regex.Match(input);

                if (Options.ContentFilter.IsMatch(match))
                {
                    EndProgress(progress);

                    if (!Options.OmitPath)
                        WritePath(result, basePath, colors: Colors.Matched_Path, matchColors: (Options.HighlightMatch) ? Colors.Match_Path : default, indent: indent, verbosity: Verbosity.Minimal);

                    if (Options.ContentFilter.IsNegative)
                    {
                        WriteLineIf(!Options.OmitPath, Verbosity.Minimal);
                    }
                    else
                    {
                        WriteMatches(input, match, DirectoryWriterOptions, context);
                    }

                    if (Options.MaxMatchingFiles == context.Telemetry.MatchingFileCount)
                        context.State = SearchState.MaxReached;

                    if (context.State == SearchState.MaxReached)
                        break;

                    if (_askMode == AskMode.File
                        && ConsoleHelpers.Question("Continue without asking?", indent))
                    {
                        _askMode = AskMode.None;
                    }
                }
            }

            context.Telemetry.SearchedDirectoryCount = progress.SearchedDirectoryCount;
            context.Telemetry.DirectoryCount = progress.DirectoryCount;
            context.Telemetry.FileCount = progress.FileCount;
        }

        private void WriteMatches(
            string input,
            Match match,
            MatchWriterOptions writerOptions,
            SearchContext context)
        {
            SearchTelemetry telemetry = context.Telemetry;

            telemetry.MatchingFileCount++;

            var maxReason = MaxReason.None;

            if (ShouldLog(Verbosity.Normal)
                || _storage != null)
            {
                WriteLineIf(!Options.OmitPath, Verbosity.Minimal);

                if (Options.ModifyOptions.HasAnyFunction)
                {
                    Debug.Assert(Options.ContentDisplayStyle == ContentDisplayStyle.Value, Options.ContentDisplayStyle.ToString());

                    EnumerateValues();

                    ConsoleColors colors = (Options.HighlightMatch) ? Colors.Match : default;
                    ConsoleColors boundaryColors = (Options.HighlightBoundary) ? Colors.MatchBoundary : default;

                    var valueWriter = new ValueWriter(writerOptions.Indent, includeEndingIndent: false);

                    foreach (string value in _fileValues.Modify(Options.ModifyOptions))
                    {
                        Write(writerOptions.Indent, Verbosity.Normal);
                        valueWriter.Write(value, Symbols, colors, boundaryColors);
                        WriteLine(Verbosity.Normal);
                        telemetry.MatchCount++;
                        _storage?.Add(value);
                    }
                }
                else
                {
                    MatchOutputInfo outputInfo = Options.CreateOutputInfo(input, match);

                    using (MatchWriter matchWriter = MatchWriter.CreateFind(Options.ContentDisplayStyle, input, writerOptions, _storage, outputInfo, ask: _askMode == AskMode.Value))
                    {
                        maxReason = WriteMatches(matchWriter, match, context);
                        telemetry.MatchCount += matchWriter.MatchCount;

                        if (matchWriter.MatchingLineCount >= 0)
                        {
                            if (telemetry.MatchingLineCount == -1)
                                telemetry.MatchingLineCount = 0;

                            telemetry.MatchingLineCount += matchWriter.MatchingLineCount;
                        }

                        if (_askMode == AskMode.Value)
                        {
                            if (matchWriter is AskValueMatchWriter askValueMatchWriter)
                            {
                                if (!askValueMatchWriter.Ask)
                                    _askMode = AskMode.None;
                            }
                            else if (matchWriter is AskLineMatchWriter askLineMatchWriter)
                            {
                                if (!askLineMatchWriter.Ask)
                                    _askMode = AskMode.None;
                            }
                        }
                    }
                }
            }
            else
            {
                int fileMatchCount = EnumerateValues();

                if (Options.ModifyOptions.HasAnyFunction)
                {
                    fileMatchCount = 0;

                    foreach (string value in _fileValues.Modify(Options.ModifyOptions))
                    {
                        fileMatchCount++;
                        _storage?.Add(value);
                    }
                }

                if (!Options.OmitPath)
                {
                    Write(" ", Colors.Message_OK, Verbosity.Minimal);
                    WriteCount("", fileMatchCount, Colors.Message_OK, Verbosity.Minimal);
                    WriteIf(maxReason == MaxReason.CountExceedsMax, "+", Colors.Message_OK, Verbosity.Minimal);
                    WriteLine(Verbosity.Minimal);
                }

                telemetry.MatchCount += fileMatchCount;
            }

            int EnumerateValues()
            {
                if (Options.ModifyOptions.HasAnyFunction)
                {
                    if (_fileValues == null)
                    {
                        _fileValues = new List<string>();
                    }
                    else
                    {
                        _fileValues.Clear();
                    }

                    if (_fileStorage == null)
                        _fileStorage = new ListResultStorage(_fileValues);
                }

                using (var matchWriter = new EmptyMatchWriter(null, writerOptions, (Options.ModifyOptions.HasAnyFunction) ? _fileStorage : _storage))
                {
                    maxReason = WriteMatches(matchWriter, match, context);

                    if (matchWriter.MatchingLineCount >= 0
                        && !Options.ModifyOptions.HasAnyFunction)
                    {
                        if (telemetry.MatchingLineCount == -1)
                            telemetry.MatchingLineCount = 0;

                        telemetry.MatchingLineCount += matchWriter.MatchingLineCount;
                    }

                    return matchWriter.MatchCount;
                }
            }
        }

        private void WriteAggregatedValues(SearchContext context)
        {
            int count = 0;

            List<string> allValues = ((ListResultStorage)_storage).Values;

            using (IEnumerator<string> en = allValues
                .Modify(Options.ModifyOptions, filter: ModifyFunctions.Enumerable)
                .GetEnumerator())
            {
                if (en.MoveNext())
                {
                    OutputSymbols symbols = OutputSymbols.Create(Options.HighlightOptions);
                    ConsoleColors colors = (Options.HighlightMatch) ? Colors.Match : default;
                    ConsoleColors boundaryColors = (Options.HighlightBoundary) ? Colors.MatchBoundary : default;
                    var valueWriter = new ValueWriter(includeEndingIndent: false, verbosity: Verbosity.Minimal);

                    WriteLine(Verbosity.Minimal);

                    do
                    {
                        valueWriter.Write(en.Current, symbols, colors, boundaryColors);
                        WriteLine(Verbosity.Minimal);
                        context.Output?.WriteLine(en.Current);
                        count++;

                    } while (en.MoveNext());

                    WriteLine(Verbosity.Minimal);
                    WriteCount("Values", count, verbosity: Verbosity.Minimal);
                    WriteLine(Verbosity.Minimal);
                }
            }
        }

        protected override void WriteSummary(SearchTelemetry telemetry)
        {
            WriteSearchedFilesAndDirectories(telemetry, Options.SearchTarget);

            if (!ShouldLog(Verbosity.Minimal))
                return;

            WriteLine(Verbosity.Minimal);
            WriteCount("Matches", telemetry.MatchCount, Colors.Message_OK, Verbosity.Minimal);

            if (telemetry.MatchCount > 0)
            {
                Write("  ", Colors.Message_OK, Verbosity.Minimal);

                if (telemetry.MatchingLineCount > 0)
                {
                    WriteCount("Matching lines", telemetry.MatchingLineCount, Colors.Message_OK, Verbosity.Minimal);
                    Write("  ", Colors.Message_OK, Verbosity.Minimal);
                }

                WriteCount("Matching files", telemetry.MatchingFileCount, Colors.Message_OK, Verbosity.Minimal);
            }

            WriteLine(Verbosity.Minimal);
        }

        protected override MatchWriterOptions CreateMatchWriteOptions(string indent)
        {
            int groupNumber = Options.ContentFilter.GroupNumber;

            GroupDefinition? groupDefinition = (groupNumber >= 0)
                ? new GroupDefinition(groupNumber, Options.ContentFilter.GroupName)
                : default(GroupDefinition?);

            return new MatchWriterOptions(
                format: Options.Format,
                groupDefinition,
                symbols: Symbols,
                highlightOptions: Options.HighlightOptions,
                indent: indent);
        }
    }
}
