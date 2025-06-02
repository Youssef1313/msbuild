// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Framework;
using Microsoft.Build.Shared;

#nullable disable

namespace Microsoft.Build.UnitTests
{
    public sealed class ErrorUtilities_Tests
    {
        [TestMethod]
        public void VerifyThrowFalse()
        {
            try
            {
                ErrorUtilities.VerifyThrow(false, "msbuild rules");
            }
            catch (InternalErrorException e)
            {
                Assert.Contains("msbuild rules", e.Message); // "exception message"
                return;
            }

            Assert.Fail("Should have thrown an exception");
        }

        [TestMethod]
        public void VerifyThrowTrue()
        {
            // This shouldn't throw.
            ErrorUtilities.VerifyThrow(true, "msbuild rules");
        }

        [TestMethod]
        public void VerifyThrow0True()
        {
            // This shouldn't throw.
            ErrorUtilities.VerifyThrow(true, "blah");
        }

        [TestMethod]
        public void VerifyThrow1True()
        {
            // This shouldn't throw.
            ErrorUtilities.VerifyThrow(true, "{0}", "a");
        }

        [TestMethod]
        public void VerifyThrow2True()
        {
            // This shouldn't throw.
            ErrorUtilities.VerifyThrow(true, "{0}{1}", "a", "b");
        }

        [TestMethod]
        public void VerifyThrow3True()
        {
            // This shouldn't throw.
            ErrorUtilities.VerifyThrow(true, "{0}{1}{2}", "a", "b", "c");
        }

        [TestMethod]
        public void VerifyThrow4True()
        {
            // This shouldn't throw.
            ErrorUtilities.VerifyThrow(true, "{0}{1}{2}{3}", "a", "b", "c", "d");
        }
    }
}
