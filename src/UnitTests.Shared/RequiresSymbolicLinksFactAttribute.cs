// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

using Microsoft.Build.Shared;
using Xunit;

namespace Microsoft.Build.UnitTests
{
    /// <summary>
    /// A custom <see cref="FactAttribute"/> that skips the test if the OS doesn't support creating symlinks.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class RequiresSymbolicLinksConditionAttribute : ConditionBaseAttribute
    {
        private static readonly bool s_runningInAzurePipeline =
            bool.TryParse(Environment.GetEnvironmentVariable("TF_BUILD"), out bool value) && value;

        public RequiresSymbolicLinksConditionAttribute()
            : base(ConditionMode.Include)
        {
            if (s_runningInAzurePipeline || !NativeMethodsShared.IsWindows)
            {
                IsConditionMet = true;
                return;
            }

            // In Windows, a process can create symlinks only if it has sufficient permissions.
            // We simply try to create one and if it fails we skip the test.
            string sourceFile = FileUtilities.GetTemporaryFile();
            string destinationFile = FileUtilities.GetTemporaryFileName();
            try
            {
                File.Create(sourceFile).Dispose();

                string? errorMessage = null;
                if (!NativeMethodsShared.MakeSymbolicLink(destinationFile, sourceFile, ref errorMessage))
                {
                    IgnoreMessage = "Requires permission to create symbolic links. Need to be run elevated or under development mode " +
                        "(https://learn.microsoft.com/en-us/windows/apps/get-started/enable-your-device-for-development).";
                    IsConditionMet = false;
                }
            }
            finally
            {
                if (File.Exists(sourceFile))
                {
                    File.Delete(sourceFile);
                }
                if (File.Exists(destinationFile))
                {
                    File.Delete(destinationFile);
                }
            }
        }

        public override string GroupName => nameof(RequiresSymbolicLinksConditionAttribute);

        public override bool IsConditionMet { get; }
    }
}
