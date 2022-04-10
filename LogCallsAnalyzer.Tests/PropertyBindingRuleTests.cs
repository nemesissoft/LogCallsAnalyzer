using System.Linq;
using NUnit.Framework;
using System.Threading.Tasks;

using VerifyCs = LogCallsAnalyzer.Tests.Verifiers.CSharpAnalyzerVerifier<LogCallsAnalyzer.SerilogAnalyzer>;
using static LogCallsAnalyzer.Tests.Helpers.SourceBuilder;

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Testing;


namespace LogCallsAnalyzer.Tests
{
    [TestFixture]
    public class PropertyBindingRuleTests
    {
        [Test]
        public async Task EmptyMessageTemplate_Positive()
        {
            var source = BuildTestSource(@"InfoFormat(""Hello, NOTHING!"")");
            await VerifyCs.VerifyAnalyzerAsync(source);
        }

        private static IEnumerable<int> PropertiesCountData() => Enumerable.Range(1, 5).ToList();

        [TestCaseSource(nameof(PropertiesCountData))]
        public async Task MoreArgumentsThanProperties_Negative(int propertiesCount)
        {
            var properties = string.Join(" ", Enumerable.Range(1, propertiesCount).Select(i => $"{{Prop{i:00}}}"));

            for (int argsCount = propertiesCount + 1; argsCount <= propertiesCount + 4; argsCount++)
            {
                var args = string.Join(", " + NL, Enumerable.Range(1, argsCount)
                    .Select(i => $"\"Arg{i:00}\""));

                var source = BuildTestSource(@$"InfoFormat(""Hello, {properties}!"", {NL}{args})");

                var expectedDiagnostic = Enumerable.Range(propertiesCount + 1, argsCount - propertiesCount)
                    .Select(i =>
                        GetPropertyDiagnostic(10 + i - 1, 1, $"There is no named property that corresponds to argument '\"Arg{i:00}\"'")
                    ).ToArray();

                await VerifyCs.VerifyAnalyzerAsync(source, expectedDiagnostic);
            }
        }

        [Test]
        public async Task MoreArguments_EmptyTemplate_Negative()
        {
            var source = BuildTestSource(@"InfoFormat(System.String.Empty, ""Hello"")");

            await VerifyCs.VerifyAnalyzerAsync(source, GetPropertyDiagnostic(9, 57, "There is no property but 1 arguments are left to match"));
        }

        [Test]
        public async Task NoPropertiesButSomeArguments_Negative()
        {
            for (int argsCount = 1; argsCount <= 3; argsCount++)
            {
                var args = string.Join(", " + NL, Enumerable.Range(1, argsCount)
                    .Select(i => $"\"Arg{i:00}\""));

                var source = BuildTestSource(@$"InfoFormat(""Hello, NO DATA!"", {NL}{args})");

                var expectedDiagnostic = GetPropertyDiagnostic(10, 1, $"There is no property but {argsCount} arguments are left to match");
                await VerifyCs.VerifyAnalyzerAsync(source, expectedDiagnostic);
            }
        }

        [TestCaseSource(nameof(PropertiesCountData))]
        public async Task LessArgumentsThanProperties_Negative(int propertiesCount)
            => await LessArgumentsThanProperties("InfoFormat", propertiesCount);

        private static async Task LessArgumentsThanProperties(string methodName, int propertiesCount)
        {
            var properties = string.Join(" " + NL, Enumerable.Range(1, propertiesCount).Select(i => $"{{Prop{i:00}}}"));

            for (int argsCount = 0; argsCount < propertiesCount; argsCount++)
            {
                var args = string.Join(", ", Enumerable.Range(1, argsCount)
                    .Select(i => $"\"Arg{i:00}\""));

                var source = BuildTestSource(@$"{methodName}(@""Hello,{NL}{properties}!""{(argsCount > 0 ? ", " : "")} {args})");

                var expectedDiagnostic = Enumerable.Range(argsCount, propertiesCount - argsCount)
                    .Select(i =>
                        GetPropertyDiagnostic(10 + i, 1, $"There is no argument that corresponds to the named property 'Prop{(i + 1):00}'")
                    ).ToArray();

                await VerifyCs.VerifyAnalyzerAsync(source, expectedDiagnostic);
            }
        }

        [TestCaseSource(nameof(PropertiesCountData))]
        public async Task SameNumberOfArgumentsAsProperties_Positive(int propertiesCount)
        {
            var properties = string.Join(" ", Enumerable.Range(1, propertiesCount).Select(i => $"{{Prop{i:00}}}"));

            var args = string.Join(", ", Enumerable.Range(1, propertiesCount).Select(i => $"\"Arg{i:00}\""));

            var source = BuildTestSource(@$"InfoFormat(@""Hello,{properties}!"", {args})");

            await VerifyCs.VerifyAnalyzerAsync(source);
        }

        private static IEnumerable<string> FormatMethods() => new[] { "DebugFormat", "InfoFormat", "WarnFormat", "ErrorFormat", "FatalFormat" };

