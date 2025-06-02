// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Xml;
using Microsoft.Build.Construction;
using Xunit;
using InvalidProjectFileException = Microsoft.Build.Exceptions.InvalidProjectFileException;

#nullable disable

namespace Microsoft.Build.UnitTests.OM.Construction
{
    /// <summary>
    /// Tests for the ProjectUsingParameterElement class
    /// </summary>
    public class UsingTaskParameterElement_Tests
    {
        /// <summary>
        /// Parameter element with all attributes set
        /// </summary>
        private static string s_contentAllAttributesSet = @"
                    <Project>
                        <UsingTask TaskName='SuperTask' AssemblyFile='af' TaskFactory='AssemblyFactory'>
                           <ParameterGroup>
                              <MyParameter ParameterType='System.String' Output='true' Required='false'/>
                           </ParameterGroup>
                       </UsingTask>
                    </Project>
                ";

        /// <summary>
        /// Parameter element with no attributes set
        /// </summary>
        private static string s_contentNoAttributesSet = @"
                    <Project>
                        <UsingTask TaskName='SuperTask' AssemblyFile='af' TaskFactory='AssemblyFactory'>
                           <ParameterGroup>
                              <MyParameter/>
                           </ParameterGroup>
                       </UsingTask>
                    </Project>
                ";

        /// <summary>
        /// Read simple task body
        /// </summary>
        [TestMethod]
        public void ReadParameterWithAllAttributes()
        {
            ProjectUsingTaskParameterElement parameter = GetParameterXml(s_contentAllAttributesSet);

            Assert.Equal("MyParameter", parameter.Name);
            Assert.Equal("System.String", parameter.ParameterType);
            Assert.Equal("true", parameter.Output);
            Assert.Equal("false", parameter.Required);
        }

        /// <summary>
        /// Read simple task body
        /// </summary>
        [TestMethod]
        public void ReadParameterWithNOAttributes()
        {
            ProjectUsingTaskParameterElement parameter = GetParameterXml(s_contentNoAttributesSet);

            Assert.Equal("MyParameter", parameter.Name);
            Assert.Equal(typeof(String).FullName, parameter.ParameterType);
            Assert.Equal(bool.FalseString, parameter.Output);
            Assert.Equal(bool.FalseString, parameter.Required);
        }

        /// <summary>
        /// Read parameter with an invalid attribute
        /// </summary>
        [TestMethod]
        public void ReadInvalidAttribute()
        {
            Assert.Throws<InvalidProjectFileException>(() =>
            {
                string content = @"
                    <Project>
                        <UsingTask TaskName='SuperTask' AssemblyFile='af' TaskFactory='AssemblyFactory'>
                           <ParameterGroup>
                              <MyParameter Invaliid='System.String'/>
                           </ParameterGroup>
                       </UsingTask>
                    </Project>
                ";

                ProjectRootElement.Create(XmlReader.Create(new StringReader(content)));
                Assert.Fail();
            });
        }
        /// <summary>
        /// Set type value
        /// </summary>
        [TestMethod]
        public void SetType()
        {
            ProjectUsingTaskParameterElement parameter = GetParameterXml(s_contentAllAttributesSet);
            Helpers.ClearDirtyFlag(parameter.ContainingProject);

            parameter.ParameterType = "newType";
            Assert.Equal("newType", parameter.ParameterType);
            Assert.True(parameter.ContainingProject.HasUnsavedChanges);
        }

        /// <summary>
        /// Set output value
        /// </summary>
        [TestMethod]
        public void SetOutput()
        {
            ProjectUsingTaskParameterElement parameter = GetParameterXml(s_contentAllAttributesSet);
            Helpers.ClearDirtyFlag(parameter.ContainingProject);

            parameter.Output = "output";
            Assert.Equal("output", parameter.Output);
            Assert.True(parameter.ContainingProject.HasUnsavedChanges);
        }

