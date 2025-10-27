// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Historically, this repo used xUnit.
// To make the migration to MSTest easier, we introduce types that used to exist in xUnit and are extensively used in the codebase.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Xunit.Abstractions
{
    /// <summary>
    /// Represents a class which can be used to provide test output.
    /// </summary>
    public interface ITestOutputHelper
    {
        /// <summary>
        /// Adds a line of text to the output.
        /// </summary>
        /// <param name="message">The message</param>
        void WriteLine(string message);

        /// <summary>
        /// Formats a line of text and adds it to the output.
        /// </summary>
        /// <param name="format">The message format</param>
        /// <param name="args">The format arguments</param>
        void WriteLine(string format, params object[] args);
    }
}

namespace Xunit
{
    public sealed class FactAttribute : TestMethodAttribute
    {
        public FactAttribute([CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = -1)
            : base(callerFilePath, callerLineNumber)
        {
        }

        // TODO: Remove this property and replace this with MSTest's IgnoreAttribute.
        // This is only added to ease the move from xunit.
        public string? Skip { get; set; }

        public override Task<TestResult[]> ExecuteAsync(ITestMethod testMethod)
        {
            if (string.IsNullOrEmpty(Skip))
            {
                return base.ExecuteAsync(testMethod);
            }

            return Task.FromResult<TestResult[]>([new TestResult()
            {
                Outcome = UnitTestOutcome.Ignored
            }]);
        }
    }

    public sealed class TheoryAttribute : TestMethodAttribute
    {
        public TheoryAttribute([CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = -1)
            : base(callerFilePath, callerLineNumber)
        {
        }

        // TODO: Remove this property and replace this with MSTest's IgnoreAttribute.
        // This is only added to ease the move from xunit.
        public string? Skip { get; set; }

        public override Task<TestResult[]> ExecuteAsync(ITestMethod testMethod)
        {
            if (string.IsNullOrEmpty(Skip))
            {
                return base.ExecuteAsync(testMethod);
            }

            return Task.FromResult<TestResult[]>([new TestResult()
            {
                Outcome = UnitTestOutcome.Ignored
            }]);
        }
    }

    public sealed class InlineDataAttribute : DataRowAttribute
    {
        public InlineDataAttribute(params object?[]? data) : base(data)
        {
        }
    }

    public sealed class TraitAttribute : TestPropertyAttribute
    {
        public TraitAttribute(string name, string value)
            : base(name, value)
        {
        }
    }

    /// <summary>
    /// Attribute to define dynamic data for a test method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public sealed class MemberDataAttribute : Attribute, ITestDataSource, ITestDataSourceIgnoreCapability
    {
        private readonly DynamicDataAttribute _dynamicDataAttribute;

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicDataAttribute"/> class.
        /// </summary>
        /// <param name="dynamicDataSourceName">
        /// The name of method, property, or field having test data.
        /// </param>
        public MemberDataAttribute(string dynamicDataSourceName)
        {
            _dynamicDataAttribute = new DynamicDataAttribute(dynamicDataSourceName);
        }

        /// <summary>
        /// Gets or sets a reason to ignore this dynamic data source. Setting the property to non-null value will ignore the dynamic data source.
        /// </summary>
        public string? IgnoreMessage
        {
            get => _dynamicDataAttribute.IgnoreMessage;
            set => _dynamicDataAttribute.IgnoreMessage = value;
        }

        /// <inheritdoc />
        public IEnumerable<object[]> GetData(MethodInfo methodInfo)
            => _dynamicDataAttribute.GetData(methodInfo);

        /// <inheritdoc />
        public string? GetDisplayName(MethodInfo methodInfo, object?[]? data)
            => _dynamicDataAttribute.GetDisplayName(methodInfo, data);
    }
}
