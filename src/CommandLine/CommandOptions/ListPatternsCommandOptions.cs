﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.RegularExpressions;

namespace Orang.CommandLine
{
    internal class ListPatternsCommandOptions
    {
        internal ListPatternsCommandOptions()
        {
        }

        public char Value { get; internal set; }

        public RegexOptions RegexOptions { get; internal set; }

        public bool InCharGroup { get; internal set; }
    }
}
