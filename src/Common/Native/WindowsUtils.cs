// Copyright Bastian Eicher
// Licensed under the MIT License

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using System.Text;
using System.Threading;
using JetBrains.Annotations;
using NanoByte.Common.Properties;
using NanoByte.Common.Storage;

namespace NanoByte.Common.Native
{
    /// <summary>
    /// Provides helper methods and API calls specific to the Windows platform.
    /// </summary>
    public static partial class WindowsUtils
    {
        #region Win32 Error Codes
        /// <summary>
        /// The <see cref="Win32Exception.NativeErrorCode"/> value indicating that a file was not found.
        /// </summary>
        internal const int Win32ErrorFileNotFound = 2;

        /// <summary>
        /// The <see cref="Win32Exception.NativeErrorCode"/> value indicating that access to a resource was denied.
        /// </summary>
        internal const int Win32ErrorAccessDenied = 5;

        /// <summary>
        /// The <see cref="Win32Exception.NativeErrorCode"/> value indicating that write access to a resource failed.
        /// </summary>
        internal const int Win32ErrorWriteFault = 29;

        /// <summary>
        /// The <see cref="Win32Exception.NativeErrorCode"/> value indicating that an operation timed out.
        /// </summary>
        internal const int Win32ErrorSemTimeout = 121;

        /// <summary>
        /// The <see cref="Win32Exception.NativeErrorCode"/> value indicating that an element (e.g. a file) already exists.
        /// </summary>
        internal const int Win32ErrorAlreadyExists = 183;

        /// <summary>
        /// The <see cref="Win32Exception.NativeErrorCode"/> value indicating that more data is available and the query should be repeated with a larger output buffer/array.
        /// </summary>
        internal const int Win32ErrorMoreData = 234;

        /// <summary>
        /// The <see cref="Win32Exception.NativeErrorCode"/> value indicating that the requested application needs UAC elevation.
        /// </summary>
        internal const int Win32ErrorRequestedOperationRequiresElevation = 740;

        /// <summary>
        /// The <see cref="Win32Exception.NativeErrorCode"/> value indicating that an operation was cancelled by the user.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "Cancelled", Justification = "Naming matches the Win32 docs")]
        internal const int Win32ErrorCancelled = 1223;

        /// <summary>
        /// Builds a suitable <see cref="Exception"/> for a given <see cref="Win32Exception.NativeErrorCode"/>.
        /// </summary>
        internal static Exception BuildException(int error)
        {
            var ex = new Win32Exception(error);
            switch (error)
            {
                case Win32ErrorAlreadyExists:
                case Win32ErrorWriteFault:
                    return new IOException(ex.Message, ex);

                case Win32ErrorFileNotFound:
                    return new FileNotFoundException(ex.Message, ex);

                case Win32ErrorAccessDenied:
                    return new UnauthorizedAccessException(ex.Message, ex);

                case Win32ErrorRequestedOperationRequiresElevation:
                    return new NotAdminException(ex.Message, ex);

                case Win32ErrorSemTimeout:
                    return new TimeoutException();

                case Win32ErrorCancelled:
                    return new OperationCanceledException();

                default:
                    return ex;
            }
        }
        #endregion

        #region OS
        /// <summary>
        /// <c>true</c> if the current operating system is Windows (9x- or NT-based); <c>false</c> otherwise.
        /// </summary>
        public static bool IsWindows
#if NETSTANDARD2_0
            => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#else
            => Environment.OSVersion.Platform == PlatformID.Win32Windows || Environment.OSVersion.Platform == PlatformID.Win32NT;
#endif

        /// <summary>
        /// <c>true</c> if the current operating system is a modern Windows version (NT-based); <c>false</c> otherwise.
        /// </summary>
        public static bool IsWindowsNT
#if NETSTANDARD2_0
            => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#else
            => Environment.OSVersion.Platform == PlatformID.Win32Windows || Environment.OSVersion.Platform == PlatformID.Win32NT;
#endif

        /// <summary>
        /// <c>true</c> if the current operating system is Windows XP or newer; <c>false</c> otherwise.
        /// </summary>
        public static bool IsWindowsXP => IsWindowsNT && Environment.OSVersion.Version >= new Version(5, 1);

