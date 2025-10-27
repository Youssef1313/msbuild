// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Tasks;
using Xunit;

#nullable disable

namespace Microsoft.Build.UnitTests
{
    [TestClass]
    public sealed class Culture_Tests
    {
        /*
        * Method:   Basic
        *
        * Test the basic functionality.
        */
        [Fact]
        public void Basic()
        {
            Culture.ItemCultureInfo info = Culture.GetItemCultureInfo("MyResource.fr.resx", null);
            Assert.AreEqual("fr", info.culture);
            Assert.AreEqual("MyResource.resx", info.cultureNeutralFilename);
        }

        /*
        * Method:   NonCultureFile
        *
        * The item doesn't have a culture, and there isn't one embedded in the file name.
        */
        [Fact]
        public void NonCultureFile()
        {
            Culture.ItemCultureInfo info = Culture.GetItemCultureInfo("MyResource.resx", null);
            Assert.IsNull(info.culture);
            Assert.AreEqual("MyResource.resx", info.cultureNeutralFilename);
        }


        /*
        * Method:   BogusEmbeddedCulture
        *
        * The item has something that looks like an embedded culture, but isn't.
        */
        [Fact]
        [Trait("Category", "netcore-osx-failing")]
        [Trait("Category", "netcore-linux-failing")]
        public void BogusEmbeddedCulture()
        {
            Culture.ItemCultureInfo info = Culture.GetItemCultureInfo("MyResource.notalocale.resx", null);
            Assert.IsNull(info.culture);
            Assert.AreEqual("MyResource.notalocale.resx", info.cultureNeutralFilename);
        }
    }
}
