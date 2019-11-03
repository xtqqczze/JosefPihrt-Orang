// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Orang.CommandLine
{
    internal interface IValueStorage
    {
        void Add(string value);

        void AddRange(IEnumerable<string> value);
    }
}
