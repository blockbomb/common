﻿/*
 * Copyright 2006-2015 Bastian Eicher
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using NanoByte.Common.Properties;

namespace NanoByte.Common.Storage
{
    partial class Locations
    {
        /// <summary>
        /// Returns a path for storing a configuration resource (can roam across different machines).
        /// </summary>
        /// <param name="appName">The name of application. Used as part of the path, unless <see cref="IsPortable"/> is <c>true</c>.</param>
        /// <param name="isFile"><c>true</c> if the last part of <paramref name="resource"/> refers to a file instead of a directory.</param>
        /// <param name="resource">The path elements (directory and/or file names) of the resource to be stored.</param>
        /// <returns>A fully qualified path to use to store the resource. Directories are guaranteed to already exist; files are not.</returns>
        /// <exception cref="IOException">A problem occurred while creating a directory.</exception>
        /// <exception cref="UnauthorizedAccessException">Creating a directory is not permitted.</exception>
        [PublicAPI, NotNull]
        public static string GetSaveConfigPath([NotNull, Localizable(false)] string appName, bool isFile, [NotNull, ItemNotNull] params string[] resource)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(appName)) throw new ArgumentNullException("appName");
            if (resource == null) throw new ArgumentNullException("resource");
            #endregion

            string resourceCombined = (resource.Length == 0) ? "" : resource.Aggregate(Path.Combine);
            string path;
            try
            {
                path = (_isPortable
                    ? new[] {_portableBase, "config", resourceCombined}
                    : new[] {UserConfigDir, appName, resourceCombined}).Aggregate(Path.Combine);
            }
                #region Error handling
            catch (ArgumentException ex)
            {
                // Wrap exception to add context information
                throw new IOException(string.Format(Resources.InvalidConfigDir, UserConfigDir) + "\n" + ex.Message, ex);
            }
            #endregion

            // Ensure the directory part of the path exists
            string dirPath = isFile ? (Path.GetDirectoryName(path) ?? path) : path;
            if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);

            return Path.GetFullPath(path);
        }

        /// <summary>
        /// Returns a path for storing a system-wide configuration resource.
        /// </summary>
        /// <param name="appName">The name of application. Used as part of the path, unless <see cref="IsPortable"/> is <c>true</c>.</param>
        /// <param name="isFile"><c>true</c> if the last part of <paramref name="resource"/> refers to a file instead of a directory.</param>
        /// <param name="resource">The path elements (directory and/or file names) of the resource to be stored.</param>
        /// <returns>A fully qualified path to use to store the resource. Directories are guaranteed to already exist; files are not.</returns>
        /// <exception cref="IOException">A problem occurred while creating a directory.</exception>
        /// <exception cref="UnauthorizedAccessException">Creating a directory is not permitted.</exception>
        [PublicAPI, NotNull]
        public static string GetSaveSystemConfigPath([NotNull, Localizable(false)] string appName, bool isFile, [NotNull, ItemNotNull] params string[] resource)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(appName)) throw new ArgumentNullException("appName");
            if (resource == null) throw new ArgumentNullException("resource");
            #endregion

            if (_isPortable) throw new IOException(Resources.NoSystemConfigInPortableMode);

            string systemConfigDir = SystemConfigDirs.Split(Path.PathSeparator).Last();
            string resourceCombined = (resource.Length == 0) ? "" : resource.Aggregate(Path.Combine);
            string path;
            try
            {
                path = (new[] {systemConfigDir, appName, resourceCombined}).Aggregate(Path.Combine);
            }
                #region Error handling
            catch (ArgumentException ex)
            {
                // Wrap exception to add context information
                throw new IOException(string.Format(Resources.InvalidConfigDir, systemConfigDir) + "\n" + ex.Message, ex);
            }
            #endregion

            // Ensure the directory part of the path exists
            string dirPath = isFile ? (Path.GetDirectoryName(path) ?? path) : path;
            CreateSecureMachineWideDir(dirPath);

            return Path.GetFullPath(path);
        }

        /// <summary>
        /// Returns a list of paths for loading a configuration resource.
        /// </summary>
        /// <param name="appName">The name of application. Used as part of the path, unless <see cref="IsPortable"/> is <c>true</c>.</param>
        /// <param name="isFile"><c>true</c> if the last part of <paramref name="resource"/> refers to a file instead of a directory.</param>
        /// <param name="resource">The path elements (directory and/or file names) of the resource to be loaded.</param>
        /// <returns>
        /// A list of fully qualified paths to use to load the resource sorted by decreasing importance.
        /// This list will always reflect the current state in the filesystem and can not be modified! It may be empty.
        /// </returns>
        [PublicAPI, NotNull]
        public static IEnumerable<string> GetLoadConfigPaths([NotNull, Localizable(false)] string appName, bool isFile, [NotNull, ItemNotNull] params string[] resource)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(appName)) throw new ArgumentNullException("appName");
            if (resource == null) throw new ArgumentNullException("resource");
            #endregion

            string resourceCombined = (resource.Length == 0) ? "" : resource.Aggregate(Path.Combine);
            string path;
            if (_isPortable)
            {
                // Check in portable base directory
                path = new[] {_portableBase, "config", resourceCombined}.Aggregate(Path.Combine);
                if ((isFile && File.Exists(path)) || (!isFile && Directory.Exists(path)))
                    yield return Path.GetFullPath(path);
            }
            else
            {
                // Check in user profile and system directories
                foreach (var dirPath in (UserConfigDir + Path.PathSeparator + SystemConfigDirs).Split(Path.PathSeparator))
                {
                    try
                    {
                        path = new[] {dirPath, appName, resourceCombined}.Aggregate(Path.Combine);
                    }
                        #region Error handling
                    catch (ArgumentException ex)
                    {
                        // Wrap exception to add context information
                        throw new IOException(string.Format(Resources.InvalidConfigDir, dirPath) + "\n" + ex.Message, ex);
                    }
                    #endregion

                    if ((isFile && File.Exists(path)) || (!isFile && Directory.Exists(path)))
                        yield return Path.GetFullPath(path);
                }
            }
        }

        /// <summary>
        /// Returns a path for storing a data resource (should not roam across different machines).
        /// </summary>
        /// <param name="appName">The name of application. Used as part of the path, unless <see cref="IsPortable"/> is <c>true</c>.</param>
        /// <param name="isFile"><c>true</c> if the last part of <paramref name="resource"/> refers to a file instead of a directory.</param>
        /// <param name="resource">The path elements (directory and/or file names) of the resource to be stored.</param>
        /// <returns>A fully qualified path to use to store the resource. Directories are guaranteed to already exist; files are not.</returns>
        /// <exception cref="IOException">A problem occurred while creating a directory.</exception>
        /// <exception cref="UnauthorizedAccessException">Creating a directory is not permitted.</exception>
        [PublicAPI, NotNull]
        public static string GetSaveDataPath([NotNull, Localizable(false)] string appName, bool isFile, [NotNull, ItemNotNull] params string[] resource)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(appName)) throw new ArgumentNullException("appName");
            if (resource == null) throw new ArgumentNullException("resource");
            #endregion

            string resourceCombined = (resource.Length == 0) ? "" : resource.Aggregate(Path.Combine);
            string path;
            try
            {
                path = (_isPortable
                    ? new[] {_portableBase, "data", resourceCombined}
                    : new[] {UserDataDir, appName, resourceCombined}).Aggregate(Path.Combine);
            }
                #region Error handling
            catch (ArgumentException ex)
            {
                // Wrap exception to add context information
                throw new IOException(string.Format(Resources.InvalidConfigDir, UserDataDir) + "\n" + ex.Message, ex);
            }
            #endregion

            // Ensure the directory part of the path exists
            string dirPath = isFile ? (Path.GetDirectoryName(path) ?? path) : path;
            if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);

            return Path.GetFullPath(path);
        }

        /// <summary>
        /// Returns a list of paths for loading a data resource (should not roam across different machines).
        /// </summary>
        /// <param name="appName">The name of application. Used as part of the path, unless <see cref="IsPortable"/> is <c>true</c>.</param>
        /// <param name="isFile"><c>true</c> if the last part of <paramref name="resource"/> refers to a file instead of a directory.</param>
        /// <param name="resource">The path elements (directory and/or file names) of the resource to be loaded.</param>
        /// <returns>
        /// A list of fully qualified paths to use to load the resource sorted by decreasing importance.
        /// This list will always reflect the current state in the filesystem and can not be modified! It may be empty.
        /// </returns>
        [PublicAPI, NotNull]
        public static IEnumerable<string> GetLoadDataPaths([NotNull, Localizable(false)] string appName, bool isFile, [NotNull, ItemNotNull] params string[] resource)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(appName)) throw new ArgumentNullException("appName");
            if (resource == null) throw new ArgumentNullException("resource");
            #endregion

            string resourceCombined = (resource.Length == 0) ? "" : resource.Aggregate(Path.Combine);
            string path;
            if (_isPortable)
            {
                // Check in portable base directory
                path = new[] {_portableBase, "data", resourceCombined}.Aggregate(Path.Combine);
                if ((isFile && File.Exists(path)) || (!isFile && Directory.Exists(path)))
                    yield return Path.GetFullPath(path);
            }
            else
            {
                // Check in user profile and system directories
                foreach (var dirPath in (UserDataDir + Path.PathSeparator + SystemDataDirs).Split(Path.PathSeparator))
                {
                    try
                    {
                        path = new[] {dirPath, appName, resourceCombined}.Aggregate(Path.Combine);
                    }
                        #region Error handling
                    catch (ArgumentException ex)
                    {
                        // Wrap exception to add context information
                        throw new IOException(string.Format(Resources.InvalidConfigDir, dirPath) + "\n" + ex.Message, ex);
                    }
                    #endregion

                    if ((isFile && File.Exists(path)) || (!isFile && Directory.Exists(path)))
                        yield return Path.GetFullPath(path);
                }
            }
        }

        /// <summary>
        /// Tries to locate a file either in <see cref="InstallBase"/>, the location of the NanoByte.Common.dll or in the PATH.
        /// </summary>
        /// <param name="fileName">The file name of the file to search for.</param>
        /// <returns>The fully qualified path of the first located instance of the file.</returns>
        /// <exception cref="IOException">The file could not be found.</exception>
        [PublicAPI, NotNull]
        public static string GetInstalledFilePath([NotNull, Localizable(false)] string fileName)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException("fileName");
            #endregion

            try
            {
                return new[] {InstallBase, Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}
                    .Concat((Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
                    .Select(x => Path.Combine(x, fileName))
                    .First(File.Exists);
            }
            catch (InvalidOperationException)
            {
                throw new IOException(string.Format(Resources.FileNotFound, fileName));
            }
        }
    }
}
