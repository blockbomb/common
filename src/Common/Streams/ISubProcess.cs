﻿// Copyright Bastian Eicher
// Licensed under the MIT License

using System.Diagnostics;

namespace NanoByte.Common.Streams;

/// <summary>
/// Runs a sub/child process.
/// </summary>
public interface ISubProcess
{
    /// <summary>
    /// Starts the sub process and runs it in parallel with this one.
    /// </summary>
    /// <param name="arguments">Command-line arguments to launch the process with.</param>
    /// <returns>The newly launched process.</returns>
    /// <exception cref="IOException">There was a problem launching the executable.</exception>
    /// <exception cref="FileNotFoundException">The executable file could not be found.</exception>
    /// <exception cref="NotAdminException">The target process requires elevation.</exception>
    Process Start(params string[] arguments);

    /// <summary>
    /// Runs the sub process and waits for it to exit.
    /// </summary>
    /// <param name="arguments">Command-line arguments to launch the process with.</param>
    /// <exception cref="IOException">There was a problem launching the executable.</exception>
    /// <exception cref="FileNotFoundException">The executable file could not be found.</exception>
    /// <exception cref="NotAdminException">The target process requires elevation.</exception>
    /// <exception cref="ExitCodeException">The process exited with a non-zero <see cref="Process.ExitCode"/>.</exception>
    void Run(params string[] arguments);

    /// <summary>
    /// Runs the sub process, captures its stdout and stderr output and waits for it to exit.
    /// </summary>
    /// <param name="onStartup">A callback for writing to the process' stdin right after startup.</param>
    /// <param name="arguments">Command-line arguments to launch the process with.</param>
    /// <returns>The process' complete stdout output.</returns>
    /// <exception cref="IOException">There was a problem launching the executable.</exception>
    /// <exception cref="FileNotFoundException">The executable file could not be found.</exception>
    /// <exception cref="NotAdminException">The target process requires elevation.</exception>
    /// <exception cref="ExitCodeException">The process exited with a non-zero <see cref="Process.ExitCode"/>.</exception>
    string RunAndCapture(Action<StreamWriter>? onStartup, params string[] arguments);
}
