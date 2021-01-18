// Copyright Bastian Eicher
// Licensed under the MIT License

using System.ComponentModel;

namespace NanoByte.Common
{
    /// <summary>
    /// An object that has a unique human-readable name that can be used for identification in lists and sorting and that can be modified.
    /// </summary>
    /// <see cref="Collections.NamedCollection{T}"/>
    public interface INamed
    {
        /// <summary>
        /// A unique human-readable name for the object.
        /// </summary>
        [Description("A unique name for the object.")]
        string Name { get; set; }
    }
}
