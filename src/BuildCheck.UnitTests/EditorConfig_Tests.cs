// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Build.Experimental.BuildCheck.Infrastructure.EditorConfig;
using static Microsoft.Build.Experimental.BuildCheck.Infrastructure.EditorConfig.EditorConfigGlobsMatcher;

#nullable disable

namespace Microsoft.Build.BuildCheck.UnitTests;

public class EditorConfig_Tests
{

    #region AssertEqualityComparer<T>
    private sealed class AssertEqualityComparer<T> : IEqualityComparer<T>
    {
        public static readonly IEqualityComparer<T> Instance = new AssertEqualityComparer<T>();

        private static bool CanBeNull()
        {
            var type = typeof(T);
            return !type.GetTypeInfo().IsValueType ||
                (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));
        }

        public static bool IsNull(T @object)
        {
            if (!CanBeNull())
            {
                return false;
            }

            return object.Equals(@object, default(T));
        }

        public static bool Equals(T left, T right)
        {
            return Instance.Equals(left, right);
        }

        bool IEqualityComparer<T>.Equals(T x, T y)
        {
            if (CanBeNull())
            {
                if (object.Equals(x, default(T)))
                {
                    return object.Equals(y, default(T));
                }

                if (object.Equals(y, default(T)))
                {
                    return false;
                }
            }

            if (x.GetType() != y.GetType())
            {
                return false;
            }

            if (x is IEquatable<T> equatable)
            {
                return equatable.Equals(y);
            }

            if (x is IComparable<T> comparableT)
            {
                return comparableT.CompareTo(y) == 0;
            }

            if (x is IComparable comparable)
            {
                return comparable.CompareTo(y) == 0;
            }

            var enumerableX = x as IEnumerable;
            var enumerableY = y as IEnumerable;

            if (enumerableX != null && enumerableY != null)
            {
                var enumeratorX = enumerableX.GetEnumerator();
                var enumeratorY = enumerableY.GetEnumerator();

                while (true)
                {
                    bool hasNextX = enumeratorX.MoveNext();
                    bool hasNextY = enumeratorY.MoveNext();

                    if (!hasNextX || !hasNextY)
                    {
                        return hasNextX == hasNextY;
                    }

                    if (!Equals(enumeratorX.Current, enumeratorY.Current))
                    {
                        return false;
                    }
                }
            }

            return object.Equals(x, y);
        }