        /// <summary>
        /// <c>true</c> if the current operating system is Windows Vista or newer; <c>false</c> otherwise.
        /// </summary>
        public static bool IsWindowsVista => IsWindowsNT && Environment.OSVersion.Version >= new Version(6, 0);

        /// <summary>
        /// <c>true</c> if the current operating system is Windows 7 or newer; <c>false</c> otherwise.
        /// </summary>
        public static bool IsWindows7 => IsWindowsNT && Environment.OSVersion.Version >= new Version(6, 1);

        /// <summary>
        /// <c>true</c> if the current operating system is Windows 8 or newer; <c>false</c> otherwise.
        /// </summary>
        public static bool IsWindows8 => IsWindowsNT && Environment.OSVersion.Version >= new Version(6, 2);

        /// <summary>
        /// <c>true</c> if the current operating system is Windows 10 or newer; <c>false</c> otherwise.
        /// </summary>
        public static bool IsWindows10 => IsWindowsNT && Environment.OSVersion.Version >= new Version(10, 0);

        /// <summary>
        /// <c>true</c> if the current operating system is Windows 10 Anniversary Update (Redstone 1) or newer; <c>false</c> otherwise.
        /// </summary>
        public static bool IsWindows10Redstone => IsWindowsNT && Environment.OSVersion.Version >= new Version(10, 0, 14393);

        /// <summary>
        /// <c>true</c> if the current operating system supports UAC and it is enabled; <c>false</c> otherwise.
        /// </summary>
        public static bool HasUac => IsWindowsVista && RegistryUtils.GetDword(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "EnableLUA", defaultValue: 1) == 1;

