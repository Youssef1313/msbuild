// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Build.Shared;
using Shouldly;

#nullable disable

namespace Microsoft.Build.Engine.UnitTests
{
    public class FileUtilitiesRegex_Tests
    {
        private string _directoryStart = new string(MSBuildConstants.BackslashChar[0], 2);
        private string _altDirectoryStart = new string(MSBuildConstants.ForwardSlash[0], 2);

        // below are the legacy regex used before explcitly checking these patterns to reduce allocations

        // regular expression used to match file-specs comprising exactly "<drive letter>:" (with no trailing characters)
        internal static readonly Regex DrivePattern = new Regex(@"^[A-Za-z]:$", RegexOptions.Compiled);

        // regular expression used to match file-specs beginning with "<drive letter>:"
        internal static readonly Regex StartWithDrivePattern = new Regex(@"^[A-Za-z]:", RegexOptions.Compiled);

        private static readonly string s_baseUncPattern = string.Format(
            CultureInfo.InvariantCulture,
            @"^[\{0}\{1}][\{0}\{1}][^\{0}\{1}]+[\{0}\{1}][^\{0}\{1}]+",
            '\\', '/');

        // regular expression used to match UNC paths beginning with "\\<server>\<share>"
        internal static readonly Regex StartsWithUncPattern = new Regex(s_baseUncPattern, RegexOptions.Compiled);

        // regular expression used to match UNC paths comprising exactly "\\<server>\<share>"
        internal static readonly Regex UncPattern =
            new Regex(
                string.Format(CultureInfo.InvariantCulture, @"{0}$", s_baseUncPattern),
                RegexOptions.Compiled);

        [TestMethod]
        public void DrivePatternIsMatchAllProperFormats()
        {
            string s;
            for (char i = 'A'; i <= 'Z'; i++)
            {
                s = i + ":";
                DrivePattern.IsMatch(s).ShouldBeTrue();
                s = (char)(i + ('a' - 'A')) + ":";
                DrivePattern.IsMatch(s).ShouldBeTrue();
            }
        }

        [TestMethod]
        public void IsDrivePatternAllProperFormats()
        {
            string s;
            for (char i = 'A'; i <= 'Z'; i++)
            {
                s = i + ":";
                FileUtilitiesRegex.IsDrivePattern(s).ShouldBeTrue();
                s = (char)(i + ('a' - 'A')) + ":";
                FileUtilitiesRegex.IsDrivePattern(s).ShouldBeTrue();
            }
        }

        [TestMethod]
        public void StartWithDrivePatternIsMatchAllProperFormats()
        {
            string s;
            for (char i = 'A'; i <= 'Z'; i++)
            {
                s = i + ":";
                StartWithDrivePattern.IsMatch(s).ShouldBeTrue();
                s = (char)(i + ('a' - 'A')) + ":";
                StartWithDrivePattern.IsMatch(s).ShouldBeTrue();
            }
        }

        [TestMethod]
        public void DoesStartWithDrivePatternAllProperFormats()
        {
            string s;
            for (char i = 'A'; i <= 'Z'; i++)
            {
                s = i + ":";
                FileUtilitiesRegex.StartsWithDrivePattern(s).ShouldBeTrue();
                s = (char)(i + ('a' - 'A')) + ":";
                FileUtilitiesRegex.StartsWithDrivePattern(s).ShouldBeTrue();
            }
        }

        [TestMethod]
        [DataRow(" C:")]
        [DataRow(" :")]
        [DataRow("CC:")]
        [DataRow("::")]
        [DataRow("x\\")]
        [DataRow(":/")]
        [DataRow("  ")]
        [DataRow("")]
        public void DrivePatternInvalidFormat_LegacyRegex(string value)
        {
            DrivePattern.IsMatch(value).ShouldBe(false);
            StartWithDrivePattern.IsMatch(value).ShouldBe(false);
        }

        [TestMethod]
        [DataRow(" C:")]
        [DataRow(" :")]
        [DataRow("CC:")]
        [DataRow("::")]
        [DataRow("x\\")]
        [DataRow(":/")]
        [DataRow("  ")]
        [DataRow("")]
        public void DrivePatternInvalidFormat(string value)
        {
            FileUtilitiesRegex.StartsWithDrivePattern(value).ShouldBe(false);
            FileUtilitiesRegex.IsDrivePattern(value).ShouldBe(false);
        }

