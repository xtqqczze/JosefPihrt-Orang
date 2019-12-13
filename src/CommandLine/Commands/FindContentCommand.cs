// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
        private List<int> _storageIndexes;
        private IResultStorage _fileStorage;
        private List<string> _fileValues;
        private OutputSymbols _symbols;

        public FindContentCommand(FindCommandOptions options) : base(options)
        {
        }

        private OutputSymbols Symbols => _symbols ?? (_symbols = OutputSymbols.Create(Options.HighlightOptions));

        protected override void ExecuteCore(SearchContext context)
        {
            context.Telemetry.MatchingLineCount = -1;

            if (ConsoleOut.Verbosity >= Verbosity.Minimal)
                _askMode = Options.AskMode;

            bool aggregate = (Options.ModifyOptions.Functions & ModifyFunctions.Intersect) != 0
                || (Options.ModifyOptions.Aggregate
                    && (Options.ModifyOptions.Modify != null
                        || (Options.ModifyOptions.Functions & ModifyFunctions.Enumerable) != 0));

            if (aggregate)
            {
                _storage = new ListResultStorage();

                if ((Options.ModifyOptions.Functions & ModifyFunctions.Intersect) != 0)
                    _storageIndexes = new List<int>();
            }

            base.ExecuteCore(context);

            if (aggregate)
                WriteAggregatedValues();
        }

        protected override void ExecuteFile(string filePath, SearchContext context)
        {
            context.Telemetry.FileCount++;

            FileSystemFinderResult? maybeResult = MatchFile(filePath);

            if (maybeResult != null)
                ProcessResult(maybeResult.Value, context, FileWriterOptions);
        }

        protected override void ExecuteDirectory(string directoryPath, SearchContext context)
        {
            foreach (FileSystemFinderResult result in Find(directoryPath, context))
            {
                ProcessResult(result, context, DirectoryWriterOptions, directoryPath);

                if (context.State == SearchState.Canceled)
                    break;

                if (context.State == SearchState.MaxReached)
                    break;
            }
        }

        private void ProcessResult(
            FileSystemFinderResult result,
            SearchContext context,
            ContentWriterOptions writerOptions,
            string baseDirectoryPath = null)
        {
            string input = ReadFile(result.Path, baseDirectoryPath, Options.DefaultEncoding, context);

            if (input == null)
                return;

            Match match = Options.ContentFilter.Match(input, context.CancellationToken);

            if (match == null)
                return;

            ExecuteOrAddResult(result, context, writerOptions, match, input, default(Encoding), baseDirectoryPath);
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
            string indent = (baseDirectoryPath != null && Options.DisplayRelativePath)
                ? Options.Indent
                : "";

            if (!Options.OmitPath)
                WritePath(context, result, baseDirectoryPath, indent, columnWidths);

            if (Options.ContentFilter.IsNegative)
            {
                WriteLineIf(!Options.OmitPath, Verbosity.Minimal);
            }
            else
            {
                WriteMatches(input, match, writerOptions, context);
            }

            if (_askMode == AskMode.File)
            {
                try
                {
                    if (ConsoleHelpers.Question("Continue without asking?", indent))
                        _askMode = AskMode.None;
                }
                catch (OperationCanceledException)
                {
                    context.State = SearchState.Canceled;
                }
            }
        }

        private void WriteMatches(
            string input,
            Match match,
            ContentWriterOptions writerOptions,
            SearchContext context)
        {
            SearchTelemetry telemetry = context.Telemetry;

            ContentWriter contentWriter = null;
            List<Group> groups = null;

            try
            {
                groups = ListCache<Group>.GetInstance();

                GetGroups(match, writerOptions.GroupNumber, context, isPathWritten: !Options.OmitPath, predicate: Options.ContentFilter.Predicate, groups: groups);

                if (_storage != null
                    || Options.AskMode == AskMode.Value
                    || ShouldLog(Verbosity.Normal))
                {
                    if (Options.ModifyOptions.HasAnyFunction)
                    {
                        Debug.Assert(Options.ContentDisplayStyle == ContentDisplayStyle.Value, Options.ContentDisplayStyle.ToString());

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

                        using (var contentWriter2 = new EmptyContentWriter(null, writerOptions, (Options.ModifyOptions.HasAnyFunction) ? _fileStorage : _storage))
                        {
                            WriteMatches(contentWriter2, groups, context);
                        }

                        ConsoleColors colors = (Options.HighlightMatch) ? Colors.Match : default;
                        ConsoleColors boundaryColors = (Options.HighlightBoundary) ? Colors.MatchBoundary : default;

                        var valueWriter = new ValueWriter(writerOptions.Indent, includeEndingIndent: false);

                        foreach (string value in _fileValues.Modify(Options.ModifyOptions))
                        {
                            Write(writerOptions.Indent, Verbosity.Normal);
                            valueWriter.Write(value, Symbols, colors, boundaryColors);
                            WriteLine(Verbosity.Normal);

                            _storage?.Add(value);
                            telemetry.MatchCount++;
                        }

                        _storageIndexes?.Add(_storage!.Count);
                    }
                    else
                    {
                        MatchOutputInfo outputInfo = Options.CreateOutputInfo(input, match);

                        contentWriter = ContentWriter.CreateFind(Options.ContentDisplayStyle, input, writerOptions, _storage, outputInfo, ask: _askMode == AskMode.Value);
                    }
                }
                else if (Options.ModifyOptions.HasAnyFunction)
                {
                    foreach (string value in _fileValues.Modify(Options.ModifyOptions))
                    {
                        telemetry.MatchCount++;
                        _storage?.Add(value);
                    }

                    _storageIndexes?.Add(_storage!.Count);
                }
                else
                {
                    contentWriter = new EmptyContentWriter(null, writerOptions);
                }

                if (contentWriter != null)
                {
                    WriteMatches(contentWriter, groups, context);

                    telemetry.MatchCount += contentWriter.MatchCount;

                    if (contentWriter.MatchingLineCount >= 0)
                    {
                        if (telemetry.MatchingLineCount == -1)
                            telemetry.MatchingLineCount = 0;

                        telemetry.MatchingLineCount += contentWriter.MatchingLineCount;
                    }
                }

                if (_askMode == AskMode.Value)
                {
                    if (contentWriter is AskValueContentWriter askValueContentWriter)
                    {
                        if (!askValueContentWriter.Ask)
                            _askMode = AskMode.None;
                    }
                    else if (contentWriter is AskLineContentWriter askLineContentWriter)
                    {
                        if (!askLineContentWriter.Ask)
                            _askMode = AskMode.None;
                    }
                }
            }
            finally
            {
                contentWriter?.Dispose();

                if (groups != null)
                    ListCache<Group>.Free(groups);
            }
        }

        private void WriteAggregatedValues()
        {
            int count = 0;

            List<string> allValues = ((ListResultStorage)_storage).Values;

            if (_storageIndexes?.Count > 1)
                allValues = Intersect();

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

                    ConsoleOut.WriteLineIf(ConsoleOut.Verbosity > Verbosity.Minimal || (ConsoleOut.Verbosity == Verbosity.Minimal && Options.PathDisplayStyle != PathDisplayStyle.Omit));
                    Out?.WriteLineIf(Out.Verbosity > Verbosity.Minimal || (Out.Verbosity == Verbosity.Minimal && Options.PathDisplayStyle != PathDisplayStyle.Omit));

                    do
                    {
                        valueWriter.Write(en.Current, symbols, colors, boundaryColors);
                        WriteLine(Verbosity.Minimal);
                        count++;

                    } while (en.MoveNext());

                    if (ShouldLog(Verbosity.Detailed)
                        || Options.IncludeSummary)
                    {
                        WriteLine(Verbosity.Minimal);
                        WriteCount("Values", count, verbosity: Verbosity.Minimal);
                        WriteLine(Verbosity.Minimal);
                    }
                }
            }

            List<string> Intersect()
            {
                var list = new List<string>(GetRange(0, _storageIndexes[0]));

                for (int i = 1; i < _storageIndexes.Count; i++)
                {
                    IEnumerable<string> second = GetRange(_storageIndexes[i - 1], _storageIndexes[i]);

                    list = list.Intersect(second).ToList();
                }

                return list;

                IEnumerable<string> GetRange(int start, int end)
                {
                    for (int i = start; i < end; i++)
                        yield return allValues[i];
                }
            }
        }

        protected override void WriteSummary(SearchTelemetry telemetry, Verbosity verbosity)
        {
            WriteSearchedFilesAndDirectories(telemetry, Options.SearchTarget, verbosity);

            if (!ShouldLog(verbosity))
                return;

            WriteLine(verbosity);
            WriteCount("Matches", telemetry.MatchCount, Colors.Message_OK, verbosity);
            Write("  ", Colors.Message_OK, verbosity);

            if (telemetry.MatchingLineCount > 0)
            {
                WriteCount("Matching lines", telemetry.MatchingLineCount, Colors.Message_OK, verbosity);
                Write("  ", Colors.Message_OK, verbosity);
            }

            if (telemetry.MatchingFileCount > 0)
                WriteCount("Matching files", telemetry.MatchingFileCount, Colors.Message_OK, verbosity);

            WriteLine(verbosity);
        }

        protected override ContentWriterOptions CreateContentWriterOptions(string indent)
        {
            int groupNumber = Options.ContentFilter.GroupNumber;

            GroupDefinition? groupDefinition = (groupNumber >= 0)
                ? new GroupDefinition(groupNumber, Options.ContentFilter.GroupName)
                : default(GroupDefinition?);

            return new ContentWriterOptions(
                format: Options.Format,
                groupDefinition,
                symbols: Symbols,
                highlightOptions: Options.HighlightOptions,
                indent: indent);
        }
    }
}
