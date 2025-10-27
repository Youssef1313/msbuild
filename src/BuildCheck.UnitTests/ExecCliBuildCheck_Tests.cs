// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Build.Experimental.BuildCheck;
using Microsoft.Build.Experimental.BuildCheck.Checks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using Xunit;

namespace Microsoft.Build.BuildCheck.UnitTests
{
    [TestClass]
    public sealed class ExecCliBuildCheck_Tests
    {
        private const int MaxStackSizeWindows = 1024 * 1024; // 1 MB
        private const int MaxStackSizeLinux = 1024 * 1024 * 8; // 8 MB

        private readonly ExecCliBuildCheck _check;

        private readonly MockBuildCheckRegistrationContext _registrationContext;

        public static TestDataRow<string?>[] BuildCommandTestData =>
        [
            new("dotnet build"),
            new("dotnet build&dotnet build"),
            new("dotnet     build"),
            new("dotnet clean"),
            new("dotnet msbuild"),
            new("dotnet restore"),
            new("dotnet publish"),
            new("dotnet pack"),
            new("dotnet test"),
            new("dotnet vstest"),
            new("dotnet build -p:Configuration=Release"),
            new("dotnet build /t:Restore;Clean"),
            new("dotnet build&some command"),
            new("some command&dotnet build&some other command"),
            new("some command&dotnet build"),
            new("some command&amp;dotnet build&amp;some other command"),
            new("msbuild"),
            new("msbuild /t:Build"),
            new("msbuild --t:Restore;Clean"),
            new("nuget restore"),
            new("dotnet run --project project.SLN"),
            new("dotnet run project.csproj"),
            new("dotnet run project.proj"),
            new("dotnet run"),
            new(string.Join(";", new string('a', 1025), "dotnet build", new string('a', 1025))),
            new(string.Join(";", new string('a', RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? MaxStackSizeWindows * 2 : MaxStackSizeLinux * 2), "dotnet build"))
        ];

        public static TestDataRow<string?>[] NonBuildCommandTestData =>
        [
            new("dotnet help"),
            new("where dotnet"),
            new("where msbuild"),
            new("where nuget"),
            new("dotnet bin/net472/project.dll"),
            new(string.Empty),
            new(null),
            new(new string('a', RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? MaxStackSizeWindows * 2 : MaxStackSizeLinux * 2))
        ];

        public ExecCliBuildCheck_Tests()
        {
            _check = new ExecCliBuildCheck();
            _registrationContext = new MockBuildCheckRegistrationContext();
            _check.RegisterActions(_registrationContext);
        }

        [Theory]
        [MemberData(nameof(BuildCommandTestData))]
        public void ExecTask_WithCommandExecutingBuild_ShouldShowWarning(string? command)
        {
            _registrationContext.TriggerTaskInvocationAction(MakeTaskInvocationData("Exec", new Dictionary<string, TaskInvocationCheckData.TaskParameter>
            {
                { "Command", new TaskInvocationCheckData.TaskParameter(command, IsOutput: false) },
            }));

            _registrationContext.Results.Count.ShouldBe(1);
            _registrationContext.Results[0].CheckRule.Id.ShouldBe("BC0302");
        }

        [Theory]
        [MemberData(nameof(NonBuildCommandTestData))]
        public void ExecTask_WithCommandNotExecutingBuild_ShouldNotShowWarning(string? command)
        {
            _registrationContext.TriggerTaskInvocationAction(MakeTaskInvocationData("Exec", new Dictionary<string, TaskInvocationCheckData.TaskParameter>
            {
                { "Command", new TaskInvocationCheckData.TaskParameter(command, IsOutput: false) },
            }));

            _registrationContext.Results.Count.ShouldBe(0);
        }

        private TaskInvocationCheckData MakeTaskInvocationData(string taskName, Dictionary<string, TaskInvocationCheckData.TaskParameter> parameters)
        {
            string projectFile = Framework.NativeMethods.IsWindows ? @"C:\fake\project.proj" : "/fake/project.proj";
            return new TaskInvocationCheckData(
                projectFile,
                null,
                Construction.ElementLocation.EmptyLocation,
                taskName,
                projectFile,
                parameters);
        }
    }
}
