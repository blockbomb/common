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

using ICSharpCode.SharpZipLib.Zip;

namespace NanoByte.Common.Storage.SlimDX
{
    /// <summary>
    /// Represents a file in a content archive.
    /// </summary>
    internal struct ContentArchiveEntry
    {
        /// <summary>
        /// The archive containing the file.
        /// </summary>
        public ZipFile ZipFile { get; }

        /// <summary>
        /// The actual content file.
        /// </summary>
        public ZipEntry ZipEntry { get; }

        /// <summary>
        /// Creates a new content file representation
        /// </summary>
        /// <param name="zipFile">The archive containing the file</param>
        /// <param name="zipEntry">The actual content file</param>
        public ContentArchiveEntry(ZipFile zipFile, ZipEntry zipEntry)
        {
            ZipFile = zipFile;
            ZipEntry = zipEntry;
        }
    }
}