        [TestMethod]
        [DataRow("C: ")]
        [DataRow("C:/")]
        [DataRow("Z:\\")]
        [DataRow("b:a\\q/")]
        public void StartWithDrivePatternInvalidPatternValidStart_LegacyRegex(string value)
        {
            StartWithDrivePattern.IsMatch(value).ShouldBeTrue();
        }

        [TestMethod]
        [DataRow("C: ")]
        [DataRow("C:/")]
        [DataRow("Z:\\")]
        [DataRow("b:a\\q/")]
        public void StartWithDrivePatternInvalidPatternValidStart(string value)
        {
            FileUtilitiesRegex.StartsWithDrivePattern(value).ShouldBeTrue();
        }

        [TestMethod]
        public void UncPatternMultiFolderDirectory_LegacyRegex()
        {
            string winDirectory = string.Format("{0}server\\path\\test", _directoryStart);
            string unixDirectory = string.Format("{0}server/path/test", _altDirectoryStart);

            UncPattern.IsMatch(winDirectory).ShouldBe(false);
            UncPattern.IsMatch(unixDirectory).ShouldBe(false);
        }

        [TestMethod]
        public void UncPatternMultiFolderDirectory()
        {
            string winDirectory = string.Format("{0}server\\path\\test\\abc", _directoryStart);
            string unixDirectory = string.Format("{0}server/path/test/abc", _altDirectoryStart);

            FileUtilitiesRegex.IsUncPattern(winDirectory).ShouldBe(false);
            FileUtilitiesRegex.IsUncPattern(unixDirectory).ShouldBe(false);
        }

        [TestMethod]
        public void StartWithUncPatternMultiFolderDirectory()
        {
            string winDirectory = string.Format("{0}server\\path\\test\\abc", _directoryStart);
            string unixDirectory = string.Format("{0}server/path/test/abc", _altDirectoryStart);

            FileUtilitiesRegex.StartsWithUncPattern(winDirectory).ShouldBeTrue();
            FileUtilitiesRegex.StartsWithUncPattern(unixDirectory).ShouldBeTrue();
        }

        [TestMethod]
        public void StartWithUncPatternMultiFolderDirectory_LegacyRegex()
        {
            string winDirectory = string.Format("{0}server\\path\\test\\abc", _directoryStart);
            string unixDirectory = string.Format("{0}server/path/test/abc", _altDirectoryStart);

            StartsWithUncPattern.IsMatch(winDirectory).ShouldBeTrue();
            StartsWithUncPattern.IsMatch(unixDirectory).ShouldBeTrue();
        }

        [TestMethod]
        public void MatchLengthStartWithUncPatternMultiFolderDirectory()
        {
            string winDirectory = string.Format("{0}server\\path\\test\\abc", _directoryStart);
            string unixDirectory = string.Format("{0}server/path/test/abc", _altDirectoryStart);

            FileUtilitiesRegex.StartsWithUncPatternMatchLength(winDirectory).ShouldBe(13);
            FileUtilitiesRegex.StartsWithUncPatternMatchLength(unixDirectory).ShouldBe(13);
        }

        [TestMethod]
        public void MatchLengthStartWithUncPatternMultiFolderDirectory_LegacyRegex()
        {
            string winDirectory = string.Format("{0}server\\path\\test\\abc", _directoryStart);
            string unixDirectory = string.Format("{0}server/path/test/abc", _altDirectoryStart);

            var match = StartsWithUncPattern.Match(winDirectory);
            match.Success.ShouldBeTrue();
            match.Length.ShouldBe(13);

            match = StartsWithUncPattern.Match(unixDirectory);
            match.Success.ShouldBeTrue();
            match.Length.ShouldBe(13);
        }

        [TestMethod]
        public void UncPatternExactDirectory()
        {
            string winDirectory = string.Format("{0}server\\path", _directoryStart);
            string unixDirectory = string.Format("{0}server/path", _altDirectoryStart);

            FileUtilitiesRegex.IsUncPattern(winDirectory).ShouldBeTrue();
            FileUtilitiesRegex.IsUncPattern(unixDirectory).ShouldBeTrue();
        }

        [TestMethod]
        public void UncPatternExactDirectory_LegacyRegex()
        {
            string winDirectory = string.Format("{0}server\\path", _directoryStart);
            string unixDirectory = string.Format("{0}server/path", _altDirectoryStart);

            UncPattern.IsMatch(winDirectory).ShouldBeTrue();
            UncPattern.IsMatch(unixDirectory).ShouldBeTrue();
        }


