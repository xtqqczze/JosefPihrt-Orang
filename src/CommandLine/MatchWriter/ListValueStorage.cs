// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Orang.CommandLine
{
    internal class ListValueStorage : IValueStorage
    {
        private readonly List<string> _list;

        public ListValueStorage(List<string> list)
        {
            _list = list;
        }

        public void Add(string value)
        {
            _list.Add(value);
        }
    }
}
