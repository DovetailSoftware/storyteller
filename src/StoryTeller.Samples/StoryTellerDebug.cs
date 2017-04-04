using NUnit.Framework;
using StoryTeller.Execution;
using StoryTeller.Engine;

namespace StoryTellerTestHarness
{
    [TestFixture, Explicit]
    public class Template
    {
        private ProjectTestRunner runner;

        [OneTimeSetUp]
        public void SetupRunner()
        {
            runner = new ProjectTestRunner(@"..\..\..\..\samples\grammars.xml");
        }

        [Test]
        public void Tables()
        {
            var test = runner.RunTest("Tables/Tables");
        
            runner.WritePreview(test).OpenInBrowser();
        }

        [OneTimeTearDown]
        public void TeardownRunner()
        {
            runner.Dispose();
        }
    }
}