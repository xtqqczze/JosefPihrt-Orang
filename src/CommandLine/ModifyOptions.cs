// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Orang
{
    internal class ModifyOptions
    {
        public static ModifyOptions Default { get; } = new ModifyOptions();

        public ModifyOptions(
            bool distinct = false,
            bool sort = false,
            bool sortDescending = false,
            bool removeEmpty = false,
            bool removeWhiteSpace = false,
            bool trimStart = false,
            bool trimEnd = false,
            bool toLower = false,
            bool toUpper = false,
            bool aggregate = false,
            bool ignoreCase = false,
            bool cultureInvariant = false,
            Func<IEnumerable<string>, IEnumerable<string>> modify = null,
            IEnumerable<string> outputPaths = null)
        {
            Distinct = distinct;
            Sort = sort;
            SortDescending = sortDescending;
            RemoveEmpty = removeEmpty;
            RemoveWhiteSpace = removeWhiteSpace;
            TrimStart = trimStart;
            TrimEnd = trimEnd;
            ToLower = toLower;
            ToUpper = toUpper;
            Aggregate = aggregate;
            IgnoreCase = ignoreCase;
            CultureInvariant = cultureInvariant;
            Modify = modify;
            OutputPaths = outputPaths?.ToImmutableArray() ?? ImmutableArray<string>.Empty;
        }

        public bool Distinct { get; }

        public bool Sort { get; }

        public bool SortDescending { get; }

        public bool RemoveEmpty { get; }

        public bool RemoveWhiteSpace { get; }

        public bool TrimStart { get; }

        public bool TrimEnd { get; }

        public bool Trim => TrimStart && TrimEnd;

        public bool ToLower { get; }

        public bool ToUpper { get; }

        public bool Aggregate { get; }

        public bool IgnoreCase { get; }

        public bool CultureInvariant { get; }

        public Func<IEnumerable<string>, IEnumerable<string>> Modify { get; }

        public ImmutableArray<string> OutputPaths { get; }

        public bool HasAnyOperation
        {
            get
            {
                return Distinct
                    || Sort
                    || SortDescending
                    || RemoveEmpty
                    || RemoveWhiteSpace
                    || TrimStart
                    || TrimEnd
                    || Modify != null;
            }
        }
    }
}
