// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Globbing;
using Microsoft.Build.Shared;
using Xunit;

#nullable disable

namespace Microsoft.Build.Engine.UnitTests.Globbing
{
    [TestClass]
    /// <summary>
    ///     Actual parsing tests are covered by FileMatcher_Tests (e.g. FileMatcher_Tests.SplitFileSpec)/>
    /// </summary>
    public class MSBuildGlob_Tests
    {
        [Theory]
        [InlineData("")]
        [InlineData("a")]
#if TEST_ISWINDOWS
        [InlineData(@"c:\a")]
#else
        [InlineData("/a")]
#endif
        public void GlobRootEndsWithTrailingSlash(string globRoot)
        {
            var glob = MSBuildGlob.Parse(globRoot, "*");

            Assert.AreEqual(glob.TestOnlyGlobRoot.LastOrDefault(), Path.DirectorySeparatorChar);
        }

        [Fact]
        public void GlobFromAbsoluteRootDoesNotChangeTheRoot()
        {
            var globRoot = NativeMethodsShared.IsWindows ? @"c:\a" : "/a";
            var glob = MSBuildGlob.Parse(globRoot, "*");

            var expectedRoot = Path.Combine(Directory.GetCurrentDirectory(), globRoot).WithTrailingSlash();
            Assert.AreEqual(expectedRoot, glob.TestOnlyGlobRoot);
        }

        [Fact]
        public void GlobFromEmptyRootUsesCurrentDirectory()
        {
            var glob = MSBuildGlob.Parse(string.Empty, "*");

            Assert.AreEqual(Directory.GetCurrentDirectory().WithTrailingSlash(), glob.TestOnlyGlobRoot);
        }

        [Fact]
        public void GlobFromNullRootThrows()
        {
            Assert.Throws<ArgumentNullException>(() => MSBuildGlob.Parse(null, "*"));
        }

        [Fact]
        public void GlobFromRelativeGlobRootNormalizesRootAgainstCurrentDirectory()
        {
            var globRoot = "a/b/c";
            var glob = MSBuildGlob.Parse(globRoot, "*");

            var expectedRoot = NormalizeRelativePathForGlobRepresentation(globRoot);
            Assert.AreEqual(expectedRoot, glob.TestOnlyGlobRoot);
        }

        [Fact]
        public void GlobFromRootWithInvalidPathThrows()
        {
            for (int i = 0; i < 128; i++)
            {
                if (FileUtilities.InvalidPathChars.Contains((char)i))
                {
                    Assert.Throws<ArgumentException>(() => MSBuildGlob.Parse(((char)i).ToString(), "*"));
                }
            }
        }

        [Theory]
        [InlineData(
            "a/b/c",
            "**",
            "a/b/c")]
        [InlineData(
            "a/b/c",
            "../../**",
            "a")]
        [InlineData(
            "a/b/c",
            "../d/e/**",
            "a/b/d/e")]
        public void GlobWithRelativeFixedDirectoryPartShouldMismatchTheGlobRoot(string globRoot, string filespec, string expectedFixedDirectoryPart)
        {
            var glob = MSBuildGlob.Parse(globRoot, filespec);

            var expectedFixedDirectory = NormalizeRelativePathForGlobRepresentation(expectedFixedDirectoryPart);

            Assert.AreEqual(expectedFixedDirectory, glob.FixedDirectoryPart);
        }

        [Fact]
        public void GlobFromNullFileSpecThrows()
        {
            Assert.Throws<ArgumentNullException>(() => MSBuildGlob.Parse(null));
        }

        // Smoke test. Comprehensive parsing tests in FileMatcher

        [Fact]
        public void GlobParsingShouldInitializeState()
        {
            var globRoot = NativeMethodsShared.IsWindows ? @"c:\a" : "/a";
            var fileSpec = $"b/**/*.cs";
            var glob = MSBuildGlob.Parse(globRoot, fileSpec);

            var expectedFixedDirectory = Path.Combine(globRoot, "b").WithTrailingSlash();

            Assert.IsTrue(glob.IsLegal);
            Assert.IsTrue(glob.TestOnlyNeedsRecursion);
            Assert.AreEqual(fileSpec, glob.TestOnlyFileSpec);

            Assert.AreEqual(globRoot.WithTrailingSlash(), glob.TestOnlyGlobRoot);
            Assert.StartsWith(glob.TestOnlyGlobRoot, glob.FixedDirectoryPart);

            Assert.AreEqual(expectedFixedDirectory, glob.FixedDirectoryPart);
            Assert.AreEqual("**/", glob.WildcardDirectoryPart);
            Assert.AreEqual("*.cs", glob.FilenamePart);
        }

