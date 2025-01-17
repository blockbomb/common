﻿// Copyright Bastian Eicher
// Licensed under the MIT License

namespace NanoByte.Common.Drawing;

/// <summary>
/// Wraps an <see cref="Image"/> and provides and caches scaled versions of it.
/// </summary>
public sealed class ScalableImage : IDisposable
{
    private readonly Image _baseImage;
    private readonly Dictionary<SizeF, Image> _scaledImages = new();

    /// <summary>
    /// Creates a new scalable image.
    /// </summary>
    /// <param name="image">The base image.</param>
    public ScalableImage(Image image)
    {
        _baseImage = image;
    }

    /// <summary>
    /// Returns a copy of the base image scaled by the specified <paramref name="factor"/>.
    /// </summary>
    public Image Get(SizeF factor)
        => factor == new SizeF(1, 1)
            ? _baseImage
            : _scaledImages.GetOrAdd(factor, () => _baseImage.Scale(factor));

    /// <summary>
    /// Disposes the base image and any scaled images returned by <see cref="Get"/>.
    /// </summary>
    public void Dispose()
    {
        _baseImage.Dispose();
        foreach (var image in _scaledImages.Values)
            image.Dispose();
    }
}
