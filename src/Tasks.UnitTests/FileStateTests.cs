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
    /// <summary>
    /// Test FileState utility class
    /// </summary>
    public class FileStateTests
    {
        [TestMethod]
        public void BadNoName()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new FileState("");
            });
        }
        [TestMethod]
        public void BadCharsCtorOK()
        {
            new FileState("|");
        }

        [TestMethod]
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

        [LongPathSupportDisabledFact]
        public void BadTooLongLastWriteTime()
        {
            Helpers.VerifyAssertThrowsSameWay(
                delegate () { var x = new FileInfo(new String('x', 5000)).LastWriteTime; },
                delegate () { var x = new FileState(new String('x', 5000)).LastWriteTime; });
        }

        [TestMethod]
        public void Exists()
        {
            string file = null;

            try
            {
                file = FileUtilities.GetTemporaryFile();
                FileInfo info = new FileInfo(file);
                FileState state = new FileState(file);

                Assert.Equal(info.Exists, state.FileExists);
            }
            finally
            {
                File.Delete(file);
            }
        }

        [TestMethod]
        public void Name()
        {
            string file = null;

            try
            {
                file = FileUtilities.GetTemporaryFile();
                FileInfo info = new FileInfo(file);
                FileState state = new FileState(file);

                Assert.Equal(info.FullName, state.Name);
            }
            finally
            {
                File.Delete(file);
            }
        }

        [TestMethod]
        public void IsDirectoryTrue()
        {
            var state = new FileState(Path.GetTempPath());

            Assert.True(state.IsDirectory);
        }

        [TestMethod]
        public void LastWriteTime()
        {
            string file = null;

            try
            {
                file = FileUtilities.GetTemporaryFile();
                FileInfo info = new FileInfo(file);
                FileState state = new FileState(file);

                Assert.Equal(info.LastWriteTime, state.LastWriteTime);
            }
            finally
            {
                File.Delete(file);
            }
        }

        [TestMethod]
        public void LastWriteTimeUtc()
        {
            string file = null;

            try
            {
                file = FileUtilities.GetTemporaryFile();
                FileInfo info = new FileInfo(file);
                FileState state = new FileState(file);

                Assert.Equal(info.LastWriteTimeUtc, state.LastWriteTimeUtcFast);
            }
            finally
            {
                File.Delete(file);
            }
        }

        [TestMethod]
        public void Length()
        {
            string file = null;

            try
            {
                file = FileUtilities.GetTemporaryFile();
                FileInfo info = new FileInfo(file);
                FileState state = new FileState(file);

                Assert.Equal(info.Length, state.Length);
            }
            finally
            {
                File.Delete(file);
            }
        }

        [TestMethod]
        public void ReadOnly()
        {
            string file = null;

            try
            {
                file = FileUtilities.GetTemporaryFile();
                FileInfo info = new FileInfo(file);
                FileState state = new FileState(file);

                Assert.Equal(info.IsReadOnly, state.IsReadOnly);
            }
            finally
            {
                File.Delete(file);
            }
        }

        [TestMethod]
        public void ExistsReset()
        {
            string file = null;

            try
            {
                file = FileUtilities.GetTemporaryFile();
                FileInfo info = new FileInfo(file);
                FileState state = new FileState(file);

                Assert.Equal(info.Exists, state.FileExists);
                File.Delete(file);
                Assert.True(state.FileExists);
                state.Reset();
                Assert.False(state.FileExists);
            }
            finally
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
        }

        [TestMethod]
        public void NameReset()
        {
            string file = null;

            try
            {
                file = FileUtilities.GetTemporaryFile();
                FileInfo info = new FileInfo(file);
                FileState state = new FileState(file);

                Assert.Equal(info.FullName, state.Name);
                string originalName = info.FullName;
                string oldFile = file;
                file = oldFile + "2";
                File.Move(oldFile, file);
                Assert.Equal(originalName, state.Name);
                state.Reset();
                Assert.Equal(originalName, state.Name); // Name is from the constructor, didn't change
            }
            finally
            {
                File.Delete(file);
            }
        }

        [TestMethod]
        public void LastWriteTimeReset()
        {
            string file = null;

            try
            {
                file = FileUtilities.GetTemporaryFile();
                FileInfo info = new FileInfo(file);
                FileState state = new FileState(file);

                Assert.Equal(info.LastWriteTime, state.LastWriteTime);

                var time = new DateTime(2111, 1, 1);
                info.LastWriteTime = time;

                Assert.NotEqual(time, state.LastWriteTime);
                state.Reset();
                Assert.Equal(time, state.LastWriteTime);
            }
            finally
            {
                File.Delete(file);
            }
        }

        [TestMethod]
        public void LastWriteTimeUtcReset()
        {
            string file = null;

            try
            {
                file = FileUtilities.GetTemporaryFile();
                FileInfo info = new FileInfo(file);
                FileState state = new FileState(file);

                Assert.Equal(info.LastWriteTimeUtc, state.LastWriteTimeUtcFast);

                var time = new DateTime(2111, 1, 1);
                info.LastWriteTime = time;

                Assert.NotEqual(time.ToUniversalTime(), state.LastWriteTimeUtcFast);
                state.Reset();
                Assert.Equal(time.ToUniversalTime(), state.LastWriteTimeUtcFast);
            }
            finally
            {
                File.Delete(file);
            }
        }

        [TestMethod]
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

                Assert.Equal(info.Length, state.Length);
                File.WriteAllText(file, "x");

                Assert.Equal(info.Length, state.Length);
                state.Reset();
                info.Refresh();
                Assert.Equal(info.Length, state.Length);
            }
            finally
            {
                File.Delete(file);
            }
        }

        [TestMethod]
        public void ReadOnlyReset()
        {
            string file = null;

            try
            {
                file = FileUtilities.GetTemporaryFile();
                FileInfo info = new FileInfo(file);
                FileState state = new FileState(file);

                Assert.Equal(info.IsReadOnly, state.IsReadOnly);
                info.IsReadOnly = !info.IsReadOnly;
                state.Reset();
                Assert.True(state.IsReadOnly);
            }
            finally
            {
                (new FileInfo(file)).IsReadOnly = false;
                File.Delete(file);
            }
        }

        [TestMethod]
        public void ExistsButDirectory()
        {
            Assert.Equal(new FileInfo(Path.GetTempPath()).Exists, new FileState(Path.GetTempPath()).FileExists);
            Assert.True(new FileState(Path.GetTempPath()).IsDirectory);
        }

        [TestMethod]
        public void ReadOnlyOnDirectory()
        {
            Assert.Equal(new FileInfo(Path.GetTempPath()).IsReadOnly, new FileState(Path.GetTempPath()).IsReadOnly);
        }

        [TestMethod]
        public void LastWriteTimeOnDirectory()
        {
            Assert.Equal(new FileInfo(Path.GetTempPath()).LastWriteTime, new FileState(Path.GetTempPath()).LastWriteTime);
        }

        [TestMethod]
        public void LastWriteTimeUtcOnDirectory()
        {
            Assert.Equal(new FileInfo(Path.GetTempPath()).LastWriteTimeUtc, new FileState(Path.GetTempPath()).LastWriteTimeUtcFast);
        }

        [TestMethod]
        public void LengthOnDirectory()
        {
            Helpers.VerifyAssertThrowsSameWay(delegate () { var x = new FileInfo(Path.GetTempPath()).Length; }, delegate () { var x = new FileState(Path.GetTempPath()).Length; });
        }

        [TestMethod]
        [Trait("Category", "netcore-osx-failing")]
        [Trait("Category", "netcore-linux-failing")]
        public void DoesNotExistLastWriteTime()
        {
            string file = Guid.NewGuid().ToString("N");

            Assert.Equal(new FileInfo(file).LastWriteTime, new FileState(file).LastWriteTime);
        }

        [TestMethod]
        [Trait("Category", "netcore-osx-failing")]
        [Trait("Category", "netcore-linux-failing")]
        public void DoesNotExistLastWriteTimeUtc()
        {
            string file = Guid.NewGuid().ToString("N");

            Assert.Equal(new FileInfo(file).LastWriteTimeUtc, new FileState(file).LastWriteTimeUtcFast);
        }

        [TestMethod]
        public void DoesNotExistLength()
        {
            string file = Guid.NewGuid().ToString("N"); // presumably doesn't exist

            Helpers.VerifyAssertThrowsSameWay(delegate () { var x = new FileInfo(file).Length; }, delegate () { var x = new FileState(file).Length; });
        }

        [TestMethod]
        public void DoesNotExistIsDirectory()
        {
            Assert.Throws<FileNotFoundException>(() =>
            {
                string file = Guid.NewGuid().ToString("N"); // presumably doesn't exist

                var x = new FileState(file).IsDirectory;
            });
        }
        [TestMethod]
        public void DoesNotExistDirectoryOrFileExists()
        {
            string file = Guid.NewGuid().ToString("N"); // presumably doesn't exist

            Assert.Equal(Directory.Exists(file), new FileState(file).DirectoryExists);
        }

        [TestMethod]
        public void DoesNotExistParentFolderNotFound()
        {
            string file = Guid.NewGuid().ToString("N") + "\\x"; // presumably doesn't exist

            Assert.False(new FileState(file).FileExists);
            Assert.False(new FileState(file).DirectoryExists);
        }
    }
}