        [Fact]
        public void GlobParsingShouldInitializePartialStateOnIllegalFileSpec()
        {
            var globRoot = NativeMethodsShared.IsWindows ? @"c:\a" : "/a";
            var illegalFileSpec = $"b/.../**/*.cs";
            var glob = MSBuildGlob.Parse(globRoot, illegalFileSpec);

            Assert.IsFalse(glob.IsLegal);
            Assert.IsFalse(glob.TestOnlyNeedsRecursion);
            Assert.AreEqual(illegalFileSpec, glob.TestOnlyFileSpec);

            Assert.AreEqual(globRoot.WithTrailingSlash(), glob.TestOnlyGlobRoot);

            Assert.AreEqual(string.Empty, glob.FixedDirectoryPart);
            Assert.AreEqual(string.Empty, glob.WildcardDirectoryPart);
            Assert.AreEqual(string.Empty, glob.FilenamePart);

            Assert.IsFalse(glob.IsMatch($"b/.../c/d.cs"));
        }

        [Fact]
        public void GlobParsingShouldDeduplicateRegexes()
        {
            var globRoot = NativeMethodsShared.IsWindows ? @"c:\a" : "/a";
            var fileSpec = $"b/**/*.cs";
            var glob1 = MSBuildGlob.Parse(globRoot, fileSpec);
            var glob2 = MSBuildGlob.Parse(globRoot, fileSpec);

            Assert.AreSame(glob1.TestOnlyRegex, glob2.TestOnlyRegex);
        }

        [Fact]
        public void GlobIsNotUnescaped()
        {
            var glob = MSBuildGlob.Parse("%42/%42");

            Assert.IsTrue(glob.IsLegal);
            Assert.EndsWith("%42" + Path.DirectorySeparatorChar, glob.FixedDirectoryPart);
            Assert.AreEqual(string.Empty, glob.WildcardDirectoryPart);
            Assert.AreEqual("%42", glob.FilenamePart);
        }

        [Fact]
        public void GlobMatchingShouldThrowOnNullMatchArgument()
        {
            var glob = MSBuildGlob.Parse("*");

            Assert.Throws<ArgumentNullException>(() => glob.IsMatch(null));
        }

        [Fact]
        public void GlobMatchShouldReturnFalseIfArgumentContainsInvalidPathOrFileCharacters()
        {
            var glob = MSBuildGlob.Parse("*");

            for (int i = 0; i < 128; i++)
            {
                if (FileUtilities.InvalidPathChars.Contains((char)i))
                {
                    Assert.IsFalse(glob.IsMatch(((char)i).ToString()));
                }
            }

            foreach (var invalidFileChar in FileUtilities.InvalidFileNameCharsArray)
            {
                if (invalidFileChar == '\\' || invalidFileChar == '/')
                {
                    continue;
                }

                Assert.IsFalse(glob.IsMatch(invalidFileChar.ToString()));
            }
        }

        [Fact]
        public void GlobMatchingBehaviourWhenInputIsASingleSlash()
        {
            var glob = MSBuildGlob.Parse("*");

            if (NativeMethodsShared.IsUnixLike)
            {
                // \ is an acceptable file character on Unix, * should match it
                Assert.IsTrue(glob.IsMatch("\\"));

                // / means root on Unix
                Assert.IsFalse(glob.IsMatch("/"));
            }
            else
            {
                // \ means partition root on Windows
                Assert.IsFalse(glob.IsMatch("\\"));

                // / also means partition root on Windows
                Assert.IsFalse(glob.IsMatch("/"));
            }
        }

        [Fact]
        public void GlobMatchingShouldWorkWithLiteralStrings()
        {
            var glob = MSBuildGlob.Parse("abc");

            Assert.IsTrue(glob.IsMatch("abc"));
        }

        // Just a smoke test. Comprehensive tests in FileMatcher_Tests

        [Fact]
        public void GlobMatchingShouldWorkWithWildcards()
        {
            var glob = MSBuildGlob.Parse("ab?c*");

            Assert.IsFalse(glob.IsMatch("acd"));
        }

        [Fact]
        public void GlobMatchingShouldNotUnescapeInput()
        {
            var glob = MSBuildGlob.Parse("%42");

            Assert.IsFalse(glob.IsMatch("B"));
        }

        [Theory]
        [InlineData("")]
        [InlineData("a/b/c")]
        public void GlobShouldMatchEmptyArgWhenGlobIsEmpty(string globRoot)
        {
            var glob = MSBuildGlob.Parse(globRoot, string.Empty);

            Assert.IsTrue(glob.IsMatch(string.Empty));
        }

