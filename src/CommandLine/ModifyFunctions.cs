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
        RemoveEmpty = 1 << 3,
        RemoveWhiteSpace = 1 << 4,
        TrimStart = 1 << 5,
        TrimEnd = 1 << 6,
        ToLower = 1 << 7,
        ToUpper = 1 << 8,
    }
}