        /// <summary>
        /// Indicates whether the current user is an administrator. Always returns <c>true</c> on non-Windows NT systems.
        /// </summary>
        public static bool IsAdministrator
        {
            get
            {
                if (!IsWindowsNT) return true;
                try
                {
                    // ReSharper disable once AssignNullToNotNullAttribute
                    return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Indicates whether the current process is running in an interactive session (rather than, e.g. as a service). Always returns <c>true</c> on non-Windows NT systems.
        /// </summary>
        public static bool IsInteractive
        {
            get
            {
                if (!IsWindowsNT) return true;
                try
                {
                    // ReSharper disable once AssignNullToNotNullAttribute
                    return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(new SecurityIdentifier(WellKnownSidType.InteractiveSid, null));
                }
                catch
                {
                    return true;
                }
            }
        }

        /// <summary>
        /// Determines the path of the executable the current process was launched from.
        /// </summary>
        public static string CurrentProcessPath
        {
            get
            {
                var fileName = new StringBuilder(255);
                SafeNativeMethods.GetModuleFileName(IntPtr.Zero, fileName, fileName.Capacity);
                return fileName.ToString();
            }
        }
        #endregion

        #region .NET Framework
        /// <summary>The directory version number for .NET Framework 2.0. This release includes the C# 2.0 compiler and the CLR 2.0 runtime.</summary>
        public const string NetFx20 = "v2.0.50727";

        /// <summary>The directory version number for .NET Framework 3.0.</summary>
        public const string NetFx30 = "v3.0";

        /// <summary>The directory version number for .NET Framework 3.5. This release includes the C# 3.0 compiler.</summary>
        public const string NetFx35 = "v3.5";

        /// <summary>The directory version number for .NET Framework 4.x.  This release includes a C# 4.0+ compiler and the CLR 4.0 runtime.</summary>
        public const string NetFx40 = "v4.0.30319";

        /// <summary>
        /// Returns the .NET Framework root directory for a specific version of the .NET Framework. Does not verify the directory actually exists!
        /// </summary>
        /// <param name="version">The full .NET version number including the leading "v". Use predefined constants when possible.</param>
        /// <returns>The path to the .NET Framework root directory.</returns>
        /// <remarks>Returns 64-bit directories if <see cref="OSUtils.Is64BitProcess"/> is <c>true</c>.</remarks>
        [NotNull]
        public static string GetNetFxDirectory([NotNull] string version)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(version)) throw new ArgumentNullException(nameof(version));
            #endregion

            return FileUtils.PathCombine(
                Environment.GetEnvironmentVariable("windir") ?? @"C:\Windows",
                "Microsoft.NET",
                OSUtils.Is64BitProcess ? "Framework64" : "Framework",
                version);
        }
        #endregion

        #region Command-line
        /// <summary>
        /// Tries to split a command-line into individual arguments.
        /// </summary>
        /// <param name="commandLine">The command-line to be split.</param>
        /// <returns>
        /// An array of individual arguments.
        /// Will return the entire command-line as one argument when not running on Windows or if splitting failed for some other reason.
        /// </returns>
        [NotNull, ItemNotNull]
        public static string[] SplitArgs([CanBeNull] string commandLine)
        {
            if (string.IsNullOrEmpty(commandLine)) return new string[0];
            if (!IsWindows) return new[] {commandLine};

            var ptrToSplitArgs = SafeNativeMethods.CommandLineToArgvW(commandLine, out int numberOfArgs);
            if (ptrToSplitArgs == IntPtr.Zero) return new[] {commandLine};

            try
            {
                // Copy result to managed array
                var splitArgs = new string[numberOfArgs];
                for (int i = 0; i < numberOfArgs; i++)
                    splitArgs[i] = Marshal.PtrToStringUni(Marshal.ReadIntPtr(ptrToSplitArgs, i * IntPtr.Size));
                return splitArgs;
            }
            finally
            {
                NativeMethods.LocalFree(ptrToSplitArgs);
            }
        }

        /// <summary>
        /// Tries to attach to a command-line console owned by the parent process.
        /// </summary>
        /// <returns><c>true</c> if the console was successfully attached; <c>false</c> if the parent process did not own a console.</returns>
        public static bool AttachConsole() => IsWindowsNT && NativeMethods.AttachConsole(uint.MaxValue);
        #endregion

        #region Performance counter
        private static long _performanceFrequency;

        /// <summary>
        /// A time index in seconds that continuously increases.
        /// </summary>
        /// <remarks>Depending on the operating system this may be the time of the system clock or the time since the system booted.</remarks>
        public static double AbsoluteTime
        {
            get
            {
                // Use high-accuracy kernel timing methods on NT
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    if (_performanceFrequency == 0)
                        SafeNativeMethods.QueryPerformanceFrequency(out _performanceFrequency);

                    SafeNativeMethods.QueryPerformanceCounter(out long time);
                    return time / (double)_performanceFrequency;
                }

                return Environment.TickCount / 1000f;
            }
        }
        #endregion

        #region File system
        /// <summary>
        /// Reads the entire contents of a file using the Win32 API.
        /// </summary>
        /// <param name="path">The path of the file to read.</param>
        /// <returns>The contents of the file as a byte array; <c>null</c> if there was a problem reading the file.</returns>
        /// <exception cref="PlatformNotSupportedException">This method is called on a platform other than Windows.</exception>
        /// <remarks>This method works like <see cref="File.ReadAllBytes"/>, but bypasses .NET's file path validation logic.</remarks>
        [CanBeNull]
        public static byte[] ReadAllBytes([NotNull, Localizable(false)] string path)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            #endregion

            if (!IsWindows) throw new PlatformNotSupportedException(Resources.OnlyAvailableOnWindows);

            var handle = NativeMethods.CreateFile(path, FileAccess.Read, FileShare.Read, IntPtr.Zero, FileMode.Open, FileAttributes.Normal, IntPtr.Zero);
            if (handle.ToInt32() == -1) return null;

            try
            {
                uint size = NativeMethods.GetFileSize(handle, IntPtr.Zero);
                var buffer = new byte[size];
                uint read = uint.MinValue;
                var lpOverlapped = new NativeOverlapped();
                if (NativeMethods.ReadFile(handle, buffer, size, ref read, ref lpOverlapped)) return buffer;
                else return null;
            }
            finally
            {
                NativeMethods.CloseHandle(handle);
            }
        }

