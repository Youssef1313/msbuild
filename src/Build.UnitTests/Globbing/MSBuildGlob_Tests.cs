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
    /// <summary>
    ///     Actual parsing tests are covered by FileMatcher_Tests (e.g. FileMatcher_Tests.SplitFileSpec)/>
    /// </summary>
    public class MSBuildGlob_Tests
    {
        [TestMethod]
        [DataRow("")]
        [DataRow("a")]
#if TEST_ISWINDOWS
        [DataRow(@"c:\a")]
#else
        [DataRow("/a")]
#endif
        public void GlobRootEndsWithTrailingSlash(string globRoot)
        {
            var glob = MSBuildGlob.Parse(globRoot, "*");

            Assert.Equal(glob.TestOnlyGlobRoot.LastOrDefault(), Path.DirectorySeparatorChar);
        }

        [TestMethod]
        public void GlobFromAbsoluteRootDoesNotChangeTheRoot()
        {
            var globRoot = NativeMethodsShared.IsWindows ? @"c:\a" : "/a";
            var glob = MSBuildGlob.Parse(globRoot, "*");

            var expectedRoot = Path.Combine(Directory.GetCurrentDirectory(), globRoot).WithTrailingSlash();
            Assert.Equal(expectedRoot, glob.TestOnlyGlobRoot);
        }

        [TestMethod]
        public void GlobFromEmptyRootUsesCurrentDirectory()
        {
            var glob = MSBuildGlob.Parse(string.Empty, "*");

            Assert.Equal(Directory.GetCurrentDirectory().WithTrailingSlash(), glob.TestOnlyGlobRoot);
        }

        [TestMethod]
        public void GlobFromNullRootThrows()
        {
            Assert.Throws<ArgumentNullException>(() => MSBuildGlob.Parse(null, "*"));
        }

        [TestMethod]
        public void GlobFromRelativeGlobRootNormalizesRootAgainstCurrentDirectory()
        {
            var globRoot = "a/b/c";
            var glob = MSBuildGlob.Parse(globRoot, "*");

            var expectedRoot = NormalizeRelativePathForGlobRepresentation(globRoot);
            Assert.Equal(expectedRoot, glob.TestOnlyGlobRoot);
        }

        [TestMethod]
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

        [TestMethod]
        [DataRow(
            "a/b/c",
            "**",
            "a/b/c")]
        [DataRow(
            "a/b/c",
            "../../**",
            "a")]
        [DataRow(
            "a/b/c",
            "../d/e/**",
            "a/b/d/e")]
        public void GlobWithRelativeFixedDirectoryPartShouldMismatchTheGlobRoot(string globRoot, string filespec, string expectedFixedDirectoryPart)
        {
            var glob = MSBuildGlob.Parse(globRoot, filespec);

            var expectedFixedDirectory = NormalizeRelativePathForGlobRepresentation(expectedFixedDirectoryPart);

            Assert.Equal(expectedFixedDirectory, glob.FixedDirectoryPart);
        }

        [TestMethod]
        public void GlobFromNullFileSpecThrows()
        {
            Assert.Throws<ArgumentNullException>(() => MSBuildGlob.Parse(null));
        }

        // Smoke test. Comprehensive parsing tests in FileMatcher

        [TestMethod]
        public void GlobParsingShouldInitializeState()
        {
            var globRoot = NativeMethodsShared.IsWindows ? @"c:\a" : "/a";
            var fileSpec = $"b/**/*.cs";
            var glob = MSBuildGlob.Parse(globRoot, fileSpec);

            var expectedFixedDirectory = Path.Combine(globRoot, "b").WithTrailingSlash();

            Assert.True(glob.IsLegal);
            Assert.True(glob.TestOnlyNeedsRecursion);
            Assert.Equal(fileSpec, glob.TestOnlyFileSpec);

            Assert.Equal(globRoot.WithTrailingSlash(), glob.TestOnlyGlobRoot);
            Assert.StartsWith(glob.TestOnlyGlobRoot, glob.FixedDirectoryPart);

            Assert.Equal(expectedFixedDirectory, glob.FixedDirectoryPart);
            Assert.Equal("**/", glob.WildcardDirectoryPart);
            Assert.Equal("*.cs", glob.FilenamePart);
        }

        [TestMethod]
        public void GlobParsingShouldInitializePartialStateOnIllegalFileSpec()
        {
            var globRoot = NativeMethodsShared.IsWindows ? @"c:\a" : "/a";
            var illegalFileSpec = $"b/.../**/*.cs";
            var glob = MSBuildGlob.Parse(globRoot, illegalFileSpec);

            Assert.False(glob.IsLegal);
            Assert.False(glob.TestOnlyNeedsRecursion);
            Assert.Equal(illegalFileSpec, glob.TestOnlyFileSpec);

            Assert.Equal(globRoot.WithTrailingSlash(), glob.TestOnlyGlobRoot);

            Assert.Equal(string.Empty, glob.FixedDirectoryPart);
            Assert.Equal(string.Empty, glob.WildcardDirectoryPart);
            Assert.Equal(string.Empty, glob.FilenamePart);

            Assert.False(glob.IsMatch($"b/.../c/d.cs"));
        }

        [TestMethod]
        public void GlobParsingShouldDeduplicateRegexes()
        {
            var globRoot = NativeMethodsShared.IsWindows ? @"c:\a" : "/a";
            var fileSpec = $"b/**/*.cs";
            var glob1 = MSBuildGlob.Parse(globRoot, fileSpec);
            var glob2 = MSBuildGlob.Parse(globRoot, fileSpec);

            Assert.Same(glob1.TestOnlyRegex, glob2.TestOnlyRegex);
        }

        [TestMethod]
        public void GlobIsNotUnescaped()
        {
            var glob = MSBuildGlob.Parse("%42/%42");

            Assert.True(glob.IsLegal);
            Assert.EndsWith("%42" + Path.DirectorySeparatorChar, glob.FixedDirectoryPart);
            Assert.Equal(string.Empty, glob.WildcardDirectoryPart);
            Assert.Equal("%42", glob.FilenamePart);
        }

        [TestMethod]
        public void GlobMatchingShouldThrowOnNullMatchArgument()
        {
            var glob = MSBuildGlob.Parse("*");

            Assert.Throws<ArgumentNullException>(() => glob.IsMatch(null));
        }

        [TestMethod]
        public void GlobMatchShouldReturnFalseIfArgumentContainsInvalidPathOrFileCharacters()
        {
            var glob = MSBuildGlob.Parse("*");

            for (int i = 0; i < 128; i++)
            {
                if (FileUtilities.InvalidPathChars.Contains((char)i))
                {
                    Assert.False(glob.IsMatch(((char)i).ToString()));
                }
            }

            foreach (var invalidFileChar in FileUtilities.InvalidFileNameCharsArray)
            {
                if (invalidFileChar == '\\' || invalidFileChar == '/')
                {
                    continue;
                }

                Assert.False(glob.IsMatch(invalidFileChar.ToString()));
            }
        }

        [TestMethod]
        public void GlobMatchingBehaviourWhenInputIsASingleSlash()
        {
            var glob = MSBuildGlob.Parse("*");

            if (NativeMethodsShared.IsUnixLike)
            {
                // \ is an acceptable file character on Unix, * should match it
                Assert.True(glob.IsMatch("\\"));

                // / means root on Unix
                Assert.False(glob.IsMatch("/"));
            }
            else
            {
                // \ means partition root on Windows
                Assert.False(glob.IsMatch("\\"));

                // / also means partition root on Windows
                Assert.False(glob.IsMatch("/"));
            }
        }

        [TestMethod]
        public void GlobMatchingShouldWorkWithLiteralStrings()
        {
            var glob = MSBuildGlob.Parse("abc");

            Assert.True(glob.IsMatch("abc"));
        }

        // Just a smoke test. Comprehensive tests in FileMatcher_Tests

        [TestMethod]
        public void GlobMatchingShouldWorkWithWildcards()
        {
            var glob = MSBuildGlob.Parse("ab?c*");

            Assert.False(glob.IsMatch("acd"));
        }

        [TestMethod]
        public void GlobMatchingShouldNotUnescapeInput()
        {
            var glob = MSBuildGlob.Parse("%42");

            Assert.False(glob.IsMatch("B"));
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("a/b/c")]
        public void GlobShouldMatchEmptyArgWhenGlobIsEmpty(string globRoot)
        {
            var glob = MSBuildGlob.Parse(globRoot, string.Empty);

            Assert.True(glob.IsMatch(string.Empty));
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("a/b/c")]
        public void GlobShouldMatchEmptyArgWhenGlobAcceptsEmptyString(string globRoot)
        {
            var glob = MSBuildGlob.Parse(globRoot, "*");

            Assert.True(glob.IsMatch(string.Empty));
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("a/b/c")]
        public void GlobShouldNotMatchEmptyArgWhenGlobDoesNotRepresentEmpty(string globRoot)
        {
            var glob = MSBuildGlob.Parse(globRoot, "*a*");

            Assert.False(glob.IsMatch(string.Empty));
        }

        [TestMethod(Skip = "TODO")]
        public void GlobMatchingShouldTreatIllegalFileSpecAsLiteral()
        {
            var illegalSpec = "|...*";
            var glob = MSBuildGlob.Parse(illegalSpec);

            Assert.False(glob.IsLegal);
            Assert.True(glob.IsMatch(illegalSpec));
        }

        public static IEnumerable<object[]> GlobMatchingShouldRespectTheRootOfTheGlobTestData => GlobbingTestData.GlobbingConesTestData;

        [TestMethod]

        [DynamicData(nameof(GlobMatchingShouldRespectTheRootOfTheGlobTestData))]

        public void GlobMatchingShouldRespectTheRootOfTheGlob(string fileSpec, string stringToMatch, string globRoot, bool shouldMatch)
        {
            var glob = MSBuildGlob.Parse(globRoot, fileSpec);

            if (shouldMatch)
            {
                Assert.True(glob.IsMatch(stringToMatch));
            }
            else
            {
                Assert.False(glob.IsMatch(stringToMatch));
            }
        }

        private static string NormalizeRelativePathForGlobRepresentation(string expectedFixedDirectoryPart)
        {
            return Path.Combine(Directory.GetCurrentDirectory(), expectedFixedDirectoryPart).Replace("/", "\\").WithTrailingSlash();
        }

        [TestMethod]
        public void GlobMatchingShouldWorkWithComplexRelativeLiterals()
        {
            var glob = MSBuildGlob.Parse("u/x", "../../u/x/d11/d21/../d22/../../d12/a.cs");

            Assert.True(glob.IsMatch(@"../x/d13/../../x/d12/d23/../a.cs"));
            Assert.False(glob.IsMatch(@"../x/d13/../x/d12/d23/../a.cs"));
        }

        [TestMethod]
        [DataRow(
            @"a/b\c",
            @"d/e\f/**\a.cs",
            @"d\e/f\g/h\i/a.cs",
            @"d\e/f\", @"g/h\i/", @"a.cs")]
        [DataRow(
            @"a/b\c",
            @"d/e\f/*b*\*.cs",
            @"d\e/f\abc/a.cs",
            @"d\e/f\", @"abc/", @"a.cs")]
        [DataRow(
            @"a/b/\c",
            @"d/e\/*b*/\*.cs",
            @"d\e\\abc/\a.cs",
            @"d\e\\", @"abc\\", @"a.cs")]
        public void GlobMatchingIgnoresSlashOrientationAndRepetitions(string globRoot, string fileSpec, string stringToMatch,
            string fixedDirectoryPart, string wildcardDirectoryPart, string filenamePart)
        {
            var glob = MSBuildGlob.Parse(globRoot, fileSpec);

            Assert.True(glob.IsMatch(stringToMatch));

            MSBuildGlob.MatchInfoResult result = glob.MatchInfo(stringToMatch);
            Assert.True(result.IsMatch);

            string NormalizeSlashes(string path)
            {
                string normalizedPath = path.Replace(Path.DirectorySeparatorChar == '/' ? '\\' : '/', Path.DirectorySeparatorChar);
                return NativeMethodsShared.IsWindows ? normalizedPath.Replace("\\\\", "\\") : normalizedPath;
            }

            var rootedFixedDirectoryPart = Path.Combine(FileUtilities.NormalizePath(globRoot), fixedDirectoryPart);

            Assert.Equal(FileUtilities.GetFullPathNoThrow(rootedFixedDirectoryPart), result.FixedDirectoryPartMatchGroup);
            Assert.Equal(NormalizeSlashes(wildcardDirectoryPart), result.WildcardDirectoryPartMatchGroup);
            Assert.Equal(NormalizeSlashes(filenamePart), result.FilenamePartMatchGroup);
        }
    }
}
