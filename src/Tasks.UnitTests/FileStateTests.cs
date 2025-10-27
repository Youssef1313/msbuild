// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.Build.Shared;
using Microsoft.Build.Tasks;
using Xunit;

#nullable disable

namespace Microsoft.Build.UnitTests
{
    [TestClass]
    /// <summary>
    /// Test FileState utility class
    /// </summary>
    public class FileStateTests
    {
        [Fact]
        public void BadNoName()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new FileState("");
            });
        }
        [Fact]
        public void BadCharsCtorOK()
        {
            new FileState("|");
        }

        [Fact]
        public void BadTooLongCtorOK()
        {
            new FileState(new String('x', 5000));
        }

        [WindowsFullFrameworkOnlyFact(additionalMessage: ".NET Core 2.1+ no longer validates paths: https://github.com/dotnet/corefx/issues/27779#issuecomment-371253486. On Unix there is no invalid file name characters.")]
        public void BadChars()
        {
            var state = new FileState("|");
            Assert.Throws<ArgumentException>(() => { var time = state.LastWriteTime; });
        }

        [LongPathSupportCondition]
        [TestMethod]
        public void BadTooLongLastWriteTime()
        {
            Helpers.VerifyAssertThrowsSameWay(
                delegate () { var x = new FileInfo(new String('x', 5000)).LastWriteTime; },
                delegate () { var x = new FileState(new String('x', 5000)).LastWriteTime; });
        }

        [Fact]
        public void Exists()
        {
            string file = null;

            try
            {
                file = FileUtilities.GetTemporaryFile();
                FileInfo info = new FileInfo(file);
                FileState state = new FileState(file);

                Assert.AreEqual(info.Exists, state.FileExists);
            }
            finally
            {
                File.Delete(file);
            }
        }

        [Fact]
        public void Name()
        {
            string file = null;

            try
            {
                file = FileUtilities.GetTemporaryFile();
                FileInfo info = new FileInfo(file);
                FileState state = new FileState(file);

                Assert.AreEqual(info.FullName, state.Name);
            }
            finally
            {
                File.Delete(file);
            }
        }

        [Fact]
        public void IsDirectoryTrue()
        {
            var state = new FileState(Path.GetTempPath());

            Assert.IsTrue(state.IsDirectory);
        }

        [Fact]
        public void LastWriteTime()
        {
            string file = null;

            try
            {
                file = FileUtilities.GetTemporaryFile();
                FileInfo info = new FileInfo(file);
                FileState state = new FileState(file);

                Assert.AreEqual(info.LastWriteTime, state.LastWriteTime);
            }
            finally
            {
                File.Delete(file);
            }
        }

        [Fact]
        public void LastWriteTimeUtc()
        {
            string file = null;

            try
            {
                file = FileUtilities.GetTemporaryFile();
                FileInfo info = new FileInfo(file);
                FileState state = new FileState(file);

                Assert.AreEqual(info.LastWriteTimeUtc, state.LastWriteTimeUtcFast);
            }
            finally
            {
                File.Delete(file);
            }
        }

        [Fact]
        public void Length()
        {
            string file = null;

            try
            {
                file = FileUtilities.GetTemporaryFile();
                FileInfo info = new FileInfo(file);
                FileState state = new FileState(file);

                Assert.AreEqual(info.Length, state.Length);
            }
            finally
            {
                File.Delete(file);
            }
        }

        [Fact]
        public void ReadOnly()
        {
            string file = null;

            try
            {
                file = FileUtilities.GetTemporaryFile();
                FileInfo info = new FileInfo(file);
                FileState state = new FileState(file);

                Assert.AreEqual(info.IsReadOnly, state.IsReadOnly);
            }
            finally
            {
                File.Delete(file);
            }
        }

        [Fact]
        public void ExistsReset()
        {
            string file = null;

            try
            {
                file = FileUtilities.GetTemporaryFile();
                FileInfo info = new FileInfo(file);
                FileState state = new FileState(file);

                Assert.AreEqual(info.Exists, state.FileExists);
                File.Delete(file);
                Assert.IsTrue(state.FileExists);
                state.Reset();
                Assert.IsFalse(state.FileExists);
            }
            finally
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
        }

        [Fact]
        public void NameReset()
        {
            string file = null;

            try
            {
                file = FileUtilities.GetTemporaryFile();
                FileInfo info = new FileInfo(file);
                FileState state = new FileState(file);

                Assert.AreEqual(info.FullName, state.Name);
                string originalName = info.FullName;
                string oldFile = file;
                file = oldFile + "2";
                File.Move(oldFile, file);
                Assert.AreEqual(originalName, state.Name);
                state.Reset();
                Assert.AreEqual(originalName, state.Name); // Name is from the constructor, didn't change
            }
            finally
            {
                File.Delete(file);
            }
        }

        [Fact]
        public void LastWriteTimeReset()
        {
            string file = null;

            try
            {
                file = FileUtilities.GetTemporaryFile();
                FileInfo info = new FileInfo(file);
                FileState state = new FileState(file);

                Assert.AreEqual(info.LastWriteTime, state.LastWriteTime);

                var time = new DateTime(2111, 1, 1);
                info.LastWriteTime = time;

                Assert.AreNotEqual(time, state.LastWriteTime);
                state.Reset();
                Assert.AreEqual(time, state.LastWriteTime);
            }
            finally
            {
                File.Delete(file);
            }
        }

        [Fact]
        public void LastWriteTimeUtcReset()
        {
            string file = null;

            try
            {
                file = FileUtilities.GetTemporaryFile();
                FileInfo info = new FileInfo(file);
                FileState state = new FileState(file);

                Assert.AreEqual(info.LastWriteTimeUtc, state.LastWriteTimeUtcFast);

                var time = new DateTime(2111, 1, 1);
                info.LastWriteTime = time;

                Assert.AreNotEqual(time.ToUniversalTime(), state.LastWriteTimeUtcFast);
                state.Reset();
                Assert.AreEqual(time.ToUniversalTime(), state.LastWriteTimeUtcFast);
            }
            finally
            {
                File.Delete(file);
            }
        }

        [Fact]
        [Trait("Category", "netcore-osx-failing")]
        [Trait("Category", "netcore-linux-failing")]
        public void LengthReset()
        {
            string file = null;

            try
            {
                file = FileUtilities.GetTemporaryFile();
                FileInfo info = new FileInfo(file);
                FileState state = new FileState(file);

                Assert.AreEqual(info.Length, state.Length);
                File.WriteAllText(file, "x");

                Assert.AreEqual(info.Length, state.Length);
                state.Reset();
                info.Refresh();
                Assert.AreEqual(info.Length, state.Length);
            }
            finally
            {
                File.Delete(file);
            }
        }

        [Fact]
        public void ReadOnlyReset()
        {
            string file = null;

            try
            {
                file = FileUtilities.GetTemporaryFile();
                FileInfo info = new FileInfo(file);
                FileState state = new FileState(file);

                Assert.AreEqual(info.IsReadOnly, state.IsReadOnly);
                info.IsReadOnly = !info.IsReadOnly;
                state.Reset();
                Assert.IsTrue(state.IsReadOnly);
            }
            finally
            {
                (new FileInfo(file)).IsReadOnly = false;
                File.Delete(file);
            }
        }

        [Fact]
        public void ExistsButDirectory()
        {
            Assert.AreEqual(new FileInfo(Path.GetTempPath()).Exists, new FileState(Path.GetTempPath()).FileExists);
            Assert.IsTrue(new FileState(Path.GetTempPath()).IsDirectory);
        }

        [Fact]
        public void ReadOnlyOnDirectory()
        {
            Assert.AreEqual(new FileInfo(Path.GetTempPath()).IsReadOnly, new FileState(Path.GetTempPath()).IsReadOnly);
        }

        [Fact]
        public void LastWriteTimeOnDirectory()
        {
            Assert.AreEqual(new FileInfo(Path.GetTempPath()).LastWriteTime, new FileState(Path.GetTempPath()).LastWriteTime);
        }

        [Fact]
        public void LastWriteTimeUtcOnDirectory()
        {
            Assert.AreEqual(new FileInfo(Path.GetTempPath()).LastWriteTimeUtc, new FileState(Path.GetTempPath()).LastWriteTimeUtcFast);
        }

        [Fact]
        public void LengthOnDirectory()
        {
            Helpers.VerifyAssertThrowsSameWay(delegate () { var x = new FileInfo(Path.GetTempPath()).Length; }, delegate () { var x = new FileState(Path.GetTempPath()).Length; });
        }

        [Fact]
        [Trait("Category", "netcore-osx-failing")]
        [Trait("Category", "netcore-linux-failing")]
        public void DoesNotExistLastWriteTime()
        {
            string file = Guid.NewGuid().ToString("N");

            Assert.AreEqual(new FileInfo(file).LastWriteTime, new FileState(file).LastWriteTime);
        }

        [Fact]
        [Trait("Category", "netcore-osx-failing")]
        [Trait("Category", "netcore-linux-failing")]
        public void DoesNotExistLastWriteTimeUtc()
        {
            string file = Guid.NewGuid().ToString("N");

            Assert.AreEqual(new FileInfo(file).LastWriteTimeUtc, new FileState(file).LastWriteTimeUtcFast);
        }

        [Fact]
        public void DoesNotExistLength()
        {
            string file = Guid.NewGuid().ToString("N"); // presumably doesn't exist

            Helpers.VerifyAssertThrowsSameWay(delegate () { var x = new FileInfo(file).Length; }, delegate () { var x = new FileState(file).Length; });
        }

        [Fact]
        public void DoesNotExistIsDirectory()
        {
            Assert.Throws<FileNotFoundException>(() =>
            {
                string file = Guid.NewGuid().ToString("N"); // presumably doesn't exist

                var x = new FileState(file).IsDirectory;
            });
        }
        [Fact]
        public void DoesNotExistDirectoryOrFileExists()
        {
            string file = Guid.NewGuid().ToString("N"); // presumably doesn't exist

            Assert.AreEqual(Directory.Exists(file), new FileState(file).DirectoryExists);
        }

        [Fact]
        public void DoesNotExistParentFolderNotFound()
        {
            string file = Guid.NewGuid().ToString("N") + "\\x"; // presumably doesn't exist

            Assert.IsFalse(new FileState(file).FileExists);
            Assert.IsFalse(new FileState(file).DirectoryExists);
        }
    }
}
