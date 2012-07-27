using System;
using System.IO;
using DirectionFixtures;
using FubuCore.Conversion;
using NUnit.Framework;
using StateFixtures;
using StoryTeller.Domain;
using StoryTeller.Engine;
using StoryTeller.Persistence;
using StoryTeller.Samples;
using StoryTeller.Workspace;
using System.Linq;
using StructureMap;
using TestContext = StoryTeller.Engine.TestContext;

namespace StoryTeller.Testing.Workspace
{


    [TestFixture]
    public class ProjectTester
    {
        #region Setup/Teardown

        [SetUp]
        public void SetUp()
        {
        }

        #endregion

        [Test]
        public void create_a_directory()
        {
            var project = new Project
            {
                BinaryFolder = string.Empty,
                ProjectFolder = "",
                TestFolder = ""
            };


            if (Directory.Exists("NewSuite")) Directory.Delete("NewSuite", true);
            var suite = new Suite("NewSuite");

            project.CreateDirectory(suite);

            Directory.Exists("NewSuite").ShouldBeTrue();

            var childSuite = new Suite("Child");
            suite.AddSuite(childSuite);

            project.CreateDirectory(childSuite);

            Directory.Exists("NewSuite\\Child").ShouldBeTrue();
        }

        [Test]
        public void delete_a_test_file()
        {
            var project = new Project
            {
                BinaryFolder = string.Empty,
                ProjectFolder = "",
                TestFolder = ""
            };
            var test = new Test("test to be saved");
            test.AddComment("some comment");
            test.FileName = "Test001.xml";

            project.Save(test);

            File.Exists("Test001.xml").ShouldBeTrue();
            project.DeleteFile(test);
            File.Exists("Test001.xml").ShouldBeFalse();
        }


