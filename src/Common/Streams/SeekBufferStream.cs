﻿// Copyright Bastian Eicher
// Licensed under the MIT License

#if !NET20 && !NET40
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NanoByte.Common.Streams
{
    /// <summary>
    /// Decorator that adds seek buffering to another <see cref="Stream"/>.
    /// </summary>
    public sealed class SeekBufferStream : DelegatingStream
    {
        /// <summary>
        /// The default for the maximum number of bytes to buffer for seeking.
        /// </summary>
        public const int DefaultBufferSize = 1024 * 1024;

        /// <summary>
        /// Creates a new seek buffer stream.
        /// </summary>
        /// <param name="underlyingStream">Underlying stream to delegate to. Will be disposed together with this stream.</param>
        /// <param name="bufferSize">The maximum number of bytes to buffer for seeking.</param>
        public SeekBufferStream(Stream underlyingStream, int bufferSize = DefaultBufferSize)
            : base(underlyingStream)
        {
            if (!underlyingStream.CanRead) throw new ArgumentException("The underlying stream does not support reading.", nameof(underlyingStream));

            _buffer = new byte[bufferSize];
        }

        /// <inheritdoc/>
        public override bool CanSeek => true;

        /// <inheritdoc/>
        public override long Position { get; set; }

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin)
            => origin switch
            {
                SeekOrigin.Begin => Position = offset,
                SeekOrigin.Current => Position += offset,
                SeekOrigin.End => throw new NotSupportedException("Seeking from end is not supported."),
                _ => throw new ArgumentOutOfRangeException(nameof(origin), origin, null)
            };

        /// <summary>The current position of the <see cref="DelegatingStream.UnderlyingStream"/>.</summary>
        private long _underlyingPosition;

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (!TryReadFromBuffer(new Span<byte>(buffer, offset, count), out int read))
            {
                if (count > MaxRead) count = MaxRead;
                read = UnderlyingStream.Read(buffer, offset, count);
                _underlyingPosition += read;
                WriteToBuffer(new ReadOnlySpan<byte>(buffer, offset, read));
            }

            Position += read;
            return read;
        }

        /// <inheritdoc/>
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (!TryReadFromBuffer(new Span<byte>(buffer, offset, count), out int read))
            {
                if (count > MaxRead) count = MaxRead;
                read = await UnderlyingStream.ReadAsync(buffer, offset, count, cancellationToken);
                _underlyingPosition += read;
                WriteToBuffer(new ReadOnlySpan<byte>(buffer, offset, read));
            }

            Position += read;
            return read;
        }

#if !NETFRAMEWORK
        /// <inheritdoc/>
        public override int Read(Span<byte> buffer)
        {
            if (!TryReadFromBuffer(buffer, out int read))
            {
                if (buffer.Length > MaxRead) buffer = buffer[..MaxRead];
                read = UnderlyingStream.Read(buffer);
                _underlyingPosition += read;
                WriteToBuffer(buffer[..read]);
            }

            Position += read;
            return read;
        }

        /// <inheritdoc/>
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (!TryReadFromBuffer(buffer.Span, out int read))
            {
                if (buffer.Length > MaxRead) buffer = buffer[..MaxRead];
                read = await UnderlyingStream.ReadAsync(buffer, cancellationToken);
                _underlyingPosition += read;
                WriteToBuffer(buffer.Span);
            }

            Position += read;
            return read;
        }
#endif

        /// <summary>The memory used as a circular buffer storage.</summary>
        private readonly Memory<byte> _buffer;

        /// <summary>The index of the next byte to be written in the <see cref="_buffer"/>.</summary>
        private int _nextWriteIndex;

        /// <summary>
        /// Tries to read data from the buffer to <paramref name="output"/>.
        /// </summary>
        /// <param name="output">The span to read the data to.</param>
        /// <param name="read">The number of bytes that were read.</param>
        /// <returns><c>true</c> if data was read from the buffer; <c>false</c> if <see cref="DelegatingStream.UnderlyingStream"/> should be used instead.</returns>
        /// <exception cref="IOException">Tried to read data outside of the buffered range.</exception>
        private bool TryReadFromBuffer(Span<byte> output, out int read)
        {
            long longDiff = Position - _underlyingPosition;
            if (Math.Abs(longDiff) > _buffer.Length)
                throw new IOException($"Attempted to read from position {Position}. This is {longDiff} bytes away from the underlying stream position {_underlyingPosition}, but the seek buffer can only accomodate a difference of {_buffer.Length} bytes.");
            int diff = (int)longDiff;

            switch (diff)
            {
                case > 0: // Seek forwards by performing read on underlying stream
                    Position -= diff;
                    this.Read(diff);
                    read = 0;
                    return false;

                case 0: // Position in sync with underlying stream
                    read = 0;
                    return false;

                case < 0: // Seek backwards relative to underlying stream
                    int startIndex = (_nextWriteIndex + diff).Modulo(_buffer.Length);
                    var data = startIndex < _nextWriteIndex
                        ? _buffer[startIndex.._nextWriteIndex]
                        : _buffer[startIndex..];
                    if (data.Length > output.Length) data = data[..output.Length];
                    data.Span.CopyTo(output);
                    read = data.Length;
                    return true;
            }
        }

        /// <summary>
        /// The maximum number of bytes that may be read consecutively.
        /// </summary>
        private int MaxRead => _buffer.Length - _nextWriteIndex;

        /// <summary>
        /// Writes <paramref name="data"/> to the buffer.
        /// </summary>
        private void WriteToBuffer(ReadOnlySpan<byte> data)
        {
            data.CopyTo(_buffer.Span[_nextWriteIndex..]);
            _nextWriteIndex = (_nextWriteIndex + data.Length).Modulo(_buffer.Length);
        }
    }
}
#endif