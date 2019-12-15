// Copyright Bastian Eicher
// Licensed under the MIT License

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using NanoByte.Common.Info;
using NanoByte.Common.Storage;

namespace NanoByte.Common
{
    /// <summary>
    /// Describes an event relating to an entry in the <see cref="Log"/>.
    /// </summary>
    /// <param name="severity">The type/severity of the entry.</param>
    /// <param name="message">The message text of the entry.</param>
    /// <seealso cref="Log.Handler"/>
    public delegate void LogEntryEventHandler(LogSeverity severity, string message);

    /// <summary>
    /// Sends log messages to custom handlers or the <see cref="Console"/>.
    /// Additionally writes to <see cref="System.Diagnostics.Debug"/>, an in-memory buffer and a plain text file.
    /// </summary>
    public static class Log
    {
        #region File Writer
        private static StreamWriter? _fileWriter;

        [SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline", Justification = "The static constructor is used to add an identification header to the log file")]
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Any kind of problems writing the log file should be ignored")]
        static Log()
        {
            string filePath = Path.Combine(Path.GetTempPath(), $"{AppInfo.Current.Name} {Environment.UserName} Log.txt");

            // Try to open the file for writing but give up right away if there are any problems
            FileStream file;
            try
            {
                file = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
            }
            catch
            {
                return;
            }

            // Clear cache file once it reaches 1MB
            if (file.Length > 1024 * 1024)
            {
                file.Dispose();
                file = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            }

            // When writing to a new file use UTF-8 with BOM, otherwise keep existing encoding
            _fileWriter = (file.Length == 0 ? new StreamWriter(file, Encoding.UTF8) : new StreamWriter(file));
            _fileWriter.AutoFlush = true;

            // Go to end of file
            _fileWriter.BaseStream.Seek(0, SeekOrigin.End);

            // Add session identification block to the file
            _fileWriter.WriteLine("");
            _fileWriter.WriteLine("/// " + AppInfo.Current.NameVersion);
            _fileWriter.WriteLine("/// Install base: " + Locations.InstallBase);
            _fileWriter.WriteLine("/// Command-line args: " + Environment.GetCommandLineArgs().JoinEscapeArguments());
            _fileWriter.WriteLine("/// Log session started at: " + DateTime.Now.ToString(CultureInfo.InvariantCulture));
            _fileWriter.WriteLine("");
        }
        #endregion

        private static readonly List<LogEntryEventHandler> _handlers = new List<LogEntryEventHandler>
        {
            // Default handler
            (severity, message) =>
            {
                if (severity >= LogSeverity.Warn) PrintToConsole(severity, message);
            }
        };

        /// <summary>
        /// Invoked when a new entry is added to the log.
        /// Only the newest (last) registered handler is invoked.
        /// <see cref="Console"/> output is used as a fallback if no handlers are registered.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1009:DeclareEventHandlersCorrectly")]
        public static event LogEntryEventHandler? Handler
        {
            add
            {
                lock (_lock)
                {
                    if (value == null) return;
                    _sessionContent = new StringBuilder(); // Reset per session (indicated by new handler)
                    _handlers.Add(value);
                }
            }
            remove
            {
                if (value == null) return;
                lock (_lock) _handlers.Remove(value);
            }
        }

        private static StringBuilder _sessionContent = new StringBuilder();

        /// <summary>
        /// Collects all log entries from this application session.
        /// </summary>
        public static string Content
        {
            get
            {
                lock (_lock)
                    return _sessionContent.ToString();
            }
        }

        /// <summary>
        /// Writes information to help developers diagnose problems to the log.
        /// </summary>
        public static void Debug(string message) => AddEntry(LogSeverity.Debug, message);

        /// <summary>
        /// Writes an exception as an <see cref="Debug(string)"/>.
        /// </summary>
        public static void Debug(Exception ex)
        {
            if (ex == null) throw new ArgumentNullException(nameof(ex));

            Debug(ex.ToString());
        }

        /// <summary>
        /// Writes nice-to-know information to the log.
        /// </summary>
        public static void Info(string message) => AddEntry(LogSeverity.Info, message);

        /// <summary>
        /// Writes an exception's message as a <see cref="Info(string)"/>.
        /// </summary>
        /// <remarks>Also sends the entire exception to <see cref="Debug(Exception)"/>.</remarks>
        public static void Info(Exception ex)
        {
            if (ex == null) throw new ArgumentNullException(nameof(ex));

            Info(ex.GetMessageWithInner());
            Debug(ex);
        }

        /// <summary>
        /// Writes a warning that doesn't have to be acted upon immediately to the log.
        /// </summary>
        public static void Warn(string message) => AddEntry(LogSeverity.Warn, message);

        /// <summary>
        /// Writes an exception's message as a <see cref="Warn(string)"/>.
        /// </summary>
        /// <remarks>Also sends the entire exception to <see cref="Debug(Exception)"/>.</remarks>
        public static void Warn(Exception ex)
        {
            if (ex == null) throw new ArgumentNullException(nameof(ex));

            Warn(ex.GetMessageWithInner());
            Debug(ex);
        }

        /// <summary>
        /// Writes a critical error that should be attended to to the log.
        /// </summary>
        public static void Error(string message) => AddEntry(LogSeverity.Error, message);

        /// <summary>
        /// Writes an exception's message as an <see cref="Error(string)"/>.
        /// </summary>
        /// <remarks>Also sends the entire exception to <see cref="Debug(Exception)"/>.</remarks>
        public static void Error(Exception ex)
        {
            if (ex == null) throw new ArgumentNullException(nameof(ex));

            Error(ex.GetMessageWithInner());
            Debug(ex);
        }

        /// <summary>
        /// Prints a log entry to the <see cref="Console"/>.
        /// </summary>
        /// <param name="severity">The type/severity of the entry.</param>
        /// <param name="message">The message text of the entry.</param>
        public static void PrintToConsole(LogSeverity severity, string message)
        {
            try
            {
                switch (severity)
                {
                    case LogSeverity.Debug:
                        Console.ForegroundColor = ConsoleColor.Blue;
                        break;
                    case LogSeverity.Info:
                        Console.ForegroundColor = ConsoleColor.Green;
                        break;
                    case LogSeverity.Warn:
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        break;
                    case LogSeverity.Error:
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                }
            }
            #region Error handling
            catch (InvalidOperationException)
            {}
            catch (IOException)
            {}
            #endregion

            Console.Error.WriteLine(message ?? throw new ArgumentNullException(nameof(message)));
            try
            {
                Console.ResetColor();
            }
            #region Error handling
            catch (InvalidOperationException)
            {}
            catch (IOException)
            {}
            #endregion
        }

        #region Helpers
        private static readonly object _lock = new object();

        private static void AddEntry(LogSeverity severity, string message)
        {
            message = UnifyWhitespace(message ?? throw new ArgumentNullException(nameof(message)));
            string formattedMessage = FormatMessage(severity, message);

            lock (_lock)
            {
                System.Diagnostics.Debug.Write(formattedMessage);

                _sessionContent.AppendLine(formattedMessage);

                try
                {
                    _fileWriter?.WriteLine(formattedMessage);
                }
                #region Error handling
                catch (Exception ex)
                {
                    _fileWriter = null;
                    Console.Error.WriteLine("Error writing to log file:");
                    Console.Error.WriteLine(ex);
                }
                #endregion

                _handlers.Last()(severity, message);
            }
        }

        private static string UnifyWhitespace(string message)
        {
            var lines = message.Trim().SplitMultilineText();
            message = string.Join(Environment.NewLine + "\t", lines);
            return message;
        }

        private static string FormatMessage(LogSeverity severity, string message)
        {
            switch (severity)
            {
                case LogSeverity.Debug:
                    return string.Format(CultureInfo.InvariantCulture, "[{0:T}] DEBUG: {1}", DateTime.Now, message);
                case LogSeverity.Info:
                    return string.Format(CultureInfo.InvariantCulture, "[{0:T}] INFO: {1}", DateTime.Now, message);
                case LogSeverity.Warn:
                    return string.Format(CultureInfo.InvariantCulture, "[{0:T}] WARN: {1}", DateTime.Now, message);
                case LogSeverity.Error:
                    return string.Format(CultureInfo.InvariantCulture, "[{0:T}] ERROR: {1}", DateTime.Now, message);
                default:
                    return string.Format(CultureInfo.InvariantCulture, "[{0:T}] UNKNOWN: {1}", DateTime.Now, message);
            }
        }
        #endregion
    }
}