        /// <summary>
        /// Writes the entire contents of a byte array to a file using the Win32 API. Existing files with the same name are overwritten.
        /// </summary>
        /// <param name="path">The path of the file to write to.</param>
        /// <param name="data">The data to write to the file.</param>
        /// <exception cref="IOException">There was an IO problem writing the file.</exception>
        /// <exception cref="UnauthorizedAccessException">Write access to the file was denied.</exception>
        /// <exception cref="Win32Exception">There was a problem writing the file.</exception>
        /// <exception cref="PlatformNotSupportedException">This method is called on a platform other than Windows.</exception>
        /// <remarks>This method works like <see cref="File.WriteAllBytes"/>, but bypasses .NET's file path validation logic.</remarks>
        public static void WriteAllBytes([NotNull, Localizable(false)] string path, [NotNull] byte[] data)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (data == null) throw new ArgumentNullException(nameof(data));
            #endregion

            if (!IsWindows) throw new PlatformNotSupportedException(Resources.OnlyAvailableOnWindows);

            var handle = NativeMethods.CreateFile(path, FileAccess.Write, FileShare.Write, IntPtr.Zero, FileMode.Create, 0, IntPtr.Zero);
            int error = Marshal.GetLastWin32Error();
            if (handle == new IntPtr(-1)) throw BuildException(error);

