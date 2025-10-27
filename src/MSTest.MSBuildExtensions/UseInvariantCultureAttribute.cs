// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MSTest.MSBuildExtensions
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class UseInvariantCultureTestMethodAttribute : TestMethodAttribute
    {
        private readonly TestMethodAttribute? _wrappedTestMethodAttribute;

        public UseInvariantCultureTestMethodAttribute(
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = -1)
            : base(callerFilePath, callerLineNumber)
        {
        }

#pragma warning disable MSTEST0057 // false positive
        public UseInvariantCultureTestMethodAttribute(
            TestMethodAttribute testMethodAttribute,
            string callerFilePath,
            int callerLineNumber)
            : base(callerFilePath, callerLineNumber)
        {
            _wrappedTestMethodAttribute = testMethodAttribute;
        }
#pragma warning restore MSTEST0057 // false positive

        public override async Task<TestResult[]> ExecuteAsync(ITestMethod testMethod)
        {
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUICulture = CultureInfo.CurrentUICulture;

            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            try
            {
                return _wrappedTestMethodAttribute is null
                    ? await base.ExecuteAsync(testMethod)
                    : await _wrappedTestMethodAttribute.ExecuteAsync(testMethod);
            }
            finally
            {
                if (originalCulture != null)
                {
                    CultureInfo.CurrentCulture = originalCulture;
                }

                if (originalUICulture != null)
                {
                    CultureInfo.CurrentUICulture = originalUICulture;
                }
            }
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class UseInvariantCultureTestClassAttribute : TestClassAttribute
    {
        public override TestMethodAttribute? GetTestMethodAttribute(TestMethodAttribute testMethodAttribute)
            => new UseInvariantCultureTestMethodAttribute(testMethodAttribute, testMethodAttribute.DeclaringFilePath, testMethodAttribute.DeclaringLineNumber ?? -1);
    }
}
