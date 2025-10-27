// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MSTest.MSBuildExtensions;

namespace Microsoft.Build.UnitTests
{
    /// <summary>
    ///  This test should be run only on Windows, and when long path support is enabled.
    ///  It is possible to conditionally restrict the fact to be run only on full .NET Framework.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class LongPathSupportConditionAttribute : ConditionBaseAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LongPathSupportConditionAttribute"/> class.
        /// </summary>
        /// <param name="additionalMessage">The additional message that is appended to skip reason, when test is skipped.</param>
        /// <param name="fullFrameworkOnly"><see langword="true"/> if the test can be run only on full framework. The default value is <see langword="false"/>.</param>
        public LongPathSupportConditionAttribute(string? additionalMessage = null, bool fullFrameworkOnly = false)
            : base(ConditionMode.Include)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                IgnoreMessage = "This test only runs on Windows and when long path support is disabled.".AppendAdditionalMessage(additionalMessage);
                IsConditionMet = false;
                return;
            }

            if (fullFrameworkOnly && !CustomXunitAttributesUtilities.IsBuiltAgainstNetFramework)
            {
                IgnoreMessage = "This test only runs on full .NET Framework and when long path support is disabled.".AppendAdditionalMessage(additionalMessage);
                IsConditionMet = false;
                return;
            }

            if (!NativeMethodsShared.IsMaxPathLegacyWindows())
            {
                IgnoreMessage = "This test only runs when long path support is disabled.".AppendAdditionalMessage(additionalMessage);
                IsConditionMet = false;
            }

            IsConditionMet = true;
        }

        public override string GroupName => nameof(LongPathSupportConditionAttribute);

        public override bool IsConditionMet { get; }
    }
}
