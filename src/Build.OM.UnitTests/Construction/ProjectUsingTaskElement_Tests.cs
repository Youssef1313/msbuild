// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Xml;
using Microsoft.Build.Construction;
using InvalidProjectFileException = Microsoft.Build.Exceptions.InvalidProjectFileException;

#nullable disable

namespace Microsoft.Build.UnitTests.OM.Construction
{
    /// <summary>
    /// Tests for the ProjectUsingTaskElement class
    /// </summary>
    public class ProjectUsingTaskElement_Tests
    {
        /// <summary>
        /// Read project with no usingtasks
        /// </summary>
        [TestMethod]
        public void ReadNone()
        {
            ProjectRootElement project = ProjectRootElement.Create();

            Assert.Empty(project.UsingTasks);
        }

        /// <summary>
        /// Read usingtask with no task name attribute
        /// </summary>
        [TestMethod]
        public void ReadInvalidMissingTaskName()
        {
            Assert.Throws<InvalidProjectFileException>(() =>
            {
                string content = @"
                    <Project>
                        <UsingTask AssemblyFile='af'/>
                    </Project>
                ";

                ProjectRootElement.Create(XmlReader.Create(new StringReader(content)));
            });
        }
        /// <summary>
        /// Read usingtask with empty task name attribute
        /// </summary>
        [TestMethod]
        public void ReadInvalidEmptyTaskName()
        {
            Assert.Throws<InvalidProjectFileException>(() =>
            {
                string content = @"
                    <Project>
                        <UsingTask TaskName='' AssemblyFile='af'/>
                    </Project>
                ";

                ProjectRootElement.Create(XmlReader.Create(new StringReader(content)));
            });
        }
        /// <summary>
        /// Read usingtask with unexpected attribute
        /// </summary>
        [TestMethod]
        public void ReadInvalidAttribute()
        {
            Assert.Throws<InvalidProjectFileException>(() =>
            {
                string content = @"
                    <Project>
                        <UsingTask TaskName='t' AssemblyFile='af' X='Y'/>
                    </Project>
                ";

                ProjectRootElement.Create(XmlReader.Create(new StringReader(content)));
            });
        }
        /// <summary>
        /// Read usingtask with neither AssemblyFile nor AssemblyName attributes
        /// </summary>
        [TestMethod]
        public void ReadInvalidMissingAssemblyFileAssemblyName()
        {
            Assert.Throws<InvalidProjectFileException>(() =>
            {
                string content = @"
                    <Project>
                        <UsingTask TaskName='t'/>
                    </Project>
                ";

                ProjectRootElement.Create(XmlReader.Create(new StringReader(content)));
            });
        }
        /// <summary>
        /// Read usingtask with only empty AssemblyFile attribute
        /// </summary>
        [TestMethod]
        public void ReadInvalidEmptyAssemblyFile()
        {
            Assert.Throws<InvalidProjectFileException>(() =>
            {
                string content = @"
                    <Project>
                        <UsingTask TaskName='t' AssemblyFile=''/>
                    </Project>
                ";

                ProjectRootElement.Create(XmlReader.Create(new StringReader(content)));
            });
        }
        /// <summary>
        /// Read usingtask with empty AssemblyFile attribute but AssemblyName present
        /// </summary>
        [TestMethod]
        public void ReadInvalidEmptyAssemblyFileAndAssemblyNameNotEmpty()
        {
            Assert.Throws<InvalidProjectFileException>(() =>
            {
                string content = @"
                    <Project>
                        <UsingTask TaskName='t' AssemblyFile='' AssemblyName='n'/>
                    </Project>
                ";

                ProjectRootElement.Create(XmlReader.Create(new StringReader(content)));
            });
        }
        /// <summary>
        /// Read usingtask with only empty AssemblyName attribute but AssemblyFile present
        /// </summary>
        [TestMethod]
        public void ReadInvalidEmptyAssemblyNameAndAssemblyFileNotEmpty()
        {
            Assert.Throws<InvalidProjectFileException>(() =>
            {
                string content = @"
                    <Project>
                        <UsingTask TaskName='t' AssemblyName='' AssemblyFile='f'/>
                    </Project>
                ";

                ProjectRootElement.Create(XmlReader.Create(new StringReader(content)));
            });
        }
        /// <summary>
        /// Read usingtask with both AssemblyName and AssemblyFile attributes
        /// </summary>
        [TestMethod]
        public void ReadInvalidBothAssemblyFileAssemblyName()
        {
            Assert.Throws<InvalidProjectFileException>(() =>
            {
                string content = @"
                    <Project>
                        <UsingTask TaskName='t' AssemblyName='an' AssemblyFile='af'/>
                    </Project>
                ";

                ProjectRootElement.Create(XmlReader.Create(new StringReader(content)));
            });
        }
        /// <summary>
        /// Read usingtask with both AssemblyName and AssemblyFile attributes but both are empty
        /// </summary>
        [TestMethod]
        public void ReadInvalidBothEmptyAssemblyFileEmptyAssemblyNameBoth()
        {
            Assert.Throws<InvalidProjectFileException>(() =>
            {
                string content = @"
                    <Project>
                        <UsingTask TaskName='t' AssemblyName='' AssemblyFile=''/>
                    </Project>
                ";

                ProjectRootElement.Create(XmlReader.Create(new StringReader(content)));
            });
        }
        /// <summary>
        /// Read usingtask with assembly file
        /// </summary>
        [TestMethod]
        public void ReadBasicUsingTaskAssemblyFile()
        {
            ProjectUsingTaskElement usingTask = GetUsingTaskAssemblyFile();

            Assert.Equal("t1", usingTask.TaskName);
            Assert.Equal("af", usingTask.AssemblyFile);
            Assert.Equal(String.Empty, usingTask.AssemblyName);
            Assert.Equal(String.Empty, usingTask.Condition);
        }

