// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Build.Shared;

using Shouldly;
using Xunit;

#nullable disable

namespace Microsoft.Build.UnitTests
{
    public class XmakeAttributesTest
    {
        [Fact]
        public void TestIsSpecialTaskAttribute()
        {
            Assert.IsFalse(XMakeAttributes.IsSpecialTaskAttribute("NotAnAttribute"));
            Assert.IsTrue(XMakeAttributes.IsSpecialTaskAttribute(XMakeAttributes.xmlns));
            Assert.IsTrue(XMakeAttributes.IsSpecialTaskAttribute(XMakeAttributes.continueOnError));
            Assert.IsTrue(XMakeAttributes.IsSpecialTaskAttribute(XMakeAttributes.condition));
            Assert.IsTrue(XMakeAttributes.IsSpecialTaskAttribute(XMakeAttributes.msbuildArchitecture));
            Assert.IsTrue(XMakeAttributes.IsSpecialTaskAttribute(XMakeAttributes.msbuildRuntime));
        }

        [Fact]
        public void TestIsBadlyCasedSpecialTaskAttribute()
        {
            Assert.IsFalse(XMakeAttributes.IsBadlyCasedSpecialTaskAttribute("NotAnAttribute"));
            Assert.IsFalse(XMakeAttributes.IsBadlyCasedSpecialTaskAttribute(XMakeAttributes.include));
            Assert.IsFalse(XMakeAttributes.IsBadlyCasedSpecialTaskAttribute(XMakeAttributes.continueOnError));
            Assert.IsFalse(XMakeAttributes.IsBadlyCasedSpecialTaskAttribute(XMakeAttributes.condition));
            Assert.IsFalse(XMakeAttributes.IsBadlyCasedSpecialTaskAttribute(XMakeAttributes.msbuildArchitecture));
            Assert.IsFalse(XMakeAttributes.IsBadlyCasedSpecialTaskAttribute(XMakeAttributes.msbuildRuntime));
            Assert.IsTrue(XMakeAttributes.IsBadlyCasedSpecialTaskAttribute("continueOnError"));
            Assert.IsTrue(XMakeAttributes.IsBadlyCasedSpecialTaskAttribute("condition"));
            Assert.IsTrue(XMakeAttributes.IsBadlyCasedSpecialTaskAttribute("MsbuildRuntime"));
            Assert.IsTrue(XMakeAttributes.IsBadlyCasedSpecialTaskAttribute("msbuildarchitecture"));
        }

        [Fact]
        public void TestIsNonBatchingTargetAttribute()
        {
            Assert.IsFalse(XMakeAttributes.IsNonBatchingTargetAttribute("NotAnAttribute"));
            Assert.IsTrue(XMakeAttributes.IsNonBatchingTargetAttribute(XMakeAttributes.dependsOnTargets));
            Assert.IsTrue(XMakeAttributes.IsNonBatchingTargetAttribute(XMakeAttributes.name));
            Assert.IsTrue(XMakeAttributes.IsNonBatchingTargetAttribute(XMakeAttributes.condition));
        }

        [Fact]
        public void TestRuntimeValuesMatch()
        {
            Assert.IsTrue(XMakeAttributes.RuntimeValuesMatch(XMakeAttributes.MSBuildRuntimeValues.any, XMakeAttributes.MSBuildRuntimeValues.currentRuntime));
            Assert.IsTrue(XMakeAttributes.RuntimeValuesMatch(XMakeAttributes.MSBuildRuntimeValues.any, XMakeAttributes.MSBuildRuntimeValues.net));
            Assert.IsTrue(XMakeAttributes.RuntimeValuesMatch(XMakeAttributes.MSBuildRuntimeValues.any, XMakeAttributes.MSBuildRuntimeValues.clr4));
            Assert.IsTrue(XMakeAttributes.RuntimeValuesMatch(XMakeAttributes.MSBuildRuntimeValues.clr2, XMakeAttributes.MSBuildRuntimeValues.any));
#if NET5_0_OR_GREATER
            Assert.IsTrue(XMakeAttributes.RuntimeValuesMatch(XMakeAttributes.MSBuildRuntimeValues.currentRuntime, XMakeAttributes.MSBuildRuntimeValues.net));
#else
            Assert.IsTrue(XMakeAttributes.RuntimeValuesMatch(XMakeAttributes.MSBuildRuntimeValues.currentRuntime, XMakeAttributes.MSBuildRuntimeValues.clr4));
#endif

            // Never true
            Assert.IsFalse(XMakeAttributes.RuntimeValuesMatch(XMakeAttributes.MSBuildRuntimeValues.currentRuntime, XMakeAttributes.MSBuildRuntimeValues.clr2));

            Assert.IsFalse(XMakeAttributes.RuntimeValuesMatch(XMakeAttributes.MSBuildRuntimeValues.clr4, XMakeAttributes.MSBuildRuntimeValues.clr2));
            Assert.IsFalse(XMakeAttributes.RuntimeValuesMatch(XMakeAttributes.MSBuildRuntimeValues.clr4, XMakeAttributes.MSBuildRuntimeValues.net));
            Assert.IsFalse(XMakeAttributes.RuntimeValuesMatch(XMakeAttributes.MSBuildRuntimeValues.clr2, XMakeAttributes.MSBuildRuntimeValues.net));
        }

