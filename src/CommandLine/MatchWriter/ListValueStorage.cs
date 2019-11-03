// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Orang.CommandLine
{
    internal class ListValueStorage : IValueStorage
    {
        public ListValueStorage()
        {
            Values = new List<string>();
        }

        public ListValueStorage(List<string> list)
        {
            Values = list;
        }

        public List<string> Values { get; }

        public void Add(string value)
        {
            Values.Add(value);
        }
    }
}