        /// <summary>
        /// Read usingtask with assembly name
        /// </summary>
        [TestMethod]
        public void ReadBasicUsingTaskAssemblyName()
        {
            ProjectUsingTaskElement usingTask = GetUsingTaskAssemblyName();

            Assert.Equal("t2", usingTask.TaskName);
            Assert.Equal(String.Empty, usingTask.AssemblyFile);
            Assert.Equal("an", usingTask.AssemblyName);
            Assert.Equal("c", usingTask.Condition);
        }

        /// <summary>
        /// Read usingtask with task factory, required runtime and required platform
        /// </summary>
        [TestMethod]
        public void ReadBasicUsingTaskFactoryRuntimeAndPlatform()
        {
            ProjectUsingTaskElement usingTask = GetUsingTaskFactoryRuntimeAndPlatform();

            Assert.Equal("t2", usingTask.TaskName);
            Assert.Equal(String.Empty, usingTask.AssemblyFile);
            Assert.Equal("an", usingTask.AssemblyName);
            Assert.Equal("c", usingTask.Condition);
            Assert.Equal("AssemblyFactory", usingTask.TaskFactory);
        }

        /// <summary>
        /// Verify that passing in string.empty or null for TaskFactory will remove the element from the xml.
        /// </summary>
        [TestMethod]
        public void RemoveUsingTaskFactoryRuntimeAndPlatform()
        {
            ProjectUsingTaskElement usingTask = GetUsingTaskFactoryRuntimeAndPlatform();

            string value = null;
            VerifyAttributesRemoved(usingTask, value);

            usingTask = GetUsingTaskFactoryRuntimeAndPlatform();
            value = String.Empty;
            VerifyAttributesRemoved(usingTask, value);
        }

        /// <summary>
        /// Set assembly file on a usingtask that already has assembly file
        /// </summary>
        [TestMethod]
        public void SetUsingTaskAssemblyFileOnUsingTaskAssemblyFile()
        {
            ProjectUsingTaskElement usingTask = GetUsingTaskAssemblyFile();
            Helpers.ClearDirtyFlag(usingTask.ContainingProject);

            usingTask.AssemblyFile = "afb";
            Assert.Equal("afb", usingTask.AssemblyFile);
            Assert.True(usingTask.ContainingProject.HasUnsavedChanges);
        }

        /// <summary>
        /// Set assembly name on a usingtask that already has assembly name
        /// </summary>
        [TestMethod]
        public void SetUsingTaskAssemblyNameOnUsingTaskAssemblyName()
        {
            ProjectUsingTaskElement usingTask = GetUsingTaskAssemblyName();
            Helpers.ClearDirtyFlag(usingTask.ContainingProject);

            usingTask.AssemblyName = "anb";
            Assert.Equal("anb", usingTask.AssemblyName);
            Assert.True(usingTask.ContainingProject.HasUnsavedChanges);
        }

