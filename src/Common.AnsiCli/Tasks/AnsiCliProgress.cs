﻿// Copyright Bastian Eicher
// Licensed under the MIT License

using System;
using Spectre.Console;

namespace NanoByte.Common.Tasks
{
    /// <summary>
    /// Reports task progress updates using ANSI console output.
    /// </summary>
    /// <seealso cref="AnsiCliProgressContext"/>
    public class AnsiCliProgress : MarshalByRefObject, IProgress<TaskSnapshot>
    {
        private readonly ProgressTask _progressTask;

        internal AnsiCliProgress(ProgressTask progressTask)
        {
            _progressTask = progressTask;
        }

        /// <inheritdoc/>
        public void Report(TaskSnapshot value)
        {
            switch (value.State)
            {
                case TaskState.Started:
                    _progressTask.StartTask();
                    break;

                case TaskState.Data when value.UnitsTotal > 0:
                    _progressTask.MaxValue = value.UnitsTotal;
                    _progressTask.Increment(value.UnitsProcessed - _progressTask.Value);
                    break;

                case TaskState.Complete:
                    _progressTask.Increment(_progressTask.MaxValue - _progressTask.Value);
                    _progressTask.StopTask();
                    break;

                case TaskState.WebError or TaskState.IOError or TaskState.Canceled:
                    _progressTask.Description += $" ({value.State})";
                    _progressTask.StopTask();
                    break;
            }
        }
    }
}
