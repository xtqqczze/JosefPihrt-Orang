// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Orang.CommandLine
{
    internal class MoveCommand : CommonCopyCommand<MoveCommandOptions>
    {
        public MoveCommand(MoveCommandOptions options) : base(options)
        {
        }

        protected override void ExecuteFileOperation(string sourcePath, string destinationPath)
        {
            File.Move(sourcePath, destinationPath);
        }

        protected override void WriteSummary(SearchTelemetry telemetry, Verbosity verbosity)
        {
            base.WriteSummary(telemetry, verbosity);

            LogHelpers.WriteCount("Moved files", telemetry.ProcessedFileCount, Colors.Message_Change, verbosity);
            Logger.WriteLine(verbosity);
        }
    }
}