        /// <summary>
        /// Set assembly file on a usingtask that already has assembly name
        /// </summary>
        [TestMethod]
        public void SetUsingTaskAssemblyFileOnUsingTaskAssemblyName()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                ProjectUsingTaskElement usingTask = GetUsingTaskAssemblyName();

                usingTask.AssemblyFile = "afb";
            });
        }
        /// <summary>
        /// Set assembly name on a usingtask that already has assembly file
        /// </summary>
        [TestMethod]
        public void SetUsingTaskAssemblyNameOnUsingTaskAssemblyFile()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                ProjectUsingTaskElement usingTask = GetUsingTaskAssemblyFile();

                usingTask.AssemblyName = "anb";
            });
        }
        /// <summary>
        /// Set task name
        /// </summary>
        [TestMethod]
        public void SetTaskName()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectUsingTaskElement usingTask = project.AddUsingTask("t", "af", null);
            Helpers.ClearDirtyFlag(usingTask.ContainingProject);

            usingTask.TaskName = "tt";
            Assert.Equal("tt", usingTask.TaskName);
            Assert.True(usingTask.ContainingProject.HasUnsavedChanges);
        }

        /// <summary>
        /// Set condition
        /// </summary>
        [TestMethod]
        public void SetCondition()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectUsingTaskElement usingTask = project.AddUsingTask("t", "af", null);
            Helpers.ClearDirtyFlag(usingTask.ContainingProject);

            usingTask.Condition = "c";
            Assert.Equal("c", usingTask.Condition);
            Assert.True(usingTask.ContainingProject.HasUnsavedChanges);
        }

        /// <summary>
        /// Set task factory
        /// </summary>
        [TestMethod]
        public void SetTaskFactory()
        {
            ProjectRootElement project = ProjectRootElement.Create();
            ProjectUsingTaskElement usingTask = project.AddUsingTask("t", "af", null);
            Helpers.ClearDirtyFlag(usingTask.ContainingProject);

            usingTask.TaskFactory = "AssemblyFactory";
            Assert.Equal("AssemblyFactory", usingTask.TaskFactory);
            Assert.True(usingTask.ContainingProject.HasUnsavedChanges);
        }

        /// <summary>
        /// Make sure there is an exception when there are multiple parameter groups in the using task tag.
        /// </summary>
        [TestMethod]
        public void DuplicateParameterGroup()
        {
            Assert.Throws<InvalidProjectFileException>(() =>
            {
                string content = @"
                    <Project>
                        <UsingTask TaskName='t2' AssemblyName='an' Condition='c' TaskFactory='AssemblyFactory'>
                            <ParameterGroup/>
                            <ParameterGroup/>
                        </UsingTask>
                    </Project>
                ";
                ProjectRootElement.Create(XmlReader.Create(new StringReader(content)));
                Assert.Fail();
            });
        }
        /// <summary>
        /// Make sure there is an exception when there are multiple task groups in the using task tag.
        /// </summary>
        [TestMethod]
        public void DuplicateTaskGroup()
        {
            Assert.Throws<InvalidProjectFileException>(() =>
            {
                string content = @"
                    <Project>
                        <UsingTask TaskName='t2' AssemblyName='an' Condition='c' TaskFactory='AssemblyFactory'>
                            <Task/>
                            <Task/>
                        </UsingTask>
                    </Project>
                ";
                ProjectRootElement.Create(XmlReader.Create(new StringReader(content)));
                Assert.Fail();
            });
        }
        /// <summary>
        /// Make sure there is an exception when there is an unknown child
        /// </summary>
        [TestMethod]
        public void UnknownChild()
        {
            Assert.Throws<InvalidProjectFileException>(() =>
            {
                string content = @"
                    <Project>
                        <UsingTask TaskName='t2' AssemblyName='an' Condition='c' TaskFactory='AssemblyFactory'>
                            <IAMUNKNOWN/>
                        </UsingTask>
                    </Project>
                ";
                ProjectRootElement.Create(XmlReader.Create(new StringReader(content)));
                Assert.Fail();
            });
        }
        /// <summary>
        /// Make sure there is an no exception when there are children in the using task
        /// </summary>
        [TestMethod]
        public void WorksWithChildren()
        {
            string content = @"
                    <Project>
                        <UsingTask TaskName='t2' AssemblyName='an' Condition='c' TaskFactory='AssemblyFactory'>
                            <ParameterGroup>
                               <MyParameter/>
                            </ParameterGroup>
                            <Task>
                                RANDOM GOO
                            </Task>
                        </UsingTask>
                    </Project>
                ";

            using ProjectRootElementFromString projectRootElementFromString = new(content);
            ProjectRootElement project = projectRootElementFromString.Project;
            ProjectUsingTaskElement usingTask = (ProjectUsingTaskElement)Helpers.GetFirst(project.Children);
            Assert.NotNull(usingTask);
            Assert.Equal(2, usingTask.Count);
        }

        /// <summary>
        /// Make sure there is an exception when a parameter group is added but no task factory attribute is on the using task
        /// </summary>
        [TestMethod]
        public void ExceptionWhenNoTaskFactoryAndHavePG()
        {
            Assert.Throws<InvalidProjectFileException>(() =>
            {
                string content = @"
                    <Project>
                        <UsingTask TaskName='t2' AssemblyName='an' Condition='c'>
                            <ParameterGroup>
                               <MyParameter/>
                            </ParameterGroup>
                        </UsingTask>
                    </Project>
                ";

                ProjectRootElement project = ProjectRootElement.Create(XmlReader.Create(new StringReader(content)));
                Helpers.GetFirst(project.Children);
                Assert.Fail();
            });
        }
        /// <summary>
        /// Make sure there is an exception when a parameter group is added but no task factory attribute is on the using task
        /// </summary>
        [TestMethod]
        public void ExceptionWhenNoTaskFactoryAndHaveTask()
        {
            Assert.Throws<InvalidProjectFileException>(() =>
            {
                string content = @"
                    <Project>
                        <UsingTask TaskName='t2' AssemblyName='an' Condition='c'>
                            <Task/>
                        </UsingTask>
                    </Project>
                ";

                ProjectRootElement project = ProjectRootElement.Create(XmlReader.Create(new StringReader(content)));
                Helpers.GetFirst(project.Children);
                Assert.Fail();
            });
        }
        /// <summary>
        /// Helper to get a ProjectUsingTaskElement with a task factory, required runtime and required platform
        /// </summary>
        private static ProjectUsingTaskElement GetUsingTaskFactoryRuntimeAndPlatform()
        {
            string content = @"
                    <Project>
                        <UsingTask TaskName='t2' AssemblyName='an' Condition='c' TaskFactory='AssemblyFactory' />
                    </Project>
                ";

            using ProjectRootElementFromString projectRootElementFromString = new(content);
            ProjectRootElement project = projectRootElementFromString.Project;
            ProjectUsingTaskElement usingTask = (ProjectUsingTaskElement)Helpers.GetFirst(project.Children);
            return usingTask;
        }

        /// <summary>
        /// Helper to get a ProjectUsingTaskElement with an assembly file set
        /// </summary>
        private static ProjectUsingTaskElement GetUsingTaskAssemblyFile()
        {
            string content = @"
                    <Project>
                        <UsingTask TaskName='t1' AssemblyFile='af' />
                        <UsingTask TaskName='t2' AssemblyName='an' Condition='c'/>
                    </Project>
                ";

            using ProjectRootElementFromString projectRootElementFromString = new(content);
            ProjectRootElement project = projectRootElementFromString.Project;
            ProjectUsingTaskElement usingTask = (ProjectUsingTaskElement)Helpers.GetFirst(project.Children);
            return usingTask;
        }

        /// <summary>
        /// Helper to get a ProjectUsingTaskElement with an assembly name set
        /// </summary>
        private static ProjectUsingTaskElement GetUsingTaskAssemblyName()
        {
            string content = @"
                    <Project>
                        <UsingTask TaskName='t2' AssemblyName='an' Condition='c'/>
                    </Project>
                ";

            using ProjectRootElementFromString projectRootElementFromString = new(content);
            ProjectRootElement project = projectRootElementFromString.Project;
            ProjectUsingTaskElement usingTask = (ProjectUsingTaskElement)Helpers.GetFirst(project.Children);
            return usingTask;
        }

        /// <summary>
        /// Verify the attributes are removed from the xml when string.empty and null are passed in
        /// </summary>
        private static void VerifyAttributesRemoved(ProjectUsingTaskElement usingTask, string value)
        {
            Assert.Contains("TaskFactory", usingTask.ContainingProject.RawXml);
            usingTask.TaskFactory = value;
            Assert.DoesNotContain("TaskFactory", usingTask.ContainingProject.RawXml);
        }
    }
}
