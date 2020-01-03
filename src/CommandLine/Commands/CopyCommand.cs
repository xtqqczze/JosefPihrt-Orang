// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Orang.CommandLine
{
    internal class CopyCommand : CommonCopyCommand<CopyCommandOptions>
    {
        public CopyCommand(CopyCommandOptions options) : base(options)
        {
        }

        protected override void ExecuteOperation(string sourcePath, string destinationPath)
        {
            File.Copy(sourcePath, destinationPath);
        }

        protected override void WriteSummary(SearchTelemetry telemetry, Verbosity verbosity)
        {
            base.WriteSummary(telemetry, verbosity);

            LogHelpers.WriteCount("Copied files", telemetry.ProcessedFileCount, Colors.Message_Change, verbosity);
            Logger.WriteLine(verbosity);
        }
    }
}
