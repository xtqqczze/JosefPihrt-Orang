﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Orang.CommandLine
{
    internal class OutputDisplayFormat
    {
        internal static readonly string DefaultIndent = "  ";

        public OutputDisplayFormat(
            ContentDisplayStyle contentDisplayStyle,
            PathDisplayStyle pathDisplayStyle = PathDisplayStyle.Full,
            LineDisplayOptions lineOptions = LineDisplayOptions.None,
            bool? includeSummary = null,
            bool? includeCount = null,
            string indent = null,
            string separator = null)
        {
            ContentDisplayStyle = contentDisplayStyle;
            PathDisplayStyle = pathDisplayStyle;
            LineOptions = lineOptions;
            IncludeSummary = includeSummary;
            IncludeCount = includeCount;
            Indent = indent ?? DefaultIndent;
            Separator = separator ?? Environment.NewLine;
        }

        public ContentDisplayStyle ContentDisplayStyle { get; }

        public PathDisplayStyle PathDisplayStyle { get; }

        public LineDisplayOptions LineOptions { get; }

        public bool? IncludeSummary { get; }

        public bool? IncludeCount { get; }

        public string Indent { get; }

        public string Separator { get; }

        public bool Includes(LineDisplayOptions options) => (LineOptions & options) == options;
    }
}