            try
            {
                uint bytesWritten = 0;
                var lpOverlapped = new NativeOverlapped();
                if (!NativeMethods.WriteFile(handle, data, (uint)data.Length, ref bytesWritten, ref lpOverlapped))
                    throw BuildException(Marshal.GetLastWin32Error());
            }
            finally
            {
                NativeMethods.CloseHandle(handle);
            }
        }

        /// <summary>
        /// Creates a symbolic link for a file or directory.
        /// </summary>
        /// <param name="sourcePath">The path of the link to create.</param>
        /// <param name="targetPath">The path of the existing file or directory to point to (relative to <paramref name="sourcePath"/>).</param>
        /// <exception cref="IOException">There was an IO problem creating the symlink.</exception>
        /// <exception cref="UnauthorizedAccessException">You have insufficient rights to create the symbolic link.</exception>
        /// <exception cref="Win32Exception">The symbolic link creation failed.</exception>
        /// <exception cref="PlatformNotSupportedException">This method is called on a platform other than Windows NT 6.0 (Vista) or newer.</exception>
        public static void CreateSymlink([NotNull, Localizable(false)] string sourcePath, [NotNull, Localizable(false)] string targetPath)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(sourcePath)) throw new ArgumentNullException(nameof(sourcePath));
            if (string.IsNullOrEmpty(targetPath)) throw new ArgumentNullException(nameof(targetPath));
            #endregion

            if (!IsWindowsVista) throw new PlatformNotSupportedException(Resources.OnlyAvailableOnWindows);

            string targetAbsolute = Path.Combine(Path.GetDirectoryName(sourcePath) ?? Directory.GetCurrentDirectory(), targetPath);
            if (!NativeMethods.CreateSymbolicLink(sourcePath, targetPath, Directory.Exists(targetAbsolute) ? 1 : 0))
                throw BuildException(Marshal.GetLastWin32Error());
        }

        /// <summary>
        /// Checks whether a file is an NTFS symbolic link.
        /// </summary>
        /// <param name="path">The path of the file to check.</param>
        /// <returns><c>true</c> if <paramref name="path"/> points to a symbolic link; <c>false</c> otherwise.</returns>
        /// <remarks>Will return <c>false</c> for non-existing files.</remarks>
        /// <exception cref="IOException">There was an IO problem getting link information.</exception>
        /// <exception cref="UnauthorizedAccessException">You have insufficient rights to get link information.</exception>
        /// <exception cref="Win32Exception">Getting link information failed.</exception>
        /// <exception cref="PlatformNotSupportedException">This method is called on a platform other than Windows NT 6.0 (Vista) or newer.</exception>
        public static bool IsSymlink([NotNull, Localizable(false)] string path)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            #endregion

            if (!IsWindowsVista) throw new PlatformNotSupportedException(Resources.OnlyAvailableOnWindows);

            // TODO: Implement
            return false;
        }

        /// <summary>
        /// Checks whether a file is an NTFS symbolic link.
        /// </summary>
        /// <param name="path">The path of the file to check.</param>
        /// <param name="target">Returns the target the symbolic link points to if it exists.</param>
        /// <returns><c>true</c> if <paramref name="path"/> points to a symbolic link; <c>false</c> otherwise.</returns>
        /// <exception cref="IOException">There was an IO problem getting link information.</exception>
        /// <exception cref="UnauthorizedAccessException">You have insufficient rights to get link information.</exception>
        /// <exception cref="Win32Exception">Getting link information failed.</exception>
        /// <exception cref="PlatformNotSupportedException">This method is called on a platform other than Windows NT 6.0 (Vista) or newer.</exception>
        public static bool IsSymlink([NotNull, Localizable(false)] string path, out string target)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            #endregion

            if (!IsWindowsVista) throw new PlatformNotSupportedException(Resources.OnlyAvailableOnWindows);

            // TODO: Implement
            target = null;
            return false;
        }

        /// <summary>
        /// Creates a hard link between two files.
        /// </summary>
        /// <param name="sourcePath">The path of the link to create.</param>
        /// <param name="targetPath">The absolute path of the existing file to point to.</param>
        /// <remarks>Only available on Windows 2000 or newer.</remarks>
        /// <exception cref="IOException">There was an IO problem creating the hard link.</exception>
        /// <exception cref="UnauthorizedAccessException">You have insufficient rights to create the hard link.</exception>
        /// <exception cref="Win32Exception">The hard link creation failed.</exception>
        /// <exception cref="PlatformNotSupportedException">This method is called on a platform other than Windows NT.</exception>
        public static void CreateHardlink([NotNull, Localizable(false)] string sourcePath, [NotNull, Localizable(false)] string targetPath)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(sourcePath)) throw new ArgumentNullException(nameof(sourcePath));
            if (string.IsNullOrEmpty(targetPath)) throw new ArgumentNullException(nameof(targetPath));
            #endregion

            if (!IsWindowsNT) throw new PlatformNotSupportedException(Resources.OnlyAvailableOnWindows);
            if (!NativeMethods.CreateHardLink(sourcePath, targetPath, IntPtr.Zero))
                throw BuildException(Marshal.GetLastWin32Error());
        }

        /// <summary>
        /// Determines whether to files are hardlinked.
        /// </summary>
        /// <param name="path1">The path of the first file.</param>
        /// <param name="path2">The path of the second file.</param>
        /// <exception cref="IOException">There was an IO problem checking the files.</exception>
        /// <exception cref="UnauthorizedAccessException">You have insufficient rights to check the files.</exception>
        /// <exception cref="Win32Exception">Checking the files failed.</exception>
        /// <exception cref="PlatformNotSupportedException">This method is called on a platform other than Windows NT.</exception>
        public static bool AreHardlinked([NotNull, Localizable(false)] string path1, [NotNull, Localizable(false)] string path2)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(path1)) throw new ArgumentNullException(nameof(path1));
            if (string.IsNullOrEmpty(path2)) throw new ArgumentNullException(nameof(path2));
            #endregion

            if (!IsWindowsNT) throw new PlatformNotSupportedException(Resources.OnlyAvailableOnWindows);
            return GetFileIndex(path1) == GetFileIndex(path2);
        }

        private static ulong GetFileIndex([NotNull, Localizable(false)] string path)
        {
            var handle = NativeMethods.CreateFile(path, FileAccess.Read, FileShare.Read, IntPtr.Zero, FileMode.Open, FileAttributes.Archive, IntPtr.Zero);
            if (handle == IntPtr.Zero) throw BuildException(Marshal.GetLastWin32Error());

            try
            {
                if (!NativeMethods.GetFileInformationByHandle(handle, out var fileInfo))
                    throw BuildException(Marshal.GetLastWin32Error());
                return fileInfo.FileIndexLow + (fileInfo.FileIndexHigh << 32);
            }
            finally
            {
                NativeMethods.CloseHandle(handle);
            }
        }

        /// <summary>
        /// Moves a file on the next reboot of the OS. Replaces existing files.
        /// </summary>
        /// <param name="sourcePath">The source path to move the file from.</param>
        /// <param name="destinationPath">The destination path to move the file to. <c>null</c> to delete the file instead of moving it.</param>
        /// <remarks>Useful for replacing in-use files.</remarks>
        public static void MoveFileOnReboot([NotNull] string sourcePath, [CanBeNull] string destinationPath)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(sourcePath)) throw new ArgumentNullException(nameof(sourcePath));
            #endregion

            if (!NativeMethods.MoveFileEx(sourcePath, destinationPath, NativeMethods.MoveFileFlags.MOVEFILE_REPLACE_EXISTING | NativeMethods.MoveFileFlags.MOVEFILE_DELAY_UNTIL_REBOOT))
                throw BuildException(Marshal.GetLastWin32Error());
        }
        #endregion

        #region Shell
        /// <summary>
        /// Sets the current process' explicit application user model ID.
        /// </summary>
        /// <param name="appID">The application ID to set.</param>
        /// <remarks>The application ID is used to group related windows in the taskbar.</remarks>
        public static void SetCurrentProcessAppID(string appID)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(appID)) throw new ArgumentNullException(nameof(appID));
            #endregion

            if (!IsWindows7) return;
            NativeMethods.SetCurrentProcessExplicitAppUserModelID(appID);
        }

        /// <summary>
        /// Informs the Windows shell that changes were made to the file association data in the registry.
        /// </summary>
        /// <remarks>This should be called immediately after the changes in order to trigger a refresh of the Explorer UI.</remarks>
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static void NotifyAssocChanged()
        {
            if (!IsWindows) return;

            const uint SHCNE_ASSOCCHANGED = 0x08000000;
            const uint SHCNF_IDLIST = 0;
            NativeMethods.SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static readonly IntPtr HWND_BROADCAST = new IntPtr(0xFFFF);

        /// <summary>
        /// Informs all GUI applications that changes where made to the environment variables (e.g. PATH) and that they should re-pull them.
        /// </summary>
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public static void NotifyEnvironmentChanged()
        {
            if (!IsWindows) return;

            const int WM_SETTINGCHANGE = 0x001A;
            const int SMTO_ABORTIFHUNG = 0x0002;
            NativeMethods.SendMessageTimeout(HWND_BROADCAST, WM_SETTINGCHANGE, IntPtr.Zero, "Environment", SMTO_ABORTIFHUNG, 5000, out var result);
        }
        #endregion

        #region Window messages
        /// <summary>
        /// Registers a new message type that can be sent to windows.
        /// </summary>
        /// <param name="message">A unique string used to identify the message type session-wide.</param>
        /// <returns>A unique ID number used to identify the message type session-wide.</returns>
        public static int RegisterWindowMessage([Localizable(false)] string message) => IsWindows ? NativeMethods.RegisterWindowMessage(message) : 0;

        /// <summary>
        /// Sends a message of a specific type to all windows in the current session.
        /// </summary>
        /// <param name="messageID">A unique ID number used to identify the message type session-wide.</param>
        public static void BroadcastMessage(int messageID)
        {
            if (IsWindows) NativeMethods.PostMessage(HWND_BROADCAST, messageID, IntPtr.Zero, IntPtr.Zero);
        }
        #endregion

        #region Restart Manager
        /// <summary>
        /// Registers the current application for automatic restart after updates or crashes.
        /// </summary>
        /// <param name="arguments">The command-line arguments to pass to the application on restart. Must not be empty!</param>
        public static void RegisterApplicationRestart([NotNull] string arguments)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(arguments)) throw new ArgumentNullException(nameof(arguments));
            #endregion

            if (!IsWindowsVista) return;

            int ret = NativeMethods.RegisterApplicationRestart(arguments, NativeMethods.RestartFlags.NONE);
            if (ret != 0) Log.Warn("Failed to register application for restart with arguments: " + arguments);
        }

        /// <summary>
        /// Unregisters the current application for automatic restart after updates or crashes.
        /// </summary>
        public static void UnregisterApplicationRestart()
        {
            if (!IsWindowsVista) return;

            NativeMethods.UnregisterApplicationRestart();
        }
        #endregion
    }
}