        [Theory]
        [InlineData(XMakeAttributes.MSBuildRuntimeValues.any, XMakeAttributes.MSBuildRuntimeValues.clr4, true, XMakeAttributes.MSBuildRuntimeValues.clr4)]
        [InlineData(XMakeAttributes.MSBuildRuntimeValues.clr4, XMakeAttributes.MSBuildRuntimeValues.any, true, XMakeAttributes.MSBuildRuntimeValues.clr4)]
        [InlineData(XMakeAttributes.MSBuildRuntimeValues.clr2, XMakeAttributes.MSBuildRuntimeValues.any, true, XMakeAttributes.MSBuildRuntimeValues.clr2)]
        [InlineData(XMakeAttributes.MSBuildRuntimeValues.currentRuntime, XMakeAttributes.MSBuildRuntimeValues.clr2, false, null)]
        [InlineData(XMakeAttributes.MSBuildRuntimeValues.clr4, XMakeAttributes.MSBuildRuntimeValues.clr2, false, null)]
        public void TestMergeRuntimeValues(string left, string right, bool success, string expected)
        {
            XMakeAttributes.TryMergeRuntimeValues(left, right, out string mergedRuntime)
                .ShouldBe(success);

            mergedRuntime.ShouldBe(expected);
        }

        [Fact]
        public void TestMergeRuntimeValuesAnyAcceptsCurrent()
        {
            XMakeAttributes.TryMergeRuntimeValues(XMakeAttributes.MSBuildRuntimeValues.any,
                XMakeAttributes.MSBuildRuntimeValues.currentRuntime,
                out string mergedRuntime)
                .ShouldBeTrue();

            mergedRuntime.ShouldBe(XMakeAttributes.GetCurrentMSBuildRuntime());
        }

        [WindowsFullFrameworkOnlyFact(additionalMessage: "Tests whether 'current' merges with 'clr4' which is true only on Framework.")]
        public void TestMergeRuntimeValuesCurrentToClr4()
        {
            XMakeAttributes.TryMergeRuntimeValues(
                XMakeAttributes.MSBuildRuntimeValues.currentRuntime,
                XMakeAttributes.MSBuildRuntimeValues.clr4,
                out string mergedRuntime).ShouldBeTrue();
            mergedRuntime.ShouldBe(XMakeAttributes.MSBuildRuntimeValues.clr4);

            XMakeAttributes.TryMergeRuntimeValues(
                XMakeAttributes.MSBuildRuntimeValues.currentRuntime,
                XMakeAttributes.MSBuildRuntimeValues.net,
                out mergedRuntime).ShouldBeFalse();
            mergedRuntime.ShouldBeNull();
        }

