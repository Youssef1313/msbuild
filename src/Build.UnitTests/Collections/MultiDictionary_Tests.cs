// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Build.Collections;
using Xunit;

#nullable disable

namespace Microsoft.Build.UnitTests.OM.Collections
{
    [TestClass]
    /// <summary>
    /// Tests for the multi-dictionary class
    /// </summary>
    public class MultiDictionary_Tests
    {
        /// <summary>
        /// Empty dictionary
        /// </summary>
        [Fact]
        public void Empty()
        {
            MultiDictionary<string, string> dictionary = new MultiDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            Assert.AreEqual(0, dictionary.KeyCount);
            Assert.AreEqual(0, dictionary.ValueCount);

            Assert.IsFalse(dictionary.Remove("x", "y"));

            foreach (string value in dictionary["x"])
            {
                Assert.Fail();
            }
        }

        /// <summary>
        /// Remove stuff that is there
        /// </summary>
        [Fact]
        public void Remove()
        {
            MultiDictionary<string, string> dictionary = new MultiDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            dictionary.Add("x", "x1");
            dictionary.Add("x", "x2");
            dictionary.Add("y", "y1");

            Assert.IsTrue(dictionary.Remove("x", "x1"));

            Assert.AreEqual(2, dictionary.KeyCount);
            Assert.AreEqual(2, dictionary.ValueCount);

            Assert.IsTrue(dictionary.Remove("x", "x2"));

            Assert.AreEqual(1, dictionary.KeyCount);
            Assert.AreEqual(1, dictionary.ValueCount);

            Assert.IsTrue(dictionary.Remove("y", "y1"));

            Assert.AreEqual(0, dictionary.KeyCount);
            Assert.AreEqual(0, dictionary.ValueCount);

            dictionary.Add("x", "x1");
            dictionary.Add("x", "x2");

            Assert.IsTrue(dictionary.Remove("x", "x2"));

            Assert.AreEqual(1, dictionary.KeyCount);
            Assert.AreEqual(1, dictionary.ValueCount);
        }

        /// <summary>
        /// Remove stuff that isn't there
        /// </summary>
        [Fact]
        public void RemoveNonExistent()
        {
            MultiDictionary<string, string> dictionary = new MultiDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            dictionary.Add("x", "x1");
            dictionary.Add("x", "x2");
            dictionary.Add("y", "y1");

            Assert.IsFalse(dictionary.Remove("z", "y1"));
            Assert.IsFalse(dictionary.Remove("x", "y1"));
            Assert.IsFalse(dictionary.Remove("y", "y2"));

            Assert.AreEqual(2, dictionary.KeyCount);
            Assert.AreEqual(3, dictionary.ValueCount);
        }

        /// <summary>
        /// Enumerate over all values for a key
        /// </summary>
        [Fact]
        public void Enumerate()
        {
            MultiDictionary<string, string> dictionary = new MultiDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            dictionary.Add("x", "x1");
            dictionary.Add("x", "x2");
            dictionary.Add("y", "y1");

            List<string> values = Helpers.MakeList<string>(dictionary["x"]);
            values.Sort();

            Assert.AreEqual(2, values.Count);
            Assert.AreEqual("x1", values[0]);
            Assert.AreEqual("x2", values[1]);

            values = Helpers.MakeList<string>(dictionary["y"]);

            Assert.Single(values);
            Assert.AreEqual("y1", values[0]);

            values = Helpers.MakeList<string>(dictionary["z"]);

            Assert.Empty(values);
        }

        /// <summary>
        /// Mixture of adds and removes
        /// </summary>
        [Fact]
        public void MixedAddRemove()
        {
            MultiDictionary<string, string> dictionary = new MultiDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            dictionary.Add("x", "x1");
            dictionary.Remove("x", "x1");
            dictionary.Add("x", "x1");
            dictionary.Add("x", "x1");
            dictionary.Add("x", "x1");
            dictionary.Remove("x", "x1");
            dictionary.Remove("x", "x1");
            dictionary.Remove("x", "x1");
            dictionary.Add("x", "x2");

            Assert.AreEqual(1, dictionary.KeyCount);
            Assert.AreEqual(1, dictionary.ValueCount);

            List<string> values = Helpers.MakeList<string>(dictionary["x"]);

            Assert.Single(values);
            Assert.AreEqual("x2", values[0]);
        }

        /// <summary>
        /// Clearing out
        /// </summary>
        [Fact]
        public void Clear()
        {
            MultiDictionary<string, string> dictionary = new MultiDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            dictionary.Add("x", "x1");
            dictionary.Add("x", "x2");
            dictionary.Add("y", "y1");

            dictionary.Clear();

            Assert.AreEqual(0, dictionary.KeyCount);
            Assert.AreEqual(0, dictionary.ValueCount);
        }
    }
}
