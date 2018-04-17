// Copyright Bastian Eicher
// Licensed under the MIT License

using System;
using System.Collections.Generic;
using System.Globalization;

namespace NanoByte.Common.Collections
{
    /// <summary>
    /// This compares two <see cref="CultureInfo"/>s by alphabetically comparing their string representations.
    /// </summary>
    public sealed class CultureComparer : IComparer<CultureInfo>
    {
        /// <summary>A singleton instance of the comparer.</summary>
        public static readonly CultureComparer Instance = new CultureComparer();

        private CultureComparer() {}

        /// <inheritdoc/>
        public int Compare(CultureInfo x, CultureInfo y)
        {
            #region Sanity checks
            if (x == null) throw new ArgumentNullException(nameof(x));
            if (y == null) throw new ArgumentNullException(nameof(y));
            #endregion

            // ReSharper disable once PossibleUnintendedReferenceComparison
            if (x == y) return 0;
            return StringComparer.Ordinal.Compare(x.ToString(), y.ToString());
        }
    }
}