        int IEqualityComparer<T>.GetHashCode(T obj)
        {
            throw new NotImplementedException();
        }
    }

    #endregion

    // Section Matchin Test cases: https://github.com/dotnet/roslyn/blob/ba163e712b01358a217065eec8a4a82f94a7efd5/src/Compilers/Core/CodeAnalysisTest/Analyzers/AnalyzerConfigTests.cs#L337
    #region Section Matching Tests
    [TestMethod]
    public void SimpleNameMatch()
    {
        SectionNameMatcher matcher = TryCreateSectionNameMatcher("abc").Value;
        Assert.Equal("^.*/abc$", matcher.Regex.ToString());

        Assert.True(matcher.IsMatch("/abc"));
        Assert.False(matcher.IsMatch("/aabc"));
        Assert.False(matcher.IsMatch("/ abc"));
        Assert.False(matcher.IsMatch("/cabc"));
    }

    [TestMethod]
    public void StarOnlyMatch()
    {
        SectionNameMatcher matcher = TryCreateSectionNameMatcher("*").Value;
        Assert.Equal("^.*/[^/]*$", matcher.Regex.ToString());

        Assert.True(matcher.IsMatch("/abc"));
        Assert.True(matcher.IsMatch("/123"));
        Assert.True(matcher.IsMatch("/abc/123"));
    }

    [TestMethod]
    public void StarNameMatch()
    {
        SectionNameMatcher matcher = TryCreateSectionNameMatcher("*.cs").Value;
        Assert.Equal("^.*/[^/]*\\.cs$", matcher.Regex.ToString());

        Assert.True(matcher.IsMatch("/abc.cs"));
        Assert.True(matcher.IsMatch("/123.cs"));
        Assert.True(matcher.IsMatch("/dir/subpath.cs"));
        // Only '/' is defined as a directory separator, so the caller
        // is responsible for converting any other machine directory
        // separators to '/' before matching
        Assert.True(matcher.IsMatch("/dir\\subpath.cs"));

        Assert.False(matcher.IsMatch("/abc.vb"));
    }

    [TestMethod]
    public void StarStarNameMatch()
    {
        SectionNameMatcher matcher = TryCreateSectionNameMatcher("**.cs").Value;
        Assert.Equal("^.*/.*\\.cs$", matcher.Regex.ToString());

        Assert.True(matcher.IsMatch("/abc.cs"));
        Assert.True(matcher.IsMatch("/dir/subpath.cs"));
    }

    [TestMethod]
    public void EscapeDot()
    {
        SectionNameMatcher matcher = TryCreateSectionNameMatcher("...").Value;
        Assert.Equal("^.*/\\.\\.\\.$", matcher.Regex.ToString());

        Assert.True(matcher.IsMatch("/..."));
        Assert.True(matcher.IsMatch("/subdir/..."));
        Assert.False(matcher.IsMatch("/aaa"));
        Assert.False(matcher.IsMatch("/???"));
        Assert.False(matcher.IsMatch("/abc"));
    }

    [TestMethod]
    public void EndBackslashMatch()
    {
        SectionNameMatcher? matcher = TryCreateSectionNameMatcher("abc\\");
        Assert.Null(matcher);
    }

    [TestMethod]
    public void QuestionMatch()
    {
        SectionNameMatcher matcher = TryCreateSectionNameMatcher("ab?def").Value;
        Assert.Equal("^.*/ab.def$", matcher.Regex.ToString());

        Assert.True(matcher.IsMatch("/abcdef"));
        Assert.True(matcher.IsMatch("/ab?def"));
        Assert.True(matcher.IsMatch("/abzdef"));
        Assert.True(matcher.IsMatch("/ab/def"));
        Assert.True(matcher.IsMatch("/ab\\def"));
    }

    [TestMethod]
    public void LiteralBackslash()
    {
        SectionNameMatcher matcher = TryCreateSectionNameMatcher("ab\\\\c").Value;
        Assert.Equal("^.*/ab\\\\c$", matcher.Regex.ToString());

        Assert.True(matcher.IsMatch("/ab\\c"));
        Assert.False(matcher.IsMatch("/ab/c"));
        Assert.False(matcher.IsMatch("/ab\\\\c"));
    }

    [TestMethod]
    public void LiteralStars()
    {
        SectionNameMatcher matcher = TryCreateSectionNameMatcher("\\***\\*\\**").Value;
        Assert.Equal("^.*/\\*.*\\*\\*[^/]*$", matcher.Regex.ToString());

        Assert.True(matcher.IsMatch("/*ab/cd**efg*"));
        Assert.False(matcher.IsMatch("/ab/cd**efg*"));
        Assert.False(matcher.IsMatch("/*ab/cd*efg*"));
        Assert.False(matcher.IsMatch("/*ab/cd**ef/gh"));
    }

    [TestMethod]
    public void LiteralQuestions()
    {
        SectionNameMatcher matcher = TryCreateSectionNameMatcher("\\??\\?*\\??").Value;
        Assert.Equal("^.*/\\?.\\?[^/]*\\?.$", matcher.Regex.ToString());

        Assert.True(matcher.IsMatch("/?a?cde?f"));
        Assert.True(matcher.IsMatch("/???????f"));
        Assert.False(matcher.IsMatch("/aaaaaaaa"));
        Assert.False(matcher.IsMatch("/aa?cde?f"));
        Assert.False(matcher.IsMatch("/?a?cdexf"));
        Assert.False(matcher.IsMatch("/?axcde?f"));
    }

    [TestMethod]
    public void LiteralBraces()
    {
        SectionNameMatcher matcher = TryCreateSectionNameMatcher("abc\\{\\}def").Value;
        Assert.Equal(@"^.*/abc\{}def$", matcher.Regex.ToString());

        Assert.True(matcher.IsMatch("/abc{}def"));
        Assert.True(matcher.IsMatch("/subdir/abc{}def"));
        Assert.False(matcher.IsMatch("/abcdef"));
        Assert.False(matcher.IsMatch("/abc}{def"));
    }

    [TestMethod]
    public void LiteralComma()
    {
        SectionNameMatcher matcher = TryCreateSectionNameMatcher("abc\\,def").Value;
        Assert.Equal("^.*/abc,def$", matcher.Regex.ToString());

        Assert.True(matcher.IsMatch("/abc,def"));
        Assert.True(matcher.IsMatch("/subdir/abc,def"));
        Assert.False(matcher.IsMatch("/abcdef"));
        Assert.False(matcher.IsMatch("/abc\\,def"));
        Assert.False(matcher.IsMatch("/abc`def"));
    }

    [TestMethod]
    public void SimpleChoice()
    {
        SectionNameMatcher matcher = TryCreateSectionNameMatcher("*.{cs,vb,fs}").Value;
        Assert.Equal("^.*/[^/]*\\.(?:cs|vb|fs)$", matcher.Regex.ToString());

        Assert.True(matcher.IsMatch("/abc.cs"));
        Assert.True(matcher.IsMatch("/abc.vb"));
        Assert.True(matcher.IsMatch("/abc.fs"));
        Assert.True(matcher.IsMatch("/subdir/abc.cs"));
        Assert.True(matcher.IsMatch("/subdir/abc.vb"));
        Assert.True(matcher.IsMatch("/subdir/abc.fs"));

        Assert.False(matcher.IsMatch("/abcxcs"));
        Assert.False(matcher.IsMatch("/abcxvb"));
        Assert.False(matcher.IsMatch("/abcxfs"));
        Assert.False(matcher.IsMatch("/subdir/abcxcs"));
        Assert.False(matcher.IsMatch("/subdir/abcxcb"));
        Assert.False(matcher.IsMatch("/subdir/abcxcs"));
    }

    [TestMethod]
    public void OneChoiceHasSlashes()
    {
        SectionNameMatcher matcher = TryCreateSectionNameMatcher("{*.cs,subdir/test.vb}").Value;
        // This is an interesting case that may be counterintuitive.  A reasonable understanding
        // of the section matching could interpret the choice as generating multiple identical
        // sections, so [{a, b, c}] would be equivalent to [a] ... [b] ... [c] with all of the
        // same properties in each section. This is somewhat true, but the rules of how the matching
        // prefixes are constructed violate this assumption because they are defined as whether or
        // not a section contains a slash, not whether any of the choices contain a slash. So while
        // [*.cs] usually translates into '**/*.cs' because it contains no slashes, the slashes in
        // the second choice make this into '/*.cs', effectively matching only files in the root
        // directory of the match, instead of all subdirectories.
        Assert.Equal("^/(?:[^/]*\\.cs|subdir/test\\.vb)$", matcher.Regex.ToString());

        Assert.True(matcher.IsMatch("/test.cs"));
        Assert.True(matcher.IsMatch("/subdir/test.vb"));

        Assert.False(matcher.IsMatch("/subdir/test.cs"));
        Assert.False(matcher.IsMatch("/subdir/subdir/test.vb"));
        Assert.False(matcher.IsMatch("/test.vb"));
    }

    [TestMethod]
    public void EmptyChoice()
    {
        SectionNameMatcher matcher = TryCreateSectionNameMatcher("{}").Value;
        Assert.Equal("^.*/(?:)$", matcher.Regex.ToString());

        Assert.True(matcher.IsMatch("/"));
        Assert.True(matcher.IsMatch("/subdir/"));
        Assert.False(matcher.IsMatch("/."));
        Assert.False(matcher.IsMatch("/anything"));
    }

    [TestMethod]
    public void SingleChoice()
    {
        SectionNameMatcher matcher = TryCreateSectionNameMatcher("{*.cs}").Value;
        Assert.Equal("^.*/(?:[^/]*\\.cs)$", matcher.Regex.ToString());

        Assert.True(matcher.IsMatch("/test.cs"));
        Assert.True(matcher.IsMatch("/subdir/test.cs"));
        Assert.False(matcher.IsMatch("test.vb"));
        Assert.False(matcher.IsMatch("testxcs"));
    }

    [TestMethod]
    public void UnmatchedBraces()
    {
        SectionNameMatcher? matcher = TryCreateSectionNameMatcher("{{{{}}");
        Assert.Null(matcher);
    }

    [TestMethod]
    public void CommaOutsideBraces()
    {
        SectionNameMatcher? matcher = TryCreateSectionNameMatcher("abc,def");
        Assert.Null(matcher);
    }

    [TestMethod]
    public void RecursiveChoice()
    {
        SectionNameMatcher matcher = TryCreateSectionNameMatcher("{test{.cs,.vb},other.{a{bb,cc}}}").Value;
        Assert.Equal("^.*/(?:test(?:\\.cs|\\.vb)|other\\.(?:a(?:bb|cc)))$", matcher.Regex.ToString());

        Assert.True(matcher.IsMatch("/test.cs"));
        Assert.True(matcher.IsMatch("/test.vb"));
        Assert.True(matcher.IsMatch("/subdir/test.cs"));
        Assert.True(matcher.IsMatch("/subdir/test.vb"));
        Assert.True(matcher.IsMatch("/other.abb"));
        Assert.True(matcher.IsMatch("/other.acc"));

        Assert.False(matcher.IsMatch("/test.fs"));
        Assert.False(matcher.IsMatch("/other.bbb"));
        Assert.False(matcher.IsMatch("/other.ccc"));
        Assert.False(matcher.IsMatch("/subdir/other.bbb"));
        Assert.False(matcher.IsMatch("/subdir/other.ccc"));
    }

    [TestMethod]
    public void DashChoice()
    {
        SectionNameMatcher matcher = TryCreateSectionNameMatcher("ab{-}cd{-,}ef").Value;
        Assert.Equal("^.*/ab(?:-)cd(?:-|)ef$", matcher.Regex.ToString());

        Assert.True(matcher.IsMatch("/ab-cd-ef"));
        Assert.True(matcher.IsMatch("/ab-cdef"));

        Assert.False(matcher.IsMatch("/abcdef"));
        Assert.False(matcher.IsMatch("/ab--cd-ef"));
        Assert.False(matcher.IsMatch("/ab--cd--ef"));
    }

    [TestMethod]
    public void MiddleMatch()
    {
        SectionNameMatcher matcher = TryCreateSectionNameMatcher("ab{cs,vb,fs}cd").Value;
        Assert.Equal("^.*/ab(?:cs|vb|fs)cd$", matcher.Regex.ToString());

        Assert.True(matcher.IsMatch("/abcscd"));
        Assert.True(matcher.IsMatch("/abvbcd"));
        Assert.True(matcher.IsMatch("/abfscd"));

        Assert.False(matcher.IsMatch("/abcs"));
        Assert.False(matcher.IsMatch("/abcd"));
        Assert.False(matcher.IsMatch("/vbcd"));
    }

    private static IEnumerable<(string, string)> RangeAndInverse(string s1, string s2)
    {
        yield return (s1, s2);
        yield return (s2, s1);
    }

    [TestMethod]
    public void NumberMatch()
    {
        foreach (var (i1, i2) in RangeAndInverse("0", "10"))
        {
            var matcher = TryCreateSectionNameMatcher($"{{{i1}..{i2}}}").Value;

            Assert.True(matcher.IsMatch("/0"));
            Assert.True(matcher.IsMatch("/10"));
            Assert.True(matcher.IsMatch("/5"));
            Assert.True(matcher.IsMatch("/000005"));
            Assert.False(matcher.IsMatch("/-1"));
            Assert.False(matcher.IsMatch("/-00000001"));
            Assert.False(matcher.IsMatch("/11"));
        }
    }

    [TestMethod]
    public void NumberMatchNegativeRange()
    {
        foreach (var (i1, i2) in RangeAndInverse("-10", "0"))
        {
            var matcher = TryCreateSectionNameMatcher($"{{{i1}..{i2}}}").Value;

            Assert.True(matcher.IsMatch("/0"));
            Assert.True(matcher.IsMatch("/-10"));
            Assert.True(matcher.IsMatch("/-5"));
            Assert.False(matcher.IsMatch("/1"));
            Assert.False(matcher.IsMatch("/-11"));
            Assert.False(matcher.IsMatch("/--0"));
        }
    }

    [TestMethod]
    public void NumberMatchNegToPos()
    {
        foreach (var (i1, i2) in RangeAndInverse("-10", "10"))
        {
            var matcher = TryCreateSectionNameMatcher($"{{{i1}..{i2}}}").Value;

            Assert.True(matcher.IsMatch("/0"));
            Assert.True(matcher.IsMatch("/-5"));
            Assert.True(matcher.IsMatch("/5"));
            Assert.True(matcher.IsMatch("/-10"));
            Assert.True(matcher.IsMatch("/10"));
            Assert.False(matcher.IsMatch("/-11"));
            Assert.False(matcher.IsMatch("/11"));
            Assert.False(matcher.IsMatch("/--0"));
        }
    }

    [TestMethod]
    public void MultipleNumberRanges()
    {
        foreach (var matchString in new[] { "a{-10..0}b{0..10}", "a{0..-10}b{10..0}" })
        {
            var matcher = TryCreateSectionNameMatcher(matchString).Value;

            Assert.True(matcher.IsMatch("/a0b0"));
            Assert.True(matcher.IsMatch("/a-5b0"));
            Assert.True(matcher.IsMatch("/a-5b5"));
            Assert.True(matcher.IsMatch("/a-5b10"));
            Assert.True(matcher.IsMatch("/a-10b10"));
            Assert.True(matcher.IsMatch("/a-10b0"));
            Assert.True(matcher.IsMatch("/a-0b0"));
            Assert.True(matcher.IsMatch("/a-0b-0"));

            Assert.False(matcher.IsMatch("/a-11b10"));
            Assert.False(matcher.IsMatch("/a-11b10"));
            Assert.False(matcher.IsMatch("/a-10b11"));
        }
    }

    [TestMethod]
    public void BadNumberRanges()
    {
        var matcherOpt = TryCreateSectionNameMatcher("{0..");

        Assert.Null(matcherOpt);

        var matcher = TryCreateSectionNameMatcher("{0..}").Value;

        Assert.True(matcher.IsMatch("/0.."));
        Assert.False(matcher.IsMatch("/0"));
        Assert.False(matcher.IsMatch("/0."));
        Assert.False(matcher.IsMatch("/0abc"));

        matcher = TryCreateSectionNameMatcher("{0..A}").Value;
        Assert.True(matcher.IsMatch("/0..A"));
        Assert.False(matcher.IsMatch("/0"));
        Assert.False(matcher.IsMatch("/0abc"));

        // The reference implementation uses atoi here so we can presume
        // numbers out of range of Int32 are not well supported
        matcherOpt = TryCreateSectionNameMatcher($"{{0..{UInt32.MaxValue}}}");

        Assert.Null(matcherOpt);
    }

    [TestMethod]
    public void CharacterClassSimple()
    {
        var matcher = TryCreateSectionNameMatcher("*.[cf]s").Value;
        Assert.Equal(@"^.*/[^/]*\.[cf]s$", matcher.Regex.ToString());

        Assert.True(matcher.IsMatch("/abc.cs"));
        Assert.True(matcher.IsMatch("/abc.fs"));
        Assert.False(matcher.IsMatch("/abc.vs"));
    }

    [TestMethod]
    public void CharacterClassNegative()
    {
        var matcher = TryCreateSectionNameMatcher("*.[!cf]s").Value;
        Assert.Equal(@"^.*/[^/]*\.[^cf]s$", matcher.Regex.ToString());

        Assert.False(matcher.IsMatch("/abc.cs"));
        Assert.False(matcher.IsMatch("/abc.fs"));
        Assert.True(matcher.IsMatch("/abc.vs"));
        Assert.True(matcher.IsMatch("/abc.xs"));
        Assert.False(matcher.IsMatch("/abc.vxs"));
    }

    [TestMethod]
    public void CharacterClassCaret()
    {
        var matcher = TryCreateSectionNameMatcher("*.[^cf]s").Value;
        Assert.Equal(@"^.*/[^/]*\.[\^cf]s$", matcher.Regex.ToString());

        Assert.True(matcher.IsMatch("/abc.cs"));
        Assert.True(matcher.IsMatch("/abc.fs"));
        Assert.True(matcher.IsMatch("/abc.^s"));
        Assert.False(matcher.IsMatch("/abc.vs"));
        Assert.False(matcher.IsMatch("/abc.xs"));
        Assert.False(matcher.IsMatch("/abc.vxs"));
    }

    [TestMethod]
    public void CharacterClassRange()
    {
        var matcher = TryCreateSectionNameMatcher("[0-9]x").Value;
        Assert.Equal("^.*/[0-9]x$", matcher.Regex.ToString());

        Assert.True(matcher.IsMatch("/0x"));
        Assert.True(matcher.IsMatch("/1x"));
        Assert.True(matcher.IsMatch("/9x"));
        Assert.False(matcher.IsMatch("/yx"));
        Assert.False(matcher.IsMatch("/00x"));
    }

    [TestMethod]
    public void CharacterClassNegativeRange()
    {
        var matcher = TryCreateSectionNameMatcher("[!0-9]x").Value;
        Assert.Equal("^.*/[^0-9]x$", matcher.Regex.ToString());

        Assert.False(matcher.IsMatch("/0x"));
        Assert.False(matcher.IsMatch("/1x"));
        Assert.False(matcher.IsMatch("/9x"));
        Assert.True(matcher.IsMatch("/yx"));
        Assert.False(matcher.IsMatch("/00x"));
    }

    [TestMethod]
    public void CharacterClassRangeAndChoice()
    {
        var matcher = TryCreateSectionNameMatcher("[ab0-9]x").Value;
        Assert.Equal("^.*/[ab0-9]x$", matcher.Regex.ToString());

        Assert.True(matcher.IsMatch("/ax"));
        Assert.True(matcher.IsMatch("/bx"));
        Assert.True(matcher.IsMatch("/0x"));
        Assert.True(matcher.IsMatch("/1x"));
        Assert.True(matcher.IsMatch("/9x"));
        Assert.False(matcher.IsMatch("/yx"));
        Assert.False(matcher.IsMatch("/0ax"));
    }

    [TestMethod]
    public void CharacterClassOpenEnded()
    {
        var matcher = TryCreateSectionNameMatcher("[");
        Assert.Null(matcher);
    }

    [TestMethod]
    public void CharacterClassEscapedOpenEnded()
    {
        var matcher = TryCreateSectionNameMatcher(@"[\]");
        Assert.Null(matcher);
    }

    [TestMethod]
    public void CharacterClassEscapeAtEnd()
    {
        var matcher = TryCreateSectionNameMatcher(@"[\");
        Assert.Null(matcher);
    }

    [TestMethod]
    public void CharacterClassOpenBracketInside()
    {
        var matcher = TryCreateSectionNameMatcher(@"[[a]bc").Value;

        Assert.True(matcher.IsMatch("/abc"));
        Assert.True(matcher.IsMatch("/[bc"));
        Assert.False(matcher.IsMatch("/ab"));
        Assert.False(matcher.IsMatch("/[b"));
        Assert.False(matcher.IsMatch("/bc"));
        Assert.False(matcher.IsMatch("/ac"));
        Assert.False(matcher.IsMatch("/[c"));

        Assert.Equal(@"^.*/[\[a]bc$", matcher.Regex.ToString());
    }

    [TestMethod]
    public void CharacterClassStartingDash()
    {
        var matcher = TryCreateSectionNameMatcher(@"[-ac]bd").Value;

        Assert.True(matcher.IsMatch("/abd"));
        Assert.True(matcher.IsMatch("/cbd"));
        Assert.True(matcher.IsMatch("/-bd"));
        Assert.False(matcher.IsMatch("/bbd"));
        Assert.False(matcher.IsMatch("/-cd"));
        Assert.False(matcher.IsMatch("/bcd"));

        Assert.Equal(@"^.*/[-ac]bd$", matcher.Regex.ToString());
    }

    [TestMethod]
    public void CharacterClassEndingDash()
    {
        var matcher = TryCreateSectionNameMatcher(@"[ac-]bd").Value;

        Assert.True(matcher.IsMatch("/abd"));
        Assert.True(matcher.IsMatch("/cbd"));
        Assert.True(matcher.IsMatch("/-bd"));
        Assert.False(matcher.IsMatch("/bbd"));
        Assert.False(matcher.IsMatch("/-cd"));
        Assert.False(matcher.IsMatch("/bcd"));

        Assert.Equal(@"^.*/[ac-]bd$", matcher.Regex.ToString());
    }

    [TestMethod]
    public void CharacterClassEndBracketAfter()
    {
        var matcher = TryCreateSectionNameMatcher(@"[ab]]cd").Value;

        Assert.True(matcher.IsMatch("/a]cd"));
        Assert.True(matcher.IsMatch("/b]cd"));
        Assert.False(matcher.IsMatch("/acd"));
        Assert.False(matcher.IsMatch("/bcd"));
        Assert.False(matcher.IsMatch("/acd"));

        Assert.Equal(@"^.*/[ab]]cd$", matcher.Regex.ToString());
    }

    [TestMethod]
    public void CharacterClassEscapeBackslash()
    {
        var matcher = TryCreateSectionNameMatcher(@"[ab\\]cd").Value;

        Assert.True(matcher.IsMatch("/acd"));
        Assert.True(matcher.IsMatch("/bcd"));
        Assert.True(matcher.IsMatch("/\\cd"));
        Assert.False(matcher.IsMatch("/dcd"));
        Assert.False(matcher.IsMatch("/\\\\cd"));
        Assert.False(matcher.IsMatch("/cd"));

        Assert.Equal(@"^.*/[ab\\]cd$", matcher.Regex.ToString());
    }

    [TestMethod]
    public void EscapeOpenBracket()
    {
        var matcher = TryCreateSectionNameMatcher(@"ab\[cd").Value;

        Assert.True(matcher.IsMatch("/ab[cd"));
        Assert.False(matcher.IsMatch("/ab[[cd"));
        Assert.False(matcher.IsMatch("/abc"));
        Assert.False(matcher.IsMatch("/abd"));

        Assert.Equal(@"^.*/ab\[cd$", matcher.Regex.ToString());
    }
    #endregion

    #region Parsing Tests

    private static void SetEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, IEqualityComparer<T> comparer = null, string message = null)
    {
        var expectedSet = new HashSet<T>(expected, comparer);
        var result = expected.Count() == actual.Count() && expectedSet.SetEquals(actual);
        Assert.True(result, message);
    }

    private static void Equal<T>(
        IEnumerable<T> expected,
        IEnumerable<T> actual,
        IEqualityComparer<T> comparer = null,
        string message = null)
    {
        if (expected == null)
        {
            Assert.Null(actual);
        }
        else
        {
            Assert.NotNull(actual);
        }

        if (SequenceEqual(expected, actual, comparer))
        {
            return;
        }

        Assert.Fail(message);
    }

    private static bool SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, IEqualityComparer<T> comparer = null)
    {
        if (ReferenceEquals(expected, actual))
        {
            return true;
        }

        var enumerator1 = expected.GetEnumerator();
        var enumerator2 = actual.GetEnumerator();

        while (true)
        {
            var hasNext1 = enumerator1.MoveNext();
            var hasNext2 = enumerator2.MoveNext();

            if (hasNext1 != hasNext2)
            {
                return false;
            }

            if (!hasNext1)
            {
                break;
            }

            var value1 = enumerator1.Current;
            var value2 = enumerator2.Current;

            if (!(comparer != null ? comparer.Equals(value1, value2) : AssertEqualityComparer<T>.Equals(value1, value2)))
            {
                return false;
            }
        }

        return true;
    }

    public static KeyValuePair<K, V> Create<K, V>(K key, V value)
    {
        return new KeyValuePair<K, V>(key, value);
    }

    [TestMethod]
    public void SimpleCase()
    {
        var config = EditorConfigFile.Parse("""
root = true

# Comment1
# Comment2
##################################

my_global_prop = my_global_val

[*.cs]
my_prop = my_val
""");
        Assert.Equal("", config.GlobalSection.Name);
        var properties = config.GlobalSection.Properties;

        SetEqual(
            new[] { Create("my_global_prop", "my_global_val") ,
                    Create("root", "true") },
            properties);

        var namedSections = config.NamedSections;
        Assert.Equal("*.cs", namedSections[0].Name);
        SetEqual(
            new[] { Create("my_prop", "my_val") },
            namedSections[0].Properties);

        Assert.True(config.IsRoot);
    }


    [TestMethod]
    // [WorkItem(52469, "https://github.com/dotnet/roslyn/issues/52469")]
    public void ConfigWithEscapedValues()
    {
        var config = EditorConfigFile.Parse(@"is_global = true

[c:/\{f\*i\?le1\}.cs]
build_metadata.Compile.ToRetrieve = abc123

[c:/f\,ile\#2.cs]
build_metadata.Compile.ToRetrieve = def456

[c:/f\;i\!le\[3\].cs]
build_metadata.Compile.ToRetrieve = ghi789
");

        var namedSections = config.NamedSections;
        Assert.Equal("c:/\\{f\\*i\\?le1\\}.cs", namedSections[0].Name);
        Equal(
            new[] { Create("build_metadata.compile.toretrieve", "abc123") },
            namedSections[0].Properties);

        Assert.Equal("c:/f\\,ile\\#2.cs", namedSections[1].Name);
        Equal(
            new[] { Create("build_metadata.compile.toretrieve", "def456") },
            namedSections[1].Properties);

        Assert.Equal("c:/f\\;i\\!le\\[3\\].cs", namedSections[2].Name);
        Equal(
            new[] { Create("build_metadata.compile.toretrieve", "ghi789") },
            namedSections[2].Properties);
    }

    /*
    [TestMethod]
    [WorkItem(52469, "https://github.com/dotnet/roslyn/issues/52469")]
    public void CanGetSectionsWithSpecialCharacters()
    {
        var config = ParseConfigFile(@"is_global = true

[/home/foo/src/\{releaseid\}.cs]
build_metadata.Compile.ToRetrieve = abc123

[/home/foo/src/Pages/\#foo/HomePage.cs]
build_metadata.Compile.ToRetrieve = def456
");

        var set = CheckConfigSet.Create(ImmutableArray.Create(config));

        var sectionOptions = set.GetOptionsForSourcePath("/home/foo/src/{releaseid}.cs");
        Assert.Equal("abc123", sectionOptions.CheckOptions["build_metadata.compile.toretrieve"]);

        sectionOptions = set.GetOptionsForSourcePath("/home/foo/src/Pages/#foo/HomePage.cs");
        Assert.Equal("def456", sectionOptions.CheckOptions["build_metadata.compile.toretrieve"]);
    }*/

    [TestMethod]
    public void MissingClosingBracket()
    {
        var config = EditorConfigFile.Parse(@"
[*.cs
my_prop = my_val");
        var properties = config.GlobalSection.Properties;
        SetEqual(
            new[] { Create("my_prop", "my_val") },
            properties);

        Assert.Equal(0, config.NamedSections.Length);
    }


    [TestMethod]
    public void EmptySection()
    {
        var config = EditorConfigFile.Parse(@"
[]
my_prop = my_val");

        var properties = config.GlobalSection.Properties;
        Assert.Equal(new[] { Create("my_prop", "my_val") }, properties);
        Assert.Equal(0, config.NamedSections.Length);
    }


    [TestMethod]
    public void CaseInsensitivePropKey()
    {
        var config = EditorConfigFile.Parse(@"
my_PROP = my_VAL");
        var properties = config.GlobalSection.Properties;

        Assert.True(properties.TryGetValue("my_PrOp", out var val));
        Assert.Equal("my_VAL", val);
        Assert.Equal("my_prop", properties.Keys.Single());
    }

    // there is no reversed keys support for msbuild
    /*[TestMethod]
    public void NonReservedKeyPreservedCaseVal()
    {
        var config = ParseConfigFile(string.Join(Environment.NewLine,
            CheckConfig.ReservedKeys.Select(k => "MY_" + k + " = MY_VAL")));
        AssertEx.SetEqual(
            CheckConfig.ReservedKeys.Select(k => KeyValuePair.Create("my_" + k, "MY_VAL")).ToList(),
            config.GlobalSection.Properties);
    }*/


    [TestMethod]
    public void DuplicateKeys()
    {
        var config = EditorConfigFile.Parse(@"
my_prop = my_val
my_prop = my_other_val");

        var properties = config.GlobalSection.Properties;
        Assert.Equal(new[] { Create("my_prop", "my_other_val") }, properties);
    }


    [TestMethod]
    public void DuplicateKeysCasing()
    {
        var config = EditorConfigFile.Parse(@"
my_prop = my_val
my_PROP = my_other_val");

        var properties = config.GlobalSection.Properties;
        Assert.Equal(new[] { Create("my_prop", "my_other_val") }, properties);
    }


    [TestMethod]
    public void MissingKey()
    {
        var config = EditorConfigFile.Parse(@"
= my_val1
my_prop = my_val2");

        var properties = config.GlobalSection.Properties;
        SetEqual(
            new[] { Create("my_prop", "my_val2") },
            properties);
    }



    [TestMethod]
    public void MissingVal()
    {
        var config = EditorConfigFile.Parse(@"
my_prop1 =
my_prop2 = my_val");

        var properties = config.GlobalSection.Properties;
        SetEqual(
            new[] { Create("my_prop1", ""),
                    Create("my_prop2", "my_val") },
            properties);
    }


    [TestMethod]
    public void SpacesInProperties()
    {
        var config = EditorConfigFile.Parse(@"
my prop1 = my_val1
my_prop2 = my val2");

        var properties = config.GlobalSection.Properties;
        SetEqual(
            new[] { Create("my_prop2", "my val2") },
            properties);
    }


    [TestMethod]
    public void EndOfLineComments()
    {
        var config = EditorConfigFile.Parse(@"
my_prop2 = my val2 # Comment");

        var properties = config.GlobalSection.Properties;
        SetEqual(
            new[] { Create("my_prop2", "my val2") },
            properties);
    }

    [TestMethod]
    public void SymbolsStartKeys()
    {
        var config = EditorConfigFile.Parse(@"
@!$abc = my_val1
@!$\# = my_val2");

        var properties = config.GlobalSection.Properties;
        Assert.Equal(0, properties.Count);
    }


    [TestMethod]
    public void EqualsAndColon()
    {
        var config = EditorConfigFile.Parse(@"
my:key1 = my_val
my_key2 = my:val");

        var properties = config.GlobalSection.Properties;
        SetEqual(
            new[] { Create("my", "key1 = my_val"),
                    Create("my_key2", "my:val")},
            properties);
    }

    [TestMethod]
    public void SymbolsInProperties()
    {
        var config = EditorConfigFile.Parse(@"
my@key1 = my_val
my_key2 = my@val");

        var properties = config.GlobalSection.Properties;
        SetEqual(
            new[] { Create("my_key2", "my@val") },
            properties);
    }

    [TestMethod]
    public void LongLines()
    {
        // This example is described in the Python ConfigParser as allowing
        // line continuation via the RFC 822 specification, section 3.1.1
        // LONG HEADER FIELDS. The VS parser does not accept this as a
        // valid parse for an editorconfig file. We follow similarly.
        var config = EditorConfigFile.Parse(@"
long: this value continues
   in the next line");

        var properties = config.GlobalSection.Properties;
        SetEqual(
            new[] { Create("long", "this value continues") },
            properties);
    }


    [TestMethod]
    public void CaseInsensitiveRoot()
    {
        var config = EditorConfigFile.Parse(@"
RoOt = TruE");
        Assert.True(config.IsRoot);
    }


    /*
    Reserved values are not supported at the moment
    [TestMethod]
    public void ReservedValues()
    {
        int index = 0;
        var config = ParseConfigFile(string.Join(Environment.NewLine,
            CheckConfig.ReservedValues.Select(v => "MY_KEY" + (index++) + " = " + v.ToUpperInvariant())));
        index = 0;
        AssertEx.SetEqual(
            CheckConfig.ReservedValues.Select(v => KeyValuePair.Create("my_key" + (index++), v)).ToList(),
            config.GlobalSection.Properties);
    }
    */

    /*
    [TestMethod]
    public void ReservedKeys()
    {
        var config = ParseConfigFile(string.Join(Environment.NewLine,
            CheckConfig.ReservedKeys.Select(k => k + " = MY_VAL")));
        AssertEx.SetEqual(
            CheckConfig.ReservedKeys.Select(k => KeyValuePair.Create(k, "my_val")).ToList(),
            config.GlobalSection.Properties);
    }
    */
    #endregion
}