        [TestCaseSource(nameof(FormatMethods))]
        public async Task TestAllMethods_Negative(string methodName)
        {
            for (int i = 1; i < 5; i++)
                await LessArgumentsThanProperties(methodName, i);
        }

        [Test]
        public async Task MixedPositionalAndNamed_Negative()
        {
            var propertiesCount = 6;

            var properties = string.Join(" " + NL, Enumerable.Range(1, propertiesCount)
                .Select(i => i % 2 == 0 ? $"{{{i}}}" : $"{{Prop{i:00}}}"));

            var args = string.Join(", ", Enumerable.Range(1, propertiesCount).Select(i => $"\"Arg{i:00}\""));

            var source = BuildTestSource(@$"InfoFormat(@""Hello,{NL}{properties}!"", {args})");

            var expectedDiagnostic = GetPropertyDiagnostic(11, 1, "When named properties are being used, positional properties are not allowed: '2', '4', '6'");
            await VerifyCs.VerifyAnalyzerAsync(source, expectedDiagnostic);
        }

        [Test]
        public async Task TooManyPositionalProperties_Negative()
        {
            var source = BuildTestSource(@"InfoFormat(@""Hello,
{0,2}
{1,3}
{2,-5}
{3:X3}
!"", 11, 22, 33)");

            var expectedDiagnostic = GetPropertyDiagnostic(13, 1, "There is no argument that corresponds to the positional property '3'");

            await VerifyCs.VerifyAnalyzerAsync(source, expectedDiagnostic);
        }

        [Test]
        public async Task TooManyArgumentsForPositionalProperties_Negative()
        {
            var source = BuildTestSource(@"InfoFormat(@""Hello,
{0,2}
{1,3}
{2,-5}
{3:X3}
!"", 11, 22, 33, 44, 
55)");

            var expectedDiagnostic = GetPropertyDiagnostic(15, 1, "There is no positional property that corresponds to argument 55");

            await VerifyCs.VerifyAnalyzerAsync(source, expectedDiagnostic);
        }

        [Test]
        public async Task RepeatedPositionalProperties_Negative()
        {
            //This tests for an error in Serilog - repeated positional properties are formatted more or less correctly but it results in error written to self-log. This tests guard that behaviour and analyzer treat such templates as errors
            var source = BuildTestSource(@"InfoFormat(@""Evaluated as {2} [Sym={0} Prop={1}] Eval=
{2}"", ""Q"", ""W"", ""E"")");

            var expectedDiagnostic = GetPropertyDiagnostic(10, 1, "Serilog bug - repeated positional properties are not supported properly. Property: {2}");

            await VerifyCs.VerifyAnalyzerAsync(source, expectedDiagnostic);
        }

        [Test]
        public async Task PropertyAlignmentAndFormat_Positive()
        {
            var source = BuildTestSource(@"InfoFormat(@""Hello,
'{First}'
'{Second,5}'
'{Third,-5}'
'{Fourth:X3}'
!"", 8, 16, 32, 64)");

            await VerifyCs.VerifyAnalyzerAsync(source);
        }

        [Test]
        public async Task PositionalPropertyAlignmentAndFormat_Positive()
        {
            var source = BuildTestSource(@"InfoFormat(@""Hello,
'{0}'
'{1,5}'
'{2,-5}'
'{3:X3}'
!"", 8, 16, 32, 64)");

            await VerifyCs.VerifyAnalyzerAsync(source);
        }

        private static DiagnosticResult GetPropertyDiagnostic(int line, int column, params object[] arguments)
            => VerifyCs.Diagnostic(SerilogAnalyzer.PropertyBindingRule)
                .WithLocation(line, column)
                .WithArguments(arguments);




        [Test]
        public async Task AnonymousTypeWithoutDestructure_Negative()
        {
            var source = BuildTestSource(@"InfoFormat(""Hello World {Some}"", new { Meh = 42 })");

            await VerifyCs.VerifyAnalyzerAsync(source,
                VerifyCs.Diagnostic(SerilogAnalyzer.DestructureAnonymousObjectsRule)
                    .WithLocation(9, 49)
                    .WithArguments("Some")
                );



            source = BuildTestSource(@"InfoFormat(""Hello World {@Some}"", new { Meh = 42 })");
            await VerifyCs.VerifyAnalyzerAsync(source);
        }
        
        [Test]
        public async Task UniqueNames_Negative()
        {
            var source = BuildTestSource(@"InfoFormat(""{Tester} chats with {Tester}"", ""tester1"", ""tester2"")");

            await VerifyCs.VerifyAnalyzerAsync(source,
                VerifyCs.Diagnostic(SerilogAnalyzer.UniquePropertyNameRule)
                    .WithLocation(9, 57)
                    .WithArguments("Tester")
                );
        }
        
        [Test]
        public async Task PascalNames_Negative()
        {
            var source = BuildTestSource(@"InfoFormat(""{tester} chats with himself"", ""tester1"")");

            await VerifyCs.VerifyAnalyzerAsync(source,
                VerifyCs.Diagnostic(SerilogAnalyzer.PascalPropertyNameRule)
                    .WithLocation(9, 37)
                    .WithArguments("tester")
                );
        }
    }
}
