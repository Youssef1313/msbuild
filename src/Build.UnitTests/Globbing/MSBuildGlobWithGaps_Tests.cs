// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Linq;
using Microsoft.Build.Globbing;
using Xunit;

#nullable disable

namespace Microsoft.Build.Engine.UnitTests.Globbing
{
    public class MSBuildGlobWithGaps_Tests
    {
        [TestMethod]
        public void GlobWithGapsShouldWorkWithNoGaps()
        {
            var glob = new MSBuildGlobWithGaps(MSBuildGlob.Parse("a*"), Enumerable.Empty<IMSBuildGlob>());

            Assert.True(glob.IsMatch("ab"));
        }

        [TestMethod]
        public void GlobWithGapsShouldMatchIfNoGapsMatch()
        {
            var glob = new MSBuildGlobWithGaps(MSBuildGlob.Parse("a*"), MSBuildGlob.Parse("b*"));

            Assert.True(glob.IsMatch("ab"));
        }

        [TestMethod]
        public void GlobWithGapsShouldNotMatchIfGapsMatch()
        {
            var glob = new MSBuildGlobWithGaps(MSBuildGlob.Parse("a*"), MSBuildGlob.Parse("*b"));

            Assert.False(glob.IsMatch("ab"));
        }
    }
}
