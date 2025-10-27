// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Shared;
using Xunit;

#nullable disable

namespace Microsoft.Build.UnitTests
{
    public sealed class EscapingUtilities_Tests
    {
        /// <summary>
        /// </summary>
        [Fact]
        public void Unescape()
        {
            Assert.AreEqual("", EscapingUtilities.UnescapeAll(""));
            Assert.AreEqual("foo", EscapingUtilities.UnescapeAll("foo"));
            Assert.AreEqual("foo space", EscapingUtilities.UnescapeAll("foo%20space"));
            Assert.AreEqual("foo2;", EscapingUtilities.UnescapeAll("foo2%3B"));
            Assert.AreEqual(";foo3", EscapingUtilities.UnescapeAll("%3bfoo3"));
            Assert.AreEqual(";", EscapingUtilities.UnescapeAll("%3b"));
            Assert.AreEqual(";;;;;", EscapingUtilities.UnescapeAll("%3b%3B;%3b%3B"));
            Assert.AreEqual("%3B", EscapingUtilities.UnescapeAll("%253B"));
            Assert.AreEqual("===%ZZ %%%===", EscapingUtilities.UnescapeAll("===%ZZ%20%%%==="));
            Assert.AreEqual("hello; escaping% how( are) you?", EscapingUtilities.UnescapeAll("hello%3B escaping%25 how%28 are%29 you%3f"));

            Assert.AreEqual("%*?*%*", EscapingUtilities.UnescapeAll("%25*?*%25*"));
            Assert.AreEqual("%*?*%*", EscapingUtilities.UnescapeAll("%25%2a%3f%2a%25%2a"));

            Assert.AreEqual("*Star*craft or *War*cr@ft??", EscapingUtilities.UnescapeAll("%2aStar%2Acraft%20or %2aWar%2Acr%40ft%3f%3F"));
        }

        /// <summary>
        /// </summary>
        [Fact]
        public void Escape()
        {
            Assert.AreEqual("%2a", EscapingUtilities.Escape("*"));
            Assert.AreEqual("%3f", EscapingUtilities.Escape("?"));
            Assert.AreEqual("#%2a%3f%2a#%2a", EscapingUtilities.Escape("#*?*#*"));
            Assert.AreEqual("%25%2a%3f%2a%25%2a", EscapingUtilities.Escape("%*?*%*"));
        }

        /// <summary>
        /// </summary>
        [Fact]
        public void UnescapeEscape()
        {
            string text = "*";
            Assert.AreEqual(text, EscapingUtilities.UnescapeAll(EscapingUtilities.Escape(text)));

            text = "?";
            Assert.AreEqual(text, EscapingUtilities.UnescapeAll(EscapingUtilities.Escape(text)));

            text = "#*?*#*";
            Assert.AreEqual(text, EscapingUtilities.UnescapeAll(EscapingUtilities.Escape(text)));
        }

        /// <summary>
        /// </summary>
        [Fact]
        public void EscapeUnescape()
        {
            string text = "%2a";
            Assert.AreEqual(text, EscapingUtilities.Escape(EscapingUtilities.UnescapeAll(text)));

            text = "%3f";
            Assert.AreEqual(text, EscapingUtilities.Escape(EscapingUtilities.UnescapeAll(text)));

            text = "#%2a%3f%2a#%2a";
            Assert.AreEqual(text, EscapingUtilities.Escape(EscapingUtilities.UnescapeAll(text)));
        }

        [Fact]
        public void ContainsEscapedWildcards()
        {
            Assert.IsFalse(EscapingUtilities.ContainsEscapedWildcards("NoStarOrQMark"));
            Assert.IsFalse(EscapingUtilities.ContainsEscapedWildcards("%"));
            Assert.IsFalse(EscapingUtilities.ContainsEscapedWildcards("%%"));
            Assert.IsFalse(EscapingUtilities.ContainsEscapedWildcards("%2"));
            Assert.IsFalse(EscapingUtilities.ContainsEscapedWildcards("%4"));
            Assert.IsFalse(EscapingUtilities.ContainsEscapedWildcards("%3A"));
            Assert.IsFalse(EscapingUtilities.ContainsEscapedWildcards("%2B"));
            Assert.IsTrue(EscapingUtilities.ContainsEscapedWildcards("%2a"));
            Assert.IsTrue(EscapingUtilities.ContainsEscapedWildcards("%2A"));
            Assert.IsTrue(EscapingUtilities.ContainsEscapedWildcards("%3F"));
            Assert.IsTrue(EscapingUtilities.ContainsEscapedWildcards("%3f"));
            Assert.IsTrue(EscapingUtilities.ContainsEscapedWildcards("%%3f"));
            Assert.IsTrue(EscapingUtilities.ContainsEscapedWildcards("%3%3f"));
        }
    }
}