        [Theory]
        [InlineData("")]
        [InlineData("a/b/c")]
        public void GlobShouldMatchEmptyArgWhenGlobAcceptsEmptyString(string globRoot)
        {
            var glob = MSBuildGlob.Parse(globRoot, "*");

            Assert.IsTrue(glob.IsMatch(string.Empty));
        }

        [Theory]
        [InlineData("")]
        [InlineData("a/b/c")]
        public void GlobShouldNotMatchEmptyArgWhenGlobDoesNotRepresentEmpty(string globRoot)
        {
            var glob = MSBuildGlob.Parse(globRoot, "*a*");

            Assert.IsFalse(glob.IsMatch(string.Empty));
        }

        [Fact(Skip = "TODO")]
        public void GlobMatchingShouldTreatIllegalFileSpecAsLiteral()
        {
            var illegalSpec = "|...*";
            var glob = MSBuildGlob.Parse(illegalSpec);

            Assert.IsFalse(glob.IsLegal);
            Assert.IsTrue(glob.IsMatch(illegalSpec));
        }

        public static IEnumerable<object[]> GlobMatchingShouldRespectTheRootOfTheGlobTestData => GlobbingTestData.GlobbingConesTestData;

        [Theory]

        [MemberData(nameof(GlobMatchingShouldRespectTheRootOfTheGlobTestData))]

        public void GlobMatchingShouldRespectTheRootOfTheGlob(string fileSpec, string stringToMatch, string globRoot, bool shouldMatch)
        {
            var glob = MSBuildGlob.Parse(globRoot, fileSpec);

            if (shouldMatch)
            {
                Assert.IsTrue(glob.IsMatch(stringToMatch));
            }
            else
            {
                Assert.IsFalse(glob.IsMatch(stringToMatch));
            }
        }

        private static string NormalizeRelativePathForGlobRepresentation(string expectedFixedDirectoryPart)
        {
            return Path.Combine(Directory.GetCurrentDirectory(), expectedFixedDirectoryPart).Replace("/", "\\").WithTrailingSlash();
        }

        [Fact]
        public void GlobMatchingShouldWorkWithComplexRelativeLiterals()
        {
            var glob = MSBuildGlob.Parse("u/x", "../../u/x/d11/d21/../d22/../../d12/a.cs");

            Assert.IsTrue(glob.IsMatch(@"../x/d13/../../x/d12/d23/../a.cs"));
            Assert.IsFalse(glob.IsMatch(@"../x/d13/../x/d12/d23/../a.cs"));
        }

        [Theory]
        [InlineData(
            @"a/b\c",
            @"d/e\f/**\a.cs",
            @"d\e/f\g/h\i/a.cs",
            @"d\e/f\", @"g/h\i/", @"a.cs")]
        [InlineData(
            @"a/b\c",
            @"d/e\f/*b*\*.cs",
            @"d\e/f\abc/a.cs",
            @"d\e/f\", @"abc/", @"a.cs")]
        [InlineData(
            @"a/b/\c",
            @"d/e\/*b*/\*.cs",
            @"d\e\\abc/\a.cs",
            @"d\e\\", @"abc\\", @"a.cs")]
        public void GlobMatchingIgnoresSlashOrientationAndRepetitions(string globRoot, string fileSpec, string stringToMatch,
            string fixedDirectoryPart, string wildcardDirectoryPart, string filenamePart)
        {
            var glob = MSBuildGlob.Parse(globRoot, fileSpec);

            Assert.IsTrue(glob.IsMatch(stringToMatch));

            MSBuildGlob.MatchInfoResult result = glob.MatchInfo(stringToMatch);
            Assert.IsTrue(result.IsMatch);

            string NormalizeSlashes(string path)
            {
                string normalizedPath = path.Replace(Path.DirectorySeparatorChar == '/' ? '\\' : '/', Path.DirectorySeparatorChar);
                return NativeMethodsShared.IsWindows ? normalizedPath.Replace("\\\\", "\\") : normalizedPath;
            }

            var rootedFixedDirectoryPart = Path.Combine(FileUtilities.NormalizePath(globRoot), fixedDirectoryPart);

            Assert.AreEqual(FileUtilities.GetFullPathNoThrow(rootedFixedDirectoryPart), result.FixedDirectoryPartMatchGroup);
            Assert.AreEqual(NormalizeSlashes(wildcardDirectoryPart), result.WildcardDirectoryPartMatchGroup);
            Assert.AreEqual(NormalizeSlashes(filenamePart), result.FilenamePartMatchGroup);
        }
    }
}
