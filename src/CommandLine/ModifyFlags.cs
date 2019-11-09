// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Orang
{
    [Flags]
    internal enum ModifyFlags
    {
        None = 0,
        Distinct = 1,
        Sort = 1 << 1,
        SortDescending = 1 << 2,
        Intersect = 1 << 3,
        RemoveEmpty = 1 << 4,
        RemoveWhiteSpace = 1 << 5,
        TrimStart = 1 << 6,
        TrimEnd = 1 << 7,
        Trim = TrimStart | TrimEnd,
        ToUpper = 1 << 8,
        ToLower = 1 << 9,
        Aggregate = 1 << 10,
        IgnoreCase = 1 << 11,
        CultureInvariant = 1 << 12
    }
}