        [TestMethod]
        public void StartWithUncPatternExactDirectory()
        {
            string winDirectory = string.Format("{0}server\\path", _directoryStart);
            string unixDirectory = string.Format("{0}server/path", _altDirectoryStart);

            FileUtilitiesRegex.StartsWithUncPattern(winDirectory).ShouldBeTrue();
            FileUtilitiesRegex.StartsWithUncPattern(unixDirectory).ShouldBeTrue();
        }

        [TestMethod]
        public void StartWithUncPatternExactDirectory_LegacyRegex()
        {
            string winDirectory = string.Format("{0}server\\path", _directoryStart);
            string unixDirectory = string.Format("{0}server/path", _altDirectoryStart);

            StartsWithUncPattern.IsMatch(winDirectory).ShouldBeTrue();
            StartsWithUncPattern.IsMatch(unixDirectory).ShouldBeTrue();
        }

        [TestMethod]
        public void MatchLengthStartWithUncPatternExactDirectory()
        {
            string winDirectory = string.Format("{0}server\\path", _directoryStart);
            string unixDirectory = string.Format("{0}server/path", _altDirectoryStart);

            FileUtilitiesRegex.StartsWithUncPatternMatchLength(winDirectory).ShouldBe(13);
            FileUtilitiesRegex.StartsWithUncPatternMatchLength(unixDirectory).ShouldBe(13);
        }

        [TestMethod]
        public void MatchLengthStartWithUncPatternExactDirectory_LegacyRegex()
        {
            string winDirectory = string.Format("{0}server\\path", _directoryStart);
            string unixDirectory = string.Format("{0}server/path", _altDirectoryStart);

            var match = StartsWithUncPattern.Match(winDirectory);
            match.Success.ShouldBeTrue();
            match.Length.ShouldBe(13);

            match = StartsWithUncPattern.Match(unixDirectory);
            match.Success.ShouldBeTrue();
            match.Length.ShouldBe(13);
        }

        [TestMethod]
        public void UncPatternMixedSlashes_LegacyRegex()
        {
            string winDirectory = string.Format("{0}abc/def", _directoryStart);
            string unixDirectory = string.Format("{0}abc\\def", _altDirectoryStart);

            UncPattern.IsMatch(winDirectory).ShouldBeTrue();
            UncPattern.IsMatch(unixDirectory).ShouldBeTrue();
        }

        [TestMethod]
        public void UncPatternMixedSlashes()
        {
            string winDirectory = string.Format("{0}abc/def", _directoryStart);
            string unixDirectory = string.Format("{0}abc\\def", _altDirectoryStart);

            FileUtilitiesRegex.IsUncPattern(winDirectory).ShouldBeTrue();
            FileUtilitiesRegex.IsUncPattern(unixDirectory).ShouldBeTrue();
        }

        [TestMethod]
        public void StartWithUncPatternMixedSlashes()
        {
            string winDirectory = string.Format("{0}abc/def", _directoryStart);
            string unixDirectory = string.Format("{0}abc\\def", _altDirectoryStart);

            FileUtilitiesRegex.StartsWithUncPattern(winDirectory).ShouldBeTrue();
            FileUtilitiesRegex.StartsWithUncPattern(unixDirectory).ShouldBeTrue();
        }

        [TestMethod]
        public void StartWithUncPatternMixedSlashes_LegacyRegex()
        {
            string winDirectory = string.Format("{0}abc/def", _directoryStart);
            string unixDirectory = string.Format("{0}abc\\def", _altDirectoryStart);

            StartsWithUncPattern.IsMatch(winDirectory).ShouldBeTrue();
            StartsWithUncPattern.IsMatch(unixDirectory).ShouldBeTrue();
        }

        [TestMethod]
        public void MatchLengthStartWithUncPatternMixedSlashes_LegacyRegex()
        {
            string winDirectory = string.Format("{0}abc/def", _directoryStart);
            string unixDirectory = string.Format("{0}abc\\def", _altDirectoryStart);

            var match = StartsWithUncPattern.Match(winDirectory);
            match.Success.ShouldBeTrue();
            match.Length.ShouldBe(9);

            match = StartsWithUncPattern.Match(unixDirectory);
            match.Success.ShouldBeTrue();
            match.Length.ShouldBe(9);
        }

