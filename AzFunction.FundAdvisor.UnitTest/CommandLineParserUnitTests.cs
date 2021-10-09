using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CommandLine;
using DotNet.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AzFunction.FundAdvisor.UnitTest
{
    [TestClass]
    public class CommandLineParserUnitTests
    {
        [TestMethod]
        public void TestSplitCommandLineIntoArguments()
        {
            string commandline = @"Get-FundInfo -filter ""{Id: '123'}""";
            IReadOnlyList<string> args = CommandLineExtensions.SplitCommandLineIntoArguments(commandline, true).ToList();
            Assert.AreEqual(args.Count(), 3);
            Assert.AreEqual(args[0], "Get-FundInfo");
            Assert.AreEqual(args[1], "-filter");
            Assert.AreEqual(args[2], "{Id: '123'}");
        }

        [TestMethod]
        public void TestCommandLineParser()
        {
            StringBuilder sb = new StringBuilder();
            TextWriter parserOutputWriter = new StringWriter(sb);
            Parser parser = new Parser(settings => settings.HelpWriter = parserOutputWriter);

            string commandline = "Test-Command";
            IReadOnlyList<string> args = CommandLineExtensions.SplitCommandLineIntoArguments(commandline, true).ToList();
            parser.ParseArguments<MockOptions>(args).MapResult(
                (MockOptions o) =>
                {
                    Assert.AreEqual("{}", o.Filter);
                    return 0;
                },
                errors => 1);
            Assert.IsTrue(string.IsNullOrEmpty(parserOutputWriter.ToString()));
            sb.Clear();

            commandline = "Test-Command --Filter \"{Id: '12345'}\"";
            args = CommandLineExtensions.SplitCommandLineIntoArguments(commandline, true).ToList();
            parser.ParseArguments<MockOptions>(args).MapResult(
                (MockOptions o) =>
                {
                    Assert.AreEqual("{Id: '12345'}", o.Filter);
                    return 0;
                },
                errors => 1);
            Assert.IsTrue(string.IsNullOrEmpty(parserOutputWriter.ToString()));
            sb.Clear();

            commandline = "Test-Command --filter";
            args = CommandLineExtensions.SplitCommandLineIntoArguments(commandline, true).ToList();
            parser.ParseArguments<MockOptions>(args).MapResult(
                (MockOptions o) => 0,
                errors => 1);
            string commandHelp = parserOutputWriter.ToString();
            Assert.IsTrue(!string.IsNullOrEmpty(parserOutputWriter.ToString()));
        }


        [Verb("Test-Command")]
        internal class MockOptions
        {
            [Option("Filter")]
            public string Filter { get; set; } = "{}";
        }
    }
}
