// Copyright Bastian Eicher
// Licensed under the MIT License

using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using NanoByte.Common.Native;
using NanoByte.Common.Properties;
using NanoByte.Common.Storage;

namespace NanoByte.Common
{
    /// <summary>
    /// Provides methods for launching child processes.
    /// </summary>
    public static class ProcessUtils
    {
        /// <summary>
        /// Starts a new <see cref="Process"/> and runs it in parallel with this one. Handles and wraps <see cref="Win32Exception"/>s.
        /// </summary>
        /// <returns>The newly launched process.</returns>
        /// <exception cref="IOException">There was a problem launching the executable.</exception>
        /// <exception cref="FileNotFoundException">The executable file could not be found.</exception>
        /// <exception cref="NotAdminException">The target process requires elevation but the UAC prompt could not be displayed because <see cref="ProcessStartInfo.UseShellExecute"/> is <c>false</c>.</exception>
        /// <exception cref="OperationCanceledException">The user was asked for intervention by the OS (e.g. a UAC prompt) and the user cancelled.</exception>
        public static Process Start(this ProcessStartInfo startInfo)
        {
            #region Sanity checks
            if (startInfo == null) throw new ArgumentNullException(nameof(startInfo));
            #endregion

            Log.Debug("Launching process: " + startInfo.FileName.EscapeArgument() + " " + startInfo.Arguments);
            try
            {
                return Process.Start(startInfo);
            }
            catch (Win32Exception ex)
            {
                throw ex.NativeErrorCode switch
                {
                    WindowsUtils.Win32ErrorCancelled => (Exception)new OperationCanceledException(),
                    WindowsUtils.Win32ErrorRequestedOperationRequiresElevation => new NotAdminException($"Launching '{startInfo.FileName}' requires Administrator privileges."),
                    _ => new IOException(ex.Message, ex)
                };
            }
        }

        /// <summary>
        /// Starts a new <see cref="Process"/> and runs it in parallel with this one. Handles and wraps <see cref="Win32Exception"/>s.
        /// </summary>
        /// <param name="fileName">The path of the file to open or executable to launch.</param>
        /// <param name="arguments">The command-line arguments to pass to the executable.</param>
        /// <returns>The newly launched process; <c>null</c> if an existing process was reused.</returns>
        /// <exception cref="IOException">There was a problem launching the executable.</exception>
        /// <exception cref="FileNotFoundException">The executable file could not be found.</exception>
        /// <exception cref="NotAdminException">The target process requires elevation but the UAC prompt could not be displayed because <see cref="ProcessStartInfo.UseShellExecute"/> is <c>false</c>.</exception>
        /// <exception cref="OperationCanceledException">The user was asked for intervention by the OS (e.g. a UAC prompt) and the user cancelled.</exception>
        public static Process? Start(string fileName, params string[] arguments)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException(nameof(fileName));
            if (arguments == null) throw new ArgumentNullException(nameof(arguments));
            #endregion

            return new ProcessStartInfo(fileName, arguments.JoinEscapeArguments()).Start();
        }

        /// <summary>
        /// Starts a new <see cref="Process"/> and waits for it to complete. Handles and wraps <see cref="Win32Exception"/>s.
        /// </summary>
        /// <returns>The exit code of the child process.</returns>
        /// <exception cref="IOException">There was a problem launching the executable.</exception>
        /// <exception cref="FileNotFoundException">The executable file could not be found.</exception>
        /// <exception cref="NotAdminException">The target process requires elevation but the UAC prompt could not be displayed because <see cref="ProcessStartInfo.UseShellExecute"/> is <c>false</c>.</exception>
        /// <exception cref="OperationCanceledException">The user was asked for intervention by the OS (e.g. a UAC prompt) and the user cancelled.</exception>
        public static int Run(this ProcessStartInfo startInfo)
        {
            #region Sanity checks
            if (startInfo == null) throw new ArgumentNullException(nameof(startInfo));
            #endregion

            var process = Start(startInfo);
            if (process == null) return 0;
            process.WaitForExit();
            return process.ExitCode;
        }

        /// <summary>
        /// Creates a <see cref="ProcessStartInfo"/> for launching an assembly located in <see cref="Locations.InstallBase"/>.
        /// </summary>
        /// <param name="name">The name of the assembly to launch (without the file extension).</param>
        /// <param name="arguments">The command-line arguments to pass to the assembly.</param>
#if NETSTANDARD
        [System.Diagnostics.Contracts.Pure]
#endif
        public static ProcessStartInfo Assembly(string name, params string[] arguments)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if (arguments == null) throw new ArgumentNullException(nameof(arguments));
            #endregion

            string appPath = Path.Combine(Locations.InstallBase, name + ".exe");
            if (!File.Exists(appPath)) throw new FileNotFoundException(string.Format(Resources.UnableToLocateAssembly, name), appPath);

            // Only Windows can directly launch .NET executables, other platforms must run through Mono
            var startInfo = WindowsUtils.IsWindows
                ? new ProcessStartInfo(appPath, arguments.JoinEscapeArguments()) {UseShellExecute = false}
                : new ProcessStartInfo("mono", appPath.EscapeArgument() + " " + arguments.JoinEscapeArguments()) {UseShellExecute = false};

            return startInfo;
        }

        /// <summary>
        /// Modifies a <see cref="ProcessStartInfo"/> to request elevation to Administrator on Windows using UAC.
        /// </summary>
        /// <exception cref="PlatformNotSupportedException">The current operating system does not support UAC or it is disabled..</exception>
        public static ProcessStartInfo AsAdmin(this ProcessStartInfo startInfo)
        {
            #region Sanity checks
            if (startInfo == null) throw new ArgumentNullException(nameof(startInfo));
            #endregion

            if (!WindowsUtils.HasUac) throw new PlatformNotSupportedException("The current operating system does not support UAC or it is disabled.");

            startInfo.Verb = "runas";
            startInfo.UseShellExecute = true;
            return startInfo;
        }

        /// <summary>
        /// Workaround for environment variable problems, such variable names that differ only in case when running on Windows.
        /// </summary>
        /// <remarks>Call this before any access to <see cref="ProcessStartInfo.EnvironmentVariables"/> to avoid <see cref="ArgumentException"/>s.</remarks>
        public static void SanitizeEnvironmentVariables()
        {
            if (!WindowsUtils.IsWindows) return;

            var dict = new StringDictionary();
            foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
            {
                try
                {
                    dict.Add((string)entry.Key, (string)entry.Value);
                }
                catch (ArgumentException ex)
                {
                    Log.Warn("Ignoring environment variable '" + entry.Key + "' in this process due to problem: " + ex.Message);
                    Environment.SetEnvironmentVariable((string)entry.Key, null);
                }
            }
        }
    }
}
