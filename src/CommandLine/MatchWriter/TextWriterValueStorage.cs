// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;

namespace Orang.CommandLine
{
    internal class TextWriterValueStorage : IValueStorage
    {
        private readonly TextWriter _writer;

        public TextWriterValueStorage(TextWriter writer)
        {
            _writer = writer;
        }

        public void Add(string value)
        {
            _writer.WriteLine(value);
        }

        public void AddRange(IEnumerable<string> values)
        {
            foreach (string value in values)
                _writer.WriteLine(value);
        }
    }
}
