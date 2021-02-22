// Copyright Bastian Eicher
// Licensed under the MIT License

using System;
using System.IO;
using NanoByte.Common.Native;
using NanoByte.Common.Net;

namespace NanoByte.Common.Tasks
{
    /// <summary>
    /// Informs the user about the progress of tasks and ask questions using console output.
    /// </summary>
    public class CliTaskHandler : TaskHandlerBase
    {
        /// <summary>
        /// Creates a new CLI task handler.
        /// Registers a <see cref="Log.Handler"/>.
        /// </summary>
        public CliTaskHandler()
        {
            if (WindowsUtils.IsWindowsNT)
                CredentialProvider = new CachedCredentialProvider(new WindowsCliCredentialProvider(this));

            try
            {
                Console.CancelKeyPress += CancelKeyPressHandler;
            }
            #region Error handling
            catch (IOException)
            {
                // Ignore problems caused by unusual terminal emulators
            }
            #endregion

            Log.Handler += LogHandler;
        }

        /// <summary>
        /// Unregisters the <see cref="Log.Handler"/>.
        /// </summary>
        public override void Dispose()
        {
            Log.Handler -= LogHandler;

            try
            {
                Console.CancelKeyPress -= CancelKeyPressHandler;
            }
            #region Error handling
            catch (IOException)
            {
                // Ignore problems caused by unusual terminal emulators
            }
            #endregion

            base.Dispose();
        }

        /// <summary>
        /// Handles Ctrl+C key presses.
        /// </summary>
        private void CancelKeyPressHandler(object? sender, ConsoleCancelEventArgs e)
        {
            CancellationTokenSource.Cancel();

            // Allow the application to finish cleanup rather than terminating immediately
            e.Cancel = true;
        }

        /// <summary>
        /// Prints <see cref="Log"/> messages to the <see cref="Console"/> based on their <see cref="LogSeverity"/> and the current <see cref="Verbosity"/> level.
        /// </summary>
        /// <param name="severity">The type/severity of the entry.</param>
        /// <param name="message">The message text of the entry.</param>
        private void LogHandler(LogSeverity severity, string message)
        {
            void WriteLine(ConsoleColor color)
            {
                try
                {
                    Console.ForegroundColor = color;
                }
                catch (InvalidOperationException)
                {}
                catch (IOException)
                {}

                Console.Error.WriteLine(message);
                try
                {
                    Console.ResetColor();
                }
                catch (InvalidOperationException)
                {}
                catch (IOException)
                {}
            }

            switch (severity)
            {
                case LogSeverity.Debug when Verbosity >= Verbosity.Debug:
                    WriteLine(ConsoleColor.Blue);
                    break;
                case LogSeverity.Info when Verbosity >= Verbosity.Verbose:
                    WriteLine(ConsoleColor.Green);
                    break;
                case LogSeverity.Warn:
                    WriteLine(ConsoleColor.DarkYellow);
                    break;
                case LogSeverity.Error:
                    WriteLine(ConsoleColor.Red);
                    break;
            }
        }

        /// <inheritdoc/>
        public override void RunTask(ITask task)
        {
            #region Sanity checks
            if (task == null) throw new ArgumentNullException(nameof(task));
            #endregion

            if (Verbosity <= Verbosity.Batch)
                task.Run(CancellationToken, CredentialProvider);
            else
            {
                Log.Debug("Task: " + task.Name);
                Console.Error.WriteLine(task.Name + @"...");
                task.Run(CancellationToken, CredentialProvider, new CliProgress());
            }
        }

        /// <inheritdoc />
        public override bool Ask(string question, bool? defaultAnswer = null, string? alternateMessage = null)
        {
            #region Sanity checks
            if (question == null) throw new ArgumentNullException(nameof(question));
            #endregion

            if (Verbosity <= Verbosity.Batch && defaultAnswer.HasValue)
            {
                if (!string.IsNullOrEmpty(alternateMessage)) Log.Warn(alternateMessage);
                return defaultAnswer.Value;
            }

            Log.Debug($"Question: {question}");
            Console.Error.WriteLine(question);

            // Loop until the user has made a valid choice
            while (true)
            {
                Console.Error.Write(@"[Y/N]" + " ");
                switch ((Console.ReadLine() ?? throw new IOException("input stream closed, unable to get user input")).ToLower())
                {
                    case "y" or "yes":
                        Log.Debug("Answer: Yes");
                        return true;
                    case "n" or "no":
                        Log.Debug("Answer: No");
                        return false;
                }
            }
        }

        /// <inheritdoc/>
        public override void Output(string title, string message)
        {
            #region Sanity checks
            if (title == null) throw new ArgumentNullException(nameof(title));
            if (message == null) throw new ArgumentNullException(nameof(message));
            #endregion

            if (message.EndsWith("\n")) Console.Write(message);
            else Console.WriteLine(message);
        }

        /// <inheritdoc/>
        public override void Error(Exception exception)
            => Log.Error(exception);
    }
}
