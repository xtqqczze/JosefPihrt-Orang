// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Orang
{
    [Flags]
    internal enum ModifyFunctions
    {
        None = 0,
        Distinct = 1,
        Sort = 1 << 1,
        SortDescending = 1 << 2,
        Intersect = 1 << 3,
        Enumerable = Distinct | Sort | SortDescending,
        RemoveEmpty = 1 << 4,
        RemoveWhiteSpace = 1 << 5,
        TrimStart = 1 << 6,
        TrimEnd = 1 << 7,
        ToLower = 1 << 8,
        ToUpper = 1 << 9
    }
}