        [TestMethod]
        public void MatchLengthStartWithUncPatternMixedSlashes()
        {
            string winDirectory = string.Format("{0}abc/def", _directoryStart);
            string unixDirectory = string.Format("{0}abc\\def", _altDirectoryStart);

            FileUtilitiesRegex.StartsWithUncPatternMatchLength(winDirectory).ShouldBe(9);
            FileUtilitiesRegex.StartsWithUncPatternMatchLength(unixDirectory).ShouldBe(9);
        }

        [TestMethod]
        public void UncPatternTrailingSlash_LegacyRegex()
        {
            string winDirectory = string.Format("{0}abc\\def\\", _directoryStart);
            string unixDirectory = string.Format("{0}abc/def/", _altDirectoryStart);

            UncPattern.IsMatch(winDirectory).ShouldBe(false);
            UncPattern.IsMatch(unixDirectory).ShouldBe(false);
        }

        [TestMethod]
        public void UncPatternTrailingSlash()
        {
            string winDirectory = string.Format("{0}abc\\def\\", _directoryStart);
            string unixDirectory = string.Format("{0}abc/def/", _altDirectoryStart);

            FileUtilitiesRegex.IsUncPattern(winDirectory).ShouldBe(false);
            FileUtilitiesRegex.IsUncPattern(unixDirectory).ShouldBe(false);
        }

        [TestMethod]
        public void StartWithUncPatternTrailingSlash_LegacyRegex()
        {
            string winDirectory = string.Format("{0}abc\\def\\", _directoryStart);
            string unixDirectory = string.Format("{0}abc/def/", _altDirectoryStart);

            StartsWithUncPattern.IsMatch(winDirectory).ShouldBeTrue();
            StartsWithUncPattern.IsMatch(unixDirectory).ShouldBeTrue();
        }

        [TestMethod]
        public void StartWithUncPatternTrailingSlash()
        {
            string winDirectory = string.Format("{0}abc\\def\\", _directoryStart);
            string unixDirectory = string.Format("{0}abc/def/", _altDirectoryStart);

            FileUtilitiesRegex.StartsWithUncPattern(winDirectory).ShouldBeTrue();
            FileUtilitiesRegex.StartsWithUncPattern(unixDirectory).ShouldBeTrue();
        }

        [TestMethod]
        public void MatchLengthStartWithUncPatternTrailingSlash_LegacyRegex()
        {
            string winDirectory = string.Format("{0}abc\\def\\", _directoryStart);
            string unixDirectory = string.Format("{0}abc/def/", _altDirectoryStart);

            var match = StartsWithUncPattern.Match(winDirectory);
            match.Success.ShouldBeTrue();
            match.Length.ShouldBe(9);

            match = StartsWithUncPattern.Match(unixDirectory);
            match.Success.ShouldBeTrue();
            match.Length.ShouldBe(9);
        }

        [TestMethod]
        public void MatchLengthStartWithUncPatternTrailingSlash()
        {
            string winDirectory = string.Format("{0}abc\\def\\", _directoryStart);
            string unixDirectory = string.Format("{0}abc/def/", _altDirectoryStart);

            FileUtilitiesRegex.StartsWithUncPatternMatchLength(winDirectory).ShouldBe(9);
            FileUtilitiesRegex.StartsWithUncPatternMatchLength(unixDirectory).ShouldBe(9);
        }

        [TestMethod]
        public void UncPatternLessThanMinimum_LegacyRegex()
        {
            string winDirectory = string.Format("{0}", _directoryStart);
            string unixDirectory = string.Format("{0}", _altDirectoryStart);

            UncPattern.IsMatch(winDirectory).ShouldBe(false);
            UncPattern.IsMatch(unixDirectory).ShouldBe(false);
        }

        [TestMethod]
        public void UncPatternLessThanMinimum()
        {
            string winDirectory = string.Format("{0}", _directoryStart);
            string unixDirectory = string.Format("{0}", _altDirectoryStart);

            FileUtilitiesRegex.IsUncPattern(winDirectory).ShouldBe(false);
            FileUtilitiesRegex.IsUncPattern(unixDirectory).ShouldBe(false);
        }

        [TestMethod]
        public void StartWithUncPatternLessThanMinimum_LegacyRegex()
        {
            string winDirectory = string.Format("{0}", _directoryStart);
            string unixDirectory = string.Format("{0}", _altDirectoryStart);

            StartsWithUncPattern.IsMatch(winDirectory).ShouldBe(false);
            StartsWithUncPattern.IsMatch(unixDirectory).ShouldBe(false);
        }

