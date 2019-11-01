// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
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
        private List<string> _values;
        private List<string> _fileValues;
        private OutputSymbols _symbols;
        private MatchWriterOptions _fileWriterOptions;
        private MatchWriterOptions _directoryWriterOptions;

        public FindContentCommand(FindCommandOptions options) : base(options)
        {
        }

        protected override bool CanExecuteFile => true;

        private List<string> FileValues => _fileValues ?? (_fileValues = new List<string>());

        private OutputSymbols Symbols => _symbols ?? (_symbols = OutputSymbols.Create(Options.HighlightOptions));

        private MatchWriterOptions FileWriterOptions => _fileWriterOptions ?? (_fileWriterOptions = CreateMatchWriteOptions(Options.Indent));

        private MatchWriterOptions DirectoryWriterOptions => _directoryWriterOptions ?? (_directoryWriterOptions = CreateMatchWriteOptions(Options.DoubleIndent));

        protected override void ExecuteCore(SearchContext context)
        {
            context.Telemetry.MatchingLineCount = -1;

            if (ConsoleOut.Verbosity >= Verbosity.Minimal)
                _askMode = Options.AskMode;

            if (Options.ModifyOptions.Aggregate)
                _values = new List<string>();

            base.ExecuteCore(context);
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

            string basePath = (Options.Format.Includes(MiscellaneousDisplayOptions.IncludeFullPath)) ? null : directoryPath;

            foreach (FileSystemFinderResult result in FileSystemHelpers.Find(directoryPath, Options, progress, context.CancellationToken))
            {
                string input = ReadFile(result.Path, directoryPath, Options.DefaultEncoding, progress, Options.Indent);

                if (input == null)
                    continue;

                Match match = regex.Match(input);

                if (Options.ContentFilter.IsMatch(match))
                {
                    EndProgress(progress);
                    WritePath(result, basePath, colors: Colors.Matched_Path, matchColors: (Options.HighlightMatch) ? Colors.Match_Path : default, indent: Options.Indent, verbosity: Verbosity.Minimal);

                    if (Options.ContentFilter.IsNegative)
                    {
                        WriteLine(Verbosity.Minimal);
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
                        && ConsoleHelpers.Question("Continue without asking?", Options.Indent))
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

            if (ShouldLog(Verbosity.Normal))
            {
                WriteLine(Verbosity.Minimal);

                if (Options.ContentDisplayStyle == ContentDisplayStyle.Value
                    && (Options.ModifyOptions.HasAnyOperation))
                {
                    EnumerateValues();

                    ConsoleColors colors = (Options.HighlightMatch) ? Colors.Match : default;
                    ConsoleColors boundaryColors = (Options.HighlightBoundary) ? Colors.MatchBoundary : default;

                    var valueWriter = new ValueWriter(writerOptions.Indent, includeEndingIndent: false);

                    foreach (string value in FileValues.Modify(Options.ModifyOptions))
                    {
                        valueWriter.Write(value, Symbols, colors, boundaryColors);
                        WriteLine(Verbosity.Normal);
                        telemetry.MatchCount++;
                    }
                }
                else
                {
                    MatchOutputInfo outputInfo = Options.CreateOutputInfo(input, match);

                    using (MatchWriter matchWriter = MatchWriter.CreateFind(Options.ContentDisplayStyle, input, writerOptions, _values, outputInfo, ask: _askMode == AskMode.Value))
                    {
                        WriteMatches(matchWriter, match, context);
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

                if (Options.ModifyOptions.HasAnyOperation)
                    fileMatchCount = FileValues.Modify(Options.ModifyOptions).Count();

                Write(" ", Colors.Message_OK, Verbosity.Minimal);
                WriteCount("", fileMatchCount, Colors.Message_OK, Verbosity.Minimal);
                WriteLine(Verbosity.Minimal);

                telemetry.MatchCount += fileMatchCount;
            }

            int EnumerateValues()
            {
                if (Options.ModifyOptions.Aggregate
                    || Options.ModifyOptions.HasAnyOperation)
                {
                    FileValues.Clear();
                }

                using (var matchWriter = new EmptyMatchWriter(null, writerOptions, FileValues))
                {
                    WriteMatches(matchWriter, match, context);

                    _values?.AddRange(FileValues);

                    if (matchWriter.MatchingLineCount >= 0
                        && !Options.ModifyOptions.HasAnyOperation)
                    {
                        if (telemetry.MatchingLineCount == -1)
                            telemetry.MatchingLineCount = 0;

                        telemetry.MatchingLineCount += matchWriter.MatchingLineCount;
                    }

                    return matchWriter.MatchCount;
                }
            }
        }

        protected override void WriteSummary(SearchTelemetry telemetry)
        {
            if (_values != null)
            {
                int count = 0;

                IEnumerable<string> values = _values.Modify(Options.ModifyOptions);

                ImmutableArray<string> paths = Options.ModifyOptions.OutputPaths;

                if (paths.Any())
                    values = values.ToList();

                using (IEnumerator<string> en = values.GetEnumerator())
                {
                    if (en.MoveNext())
                    {
                        OutputSymbols symbols = OutputSymbols.Create(Options.HighlightOptions);
                        ConsoleColors colors = (Options.HighlightMatch) ? Colors.Match : default;
                        ConsoleColors boundaryColors = (Options.HighlightBoundary) ? Colors.MatchBoundary : default;
                        var valueWriter = new ValueWriter(includeEndingIndent: false, verbosity: Verbosity.Quiet);

                        WriteLine();

                        do
                        {
                            valueWriter.Write(en.Current, symbols, colors, boundaryColors);
                            WriteLine();
                            count++;

                        } while (en.MoveNext());

                        WriteLine();
                        WriteCount("Values", count);
                        WriteLine();
                    }
                }

                foreach (string path in paths)
                {
                    StreamWriter writer = null;

                    try
                    {
                        WriteLine($"Saving '{path}'", Verbosity.Diagnostic);

                        writer = new StreamWriter(path, false, Encoding.UTF8);

                        foreach (string value in values)
                            writer.WriteLine(value);
                    }
                    catch (Exception ex) when (ex is IOException
                        || ex is UnauthorizedAccessException)
                    {
                        WriteWarning(ex);
                    }
                    finally
                    {
                        writer?.Dispose();
                    }
                }
            }

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

        private MatchWriterOptions CreateMatchWriteOptions(string indent)
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