        [DotNetOnlyFact(additionalMessage: "Tests whether 'current' merges with 'net' which is true only on core.")]
        public void TestMergeRuntimeValuesCurrentToCore()
        {
            XMakeAttributes.TryMergeRuntimeValues(
                XMakeAttributes.MSBuildRuntimeValues.currentRuntime,
                XMakeAttributes.MSBuildRuntimeValues.net,
                out string mergedRuntime).ShouldBeTrue();
            mergedRuntime.ShouldBe(XMakeAttributes.MSBuildRuntimeValues.net);

            XMakeAttributes.TryMergeRuntimeValues(
                XMakeAttributes.MSBuildRuntimeValues.currentRuntime,
                XMakeAttributes.MSBuildRuntimeValues.clr4,
                out mergedRuntime).ShouldBeFalse();
            mergedRuntime.ShouldBeNull();
        }

        [Fact]
        public void TestArchitectureValuesMatch()
        {
            string currentArchitecture = XMakeAttributes.GetCurrentMSBuildArchitecture();
            string notCurrentArchitecture = Environment.Is64BitProcess ? XMakeAttributes.MSBuildArchitectureValues.x86 : XMakeAttributes.MSBuildArchitectureValues.x64;

            Assert.IsTrue(XMakeAttributes.ArchitectureValuesMatch(XMakeAttributes.MSBuildArchitectureValues.any, XMakeAttributes.MSBuildArchitectureValues.currentArchitecture));
            Assert.IsTrue(XMakeAttributes.ArchitectureValuesMatch(XMakeAttributes.MSBuildArchitectureValues.any, XMakeAttributes.MSBuildArchitectureValues.x64));
            Assert.IsTrue(XMakeAttributes.ArchitectureValuesMatch(XMakeAttributes.MSBuildArchitectureValues.x86, XMakeAttributes.MSBuildArchitectureValues.any));
            Assert.IsTrue(XMakeAttributes.ArchitectureValuesMatch(XMakeAttributes.MSBuildArchitectureValues.currentArchitecture, currentArchitecture));

            Assert.IsFalse(XMakeAttributes.ArchitectureValuesMatch(XMakeAttributes.MSBuildArchitectureValues.currentArchitecture, notCurrentArchitecture));
            Assert.IsFalse(XMakeAttributes.ArchitectureValuesMatch(XMakeAttributes.MSBuildArchitectureValues.x64, XMakeAttributes.MSBuildArchitectureValues.x86));
        }

        [Fact]
        public void TestMergeArchitectureValues()
        {
            string currentArchitecture = XMakeAttributes.GetCurrentMSBuildArchitecture();
            string notCurrentArchitecture = Environment.Is64BitProcess ? XMakeAttributes.MSBuildArchitectureValues.x86 : XMakeAttributes.MSBuildArchitectureValues.x64;

            string mergedArchitecture;
            Assert.IsTrue(XMakeAttributes.TryMergeArchitectureValues(XMakeAttributes.MSBuildArchitectureValues.any, XMakeAttributes.MSBuildArchitectureValues.currentArchitecture, out mergedArchitecture));
            Assert.AreEqual(currentArchitecture, mergedArchitecture);

            Assert.IsTrue(XMakeAttributes.TryMergeArchitectureValues(XMakeAttributes.MSBuildArchitectureValues.any, XMakeAttributes.MSBuildArchitectureValues.x64, out mergedArchitecture));
            Assert.AreEqual(XMakeAttributes.MSBuildArchitectureValues.x64, mergedArchitecture);

            Assert.IsTrue(XMakeAttributes.TryMergeArchitectureValues(XMakeAttributes.MSBuildArchitectureValues.x86, XMakeAttributes.MSBuildArchitectureValues.any, out mergedArchitecture));
            Assert.AreEqual(XMakeAttributes.MSBuildArchitectureValues.x86, mergedArchitecture);

            Assert.IsTrue(XMakeAttributes.TryMergeArchitectureValues(XMakeAttributes.MSBuildArchitectureValues.currentArchitecture, currentArchitecture, out mergedArchitecture));
            Assert.AreEqual(currentArchitecture, mergedArchitecture);

            Assert.IsFalse(XMakeAttributes.TryMergeArchitectureValues(XMakeAttributes.MSBuildArchitectureValues.currentArchitecture, notCurrentArchitecture, out mergedArchitecture));
            Assert.IsFalse(XMakeAttributes.TryMergeArchitectureValues(XMakeAttributes.MSBuildArchitectureValues.x64, XMakeAttributes.MSBuildArchitectureValues.x86, out mergedArchitecture));
        }
    }
}