        /// <summary>
        /// Set required value
        /// </summary>
        [TestMethod]
        public void SetRequired()
        {
            ProjectUsingTaskParameterElement parameter = GetParameterXml(s_contentAllAttributesSet);
            Helpers.ClearDirtyFlag(parameter.ContainingProject);

            parameter.Required = "required";
            Assert.Equal("required", parameter.Required);
            Assert.True(parameter.ContainingProject.HasUnsavedChanges);
        }

        /// <summary>
        /// Set type to empty
        /// </summary>
        [TestMethod]
        public void SetEmptyType()
        {
            ProjectUsingTaskParameterElement parameter = GetParameterXml(s_contentAllAttributesSet);
            Helpers.ClearDirtyFlag(parameter.ContainingProject);

            parameter.ParameterType = String.Empty;
            Assert.Equal(typeof(String).FullName, parameter.ParameterType);
            Assert.True(parameter.ContainingProject.HasUnsavedChanges);
        }

        /// <summary>
        /// Set type output to empty
        /// </summary>
        [TestMethod]
        public void SetEmptyOutput()
        {
            ProjectUsingTaskParameterElement parameter = GetParameterXml(s_contentAllAttributesSet);
            Helpers.ClearDirtyFlag(parameter.ContainingProject);

            parameter.Output = String.Empty;
            Assert.Equal(bool.FalseString, parameter.Output);
            Assert.True(parameter.ContainingProject.HasUnsavedChanges);
        }

        /// <summary>
        /// Set type required to empty
        /// </summary>
        [TestMethod]
        public void SetEmptyRequired()
        {
            ProjectUsingTaskParameterElement parameter = GetParameterXml(s_contentAllAttributesSet);
            Helpers.ClearDirtyFlag(parameter.ContainingProject);

            parameter.Required = String.Empty;
            Assert.Equal(bool.FalseString, parameter.Required);
            Assert.True(parameter.ContainingProject.HasUnsavedChanges);
        }

        /// <summary>
        /// Set type to null
        /// </summary>
        [TestMethod]
        public void SetNullType()
        {
            ProjectUsingTaskParameterElement parameter = GetParameterXml(s_contentAllAttributesSet);
            Helpers.ClearDirtyFlag(parameter.ContainingProject);

            parameter.ParameterType = null;
            Assert.Equal(typeof(String).FullName, parameter.ParameterType);
            Assert.True(parameter.ContainingProject.HasUnsavedChanges);
        }

        /// <summary>
        /// Set type output to null
        /// </summary>
        [TestMethod]
        public void SetNullOutput()
        {
            ProjectUsingTaskParameterElement parameter = GetParameterXml(s_contentAllAttributesSet);
            Helpers.ClearDirtyFlag(parameter.ContainingProject);

            parameter.Output = null;
            Assert.Equal(bool.FalseString, parameter.Output);
            Assert.True(parameter.ContainingProject.HasUnsavedChanges);
        }

        /// <summary>
        /// Set type required to null
        /// </summary>
        [TestMethod]
        public void SetNullRequired()
        {
            ProjectUsingTaskParameterElement parameter = GetParameterXml(s_contentAllAttributesSet);
            Helpers.ClearDirtyFlag(parameter.ContainingProject);

            parameter.Required = null;
            Assert.Equal(bool.FalseString, parameter.Required);
            Assert.True(parameter.ContainingProject.HasUnsavedChanges);
        }

        /// <summary>
        /// Helper to get a UsingTaskParameterElement from xml
        /// </summary>
        private static ProjectUsingTaskParameterElement GetParameterXml(string contents)
        {
            using ProjectRootElementFromString projectRootElementFromString = new(contents);
            ProjectRootElement project = projectRootElementFromString.Project;
            ProjectUsingTaskElement usingTask = (ProjectUsingTaskElement)Helpers.GetFirst(project.Children);
            UsingTaskParameterGroupElement parameterGroup = usingTask.ParameterGroup;
            ProjectUsingTaskParameterElement body = Helpers.GetFirst(parameterGroup.Parameters);
            return body;
        }
    }
}
