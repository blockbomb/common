// Copyright Bastian Eicher
// Licensed under the MIT License

using System.IO;
using FluentAssertions;
using NanoByte.Common.Storage;
using Xunit;

namespace NanoByte.Common.Cli
{
    /// <summary>
    /// Contains test methods for <see cref="ArgumentUtils"/>.
    /// </summary>
    [Collection("WorkingDir")]
    public class ArgumentUtilsTest
    {
        [Fact]
        public void TestGetFilesAbsolute()
        {
            using (var tempDir = new TemporaryDirectory("unit-tests"))
            {
                File.WriteAllText(Path.Combine(tempDir, "a.txt"), @"a");
                File.WriteAllText(Path.Combine(tempDir, "b.txt"), @"b");
                File.WriteAllText(Path.Combine(tempDir, "c.inf"), @"c");
                File.WriteAllText(Path.Combine(tempDir, "d.nfo"), @"d");

                string subdirPath = Path.Combine(tempDir, "dir");
                Directory.CreateDirectory(subdirPath);
                File.WriteAllText(Path.Combine(subdirPath, "1.txt"), @"1");
                File.WriteAllText(Path.Combine(subdirPath, "2.inf"), @"a");

                var result = ArgumentUtils.GetFiles(new[]
                {
                    Path.Combine(tempDir, "*.txt"), // Wildcard
                    Path.Combine(tempDir, "d.nfo"), // Specifc file
                    subdirPath // Directory with implict default wildcard
                }, "*.txt");
                result[0].FullName.Should().Be(Path.Combine(tempDir, "a.txt"));
                result[1].FullName.Should().Be(Path.Combine(tempDir, "b.txt"));
                result[2].FullName.Should().Be(Path.Combine(tempDir, "d.nfo"));
                result[3].FullName.Should().Be(Path.Combine(subdirPath, "1.txt"));
            }
        }

        [Fact]
        public void TestGetFilesRelative()
        {
            using (var tempDir = new TemporaryWorkingDirectory("unit-tests"))
            {
                File.WriteAllText("a.txt", @"a");
                File.WriteAllText("b.txt", @"b");
                File.WriteAllText("c.inf", @"c");
                File.WriteAllText("d.nfo", @"d");

                const string subdirPath = "dir";
                Directory.CreateDirectory(subdirPath);
                File.WriteAllText(Path.Combine(subdirPath, "1.txt"), @"1");
                File.WriteAllText(Path.Combine(subdirPath, "2.inf"), @"a");

                var result = ArgumentUtils.GetFiles(new[]
                {
                    "*.txt", // Wildcard
                    "d.nfo", // Specifc file
                    subdirPath // Directory with implict default wildcard
                }, "*.txt");
                result[0].FullName.Should().Be(Path.Combine(tempDir, "a.txt"));
                result[1].FullName.Should().Be(Path.Combine(tempDir, "b.txt"));
                result[2].FullName.Should().Be(Path.Combine(tempDir, "d.nfo"));
                Path.Combine(tempDir, subdirPath, "1.txt").Should().Be(result[3].FullName);
            }
        }
    }
}