        [TestMethod]
        public void StartWithUncPatternLessThanMinimum()
        {
            string winDirectory = string.Format("{0}", _directoryStart);
            string unixDirectory = string.Format("{0}", _altDirectoryStart);

            FileUtilitiesRegex.StartsWithUncPattern(winDirectory).ShouldBe(false);
            FileUtilitiesRegex.StartsWithUncPattern(unixDirectory).ShouldBe(false);
        }

        [TestMethod]
        public void MatchLengthStartWithUncPatternLessThanMinimum_LegacyRegex()
        {
            string winDirectory = string.Format("{0}", _directoryStart);
            string unixDirectory = string.Format("{0}", _altDirectoryStart);

            var match = StartsWithUncPattern.Match(winDirectory);
            match.Success.ShouldBeFalse();

            match = StartsWithUncPattern.Match(unixDirectory);
            match.Success.ShouldBeFalse();
        }

        [TestMethod]
        public void MatchLengthStartWithUncPatternLessThanMinimum()
        {
            string winDirectory = string.Format("{0}", _directoryStart);
            string unixDirectory = string.Format("{0}", _altDirectoryStart);

            FileUtilitiesRegex.StartsWithUncPatternMatchLength(winDirectory).ShouldBe(-1);
            FileUtilitiesRegex.StartsWithUncPatternMatchLength(unixDirectory).ShouldBe(-1);
        }

        [TestMethod]
        public void UncPatternNoShare_LegacyRegex()
        {
            string winDirectory = string.Format("{0}server", _directoryStart);
            string unixDirectory = string.Format("{0}server", _altDirectoryStart);

            UncPattern.IsMatch(winDirectory).ShouldBe(false);
            UncPattern.IsMatch(unixDirectory).ShouldBe(false);
        }

        [TestMethod]
        public void IsUncPatternNoShare()
        {
            string winDirectory = string.Format("{0}server", _directoryStart);
            string unixDirectory = string.Format("{0}server", _altDirectoryStart);

            FileUtilitiesRegex.IsUncPattern(winDirectory).ShouldBe(false);
            FileUtilitiesRegex.IsUncPattern(unixDirectory).ShouldBe(false);
        }

        [TestMethod]
        public void StartWithUncPatternNoShare_LegacyRegex()
        {
            string winDirectory = string.Format("{0}server", _directoryStart);
            string unixDirectory = string.Format("{0}server", _altDirectoryStart);

            StartsWithUncPattern.IsMatch(winDirectory).ShouldBe(false);
            StartsWithUncPattern.IsMatch(unixDirectory).ShouldBe(false);
        }

        [TestMethod]
        public void StartWithUncPatternNoShare()
        {
            string winDirectory = string.Format("{0}server", _directoryStart);
            string unixDirectory = string.Format("{0}server", _altDirectoryStart);

            FileUtilitiesRegex.StartsWithUncPattern(winDirectory).ShouldBe(false);
            FileUtilitiesRegex.StartsWithUncPattern(unixDirectory).ShouldBe(false);
        }

        [TestMethod]
        public void MatchLengthStartWithUncPatternNoShare_LegacyRegex()
        {
            string winDirectory = string.Format("{0}server", _directoryStart);
            string unixDirectory = string.Format("{0}server", _altDirectoryStart);

            var match = StartsWithUncPattern.Match(winDirectory);
            match.Success.ShouldBeFalse();

            match = StartsWithUncPattern.Match(unixDirectory);
            match.Success.ShouldBeFalse();
        }

        [TestMethod]
        public void MatchLengthStartWithUncPatternNoShare()
        {
            string winDirectory = string.Format("{0}server", _directoryStart);
            string unixDirectory = string.Format("{0}server", _altDirectoryStart);

            FileUtilitiesRegex.StartsWithUncPatternMatchLength(winDirectory).ShouldBe(-1);
            FileUtilitiesRegex.StartsWithUncPatternMatchLength(unixDirectory).ShouldBe(-1);
        }

        [TestMethod]
        public void PatternEmptyString_LegacyRegex()
        {
            UncPattern.IsMatch(string.Empty).ShouldBeFalse();
            StartsWithUncPattern.IsMatch(string.Empty).ShouldBeFalse();
        }

        [TestMethod]
        public void PatternEmptyString()
        {
            FileUtilitiesRegex.IsUncPattern(string.Empty).ShouldBeFalse();
            FileUtilitiesRegex.StartsWithUncPattern(string.Empty).ShouldBeFalse();
            FileUtilitiesRegex.StartsWithUncPatternMatchLength(string.Empty).ShouldBe(-1);
        }
    }
}
