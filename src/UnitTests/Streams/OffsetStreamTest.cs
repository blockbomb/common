﻿// Copyright Bastian Eicher
// Licensed under the MIT License

using System.IO;
using FluentAssertions;
using Xunit;

namespace NanoByte.Common.Streams
{
    /// <summary>
    /// Contains test methods for <see cref="OffsetStream"/>.
    /// </summary>
    public class OffsetStreamTest
    {
        private readonly MemoryStream _underlyingStream = new(new byte[] {0, 1, 2, 3, 4});
        private readonly OffsetStream _stream;

        public OffsetStreamTest()
        {
            _stream = new(_underlyingStream, 2);
        }

        [Fact]
        public void TestPosition()
        {
            _stream.Position = 2;
            _underlyingStream.Position.Should().Be(4);
        }

        [Fact]
        public void TestSeekBegin()
        {
            _stream.Position = _stream.Length - 1;
            _stream.Seek(2, SeekOrigin.Begin).Should().Be(2);
            _underlyingStream.Position.Should().Be(4);
        }

        [Fact]
        public void TestSeekCurrent()
        {
            _stream.Position = 2;
            _stream.Seek(1, SeekOrigin.Current).Should().Be(3);
            _underlyingStream.Position.Should().Be(5);
        }

        [Fact]
        public void TestReadAll()
        {
            _stream.ReadAll()
                   .Should().Equal(2, 3, 4);
        }
    }
}