        [Test]
        public void get_test_path()
        {
            var project = new Project(@"c:\a\b\c\d\project.proj")
            {
                TestFolder = "tests"
            };
            Hierarchy hierarchy = DataMother.BuildHierarchy(@"
t1,Success
s1/t2,Success
s1/s2/t3,Success
");


            Test test = hierarchy.FindTest("t1");

            project.GetTestPath(test).ShouldEqual(@"c:\a\b\c\d\tests\t1.xml");
            project.GetTestPath(hierarchy.FindTest("s1/t2")).ShouldEqual(@"c:\a\b\c\d\tests\s1\t2.xml");
            project.GetTestPath(hierarchy.FindTest("s1/s2/t3")).ShouldEqual(@"c:\a\b\c\d\tests\s1\s2\t3.xml");
        }

        [Test]
        public void get_test_path_when_the_test_overrides_the_file_name()
        {
            var project = new Project(@"c:\a\b\c\d\project.proj")
            {
                TestFolder = "tests"
            };
            Hierarchy hierarchy = DataMother.BuildHierarchy(@"
t1,Success
s1/t2,Success
s1/s2/t3,Success
");

            Test test = hierarchy.FindTest("t1");
            test.FileName = "TheBigTest.xml";

            project.GetTestPath(test).ShouldEqual(@"c:\a\b\c\d\tests\TheBigTest.xml");
        }

        [Test]
        public void get_the_test_path_of_a_test_at_the_hierarchy_scope()
        {
            var project = new Project(@"c:\a\b\c\d\project.proj")
            {
                TestFolder = "tests"
            };
            var hierarchy = new Hierarchy("something");
            var test = new Test("t0");
            hierarchy.AddTest(test);
            project.GetTestPath(test).ShouldEqual(@"c:\a\b\c\d\tests\t0.xml");
        }

        [Test]
        public void GetBaseFolder()
        {
            var project = new Project(@"c:\a\b\c\d\project.proj");
            Assert.AreEqual(@"c:\a\b\c\d", project.GetBaseProjectFolder());
        }

        [Test]
        public void GetBaseFolderReturnsEmptyStringIfNoFileNameIsSet()
        {
            var project = new Project();
            Assert.AreEqual(string.Empty, project.GetBaseProjectFolder());
        }

        [Test]
        public void save_and_load_a_test()
        {
            var project = new Project
            {
                BinaryFolder = string.Empty,
                ProjectFolder = "",
                TestFolder = ""
            };
            var test = new Test("test to be saved");
            test.AddComment("some comment");
            test.FileName = "Test001.xml";

            project.Save(test);

            Test test2 = new TestReader().ReadFromFile(test.FileName);
            test2.Name.ShouldEqual(test.Name);
            test2.Parts.Count.ShouldEqual(1);

            test2.FileName.ShouldEqual("Test001.xml");
        }
    }

    [TestFixture]
    public class when_renaming_a_file
    {
        #region Setup/Teardown

        [SetUp]
        public void SetUp()
        {
            var project = new Project
            {
                BinaryFolder = string.Empty,
                ProjectFolder = "",
                TestFolder = ""
            };
            test = new Test("Test001");
            test.AddComment("some comment");

            project.Save(test);

            project.RenameTest(test, "New Name");
        }

        #endregion

        private Test test;

        [Test]
        public void the_new_file_name_should_reflect_the_new_name()
        {
            test.FileName.ShouldEqual("New_Name.xml");
        }

        [Test]
        public void the_new_test_name_should_be_set()
        {
            test.Name.ShouldEqual("New_Name");
        }

        [Test]
        public void the_old_file_should_be_deleted()
        {
            File.Exists("Test001.xml").ShouldBeFalse();
        }

        [Test]
        public void the_test_should_now_be_saved_at_the_new_file_location()
        {
            File.Exists("New_Name.xml");

            Test test2 = new TestReader().ReadFromFile("New_Name.xml");
            test2.Parts[0].ShouldBeOfType<Comment>().Text.ShouldEqual("some comment");
        }

        [Test]
        public void get_workspace_for_a_name_returns_the_same()
        {
            var project = new Project();
            project.WorkspaceFor("Main").ShouldBeTheSameAs(project.WorkspaceFor("Main"));
        }

        
    }

    
    [TestFixture]
    public class when_creating_the_combined_workflow_filter_for_selected_workspaces
    {
        private Project project;

        [SetUp]
        public void SetUp()
        {
            project = new Project();
            project.WorkspaceFor("States").AddFilter(new FixtureFilter()
            {
                Name = "StateFixtures",
                Type = FilterType.Namespace
            });


            project.WorkspaceFor("North").AddFilter(new FixtureFilter()
            {
                Name = "North",
                Type = FilterType.Fixture
            });

            project.WorkspaceFor("South").AddFilter(new FixtureFilter()
            {
                Name = "South",
                Type = FilterType.Fixture
            });


        }

        private void fixturesShouldBe(params string[] names)
        {
            var container = new Container();
            var registry = new FixtureRegistry();
            registry.AddFixture<OhioFixture>();
            registry.AddFixture<WisconsinFixture>();
            registry.AddFixture<IllinoisFixture>();
            registry.AddFixture<NorthFixture>();
            registry.AddFixture<SouthFixture>();

            registry.AddFixturesToContainer(container);

            var builder = new LibraryBuilder(new NulloFixtureObserver(),
                                             project.CurrentFixtureFilter().CreateTypeFilter(), new ObjectConverter());
            var library = builder.Build(new TestContext(container));

            Array.Sort(names);

            library.ActiveFixtures.Select(x => x.Name).ShouldHaveTheSameElementsAs(names);
        }

        [Test]
        public void no_workspaces_selected_so_all_fixtures_should_be_available()
        {
            fixturesShouldBe("Ohio", "Wisconsin", "Illinois", "North", "South");
        }

        [Test]
        public void select_a_single_workspace()
        {
            project.SelectWorkspaces(new string[] {"States"});
            fixturesShouldBe("Ohio", "Wisconsin", "Illinois");

            project.SelectWorkspaces(new string[]{"North"});
            fixturesShouldBe("North");
        }


        [Test]
        public void clear_workspace_filter_sets_back_to_no_filters()
        {
            project.SelectWorkspaces(new string[] { "States" });
            project.SelectWorkspaces(new string[0]);

            fixturesShouldBe("Ohio", "Wisconsin", "Illinois", "North", "South");
        }

        [Test]
        public void should_use_a_union_of_the_selected_workspace_filters()
        {
            project.SelectWorkspaces(new string[]{"States", "North"});

            fixturesShouldBe("Ohio", "Wisconsin", "Illinois", "North");
        }
    }

}

namespace StateFixtures
{
    public class OhioFixture : Fixture{}
    public class WisconsinFixture : Fixture{}
    public class IllinoisFixture : Fixture{}
}

namespace DirectionFixtures
{
    public class NorthFixture : Fixture{}
    public class SouthFixture : Fixture{}
}