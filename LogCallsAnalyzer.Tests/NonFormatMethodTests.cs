using System.Linq;
using NUnit.Framework;
using System.Threading.Tasks;

using VerifyCs = LogCallsAnalyzer.Tests.Verifiers.CSharpAnalyzerVerifier<LogCallsAnalyzer.SerilogAnalyzer>;
using static LogCallsAnalyzer.Tests.Helpers.SourceBuilder;

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace LogCallsAnalyzer.Tests
{
    public class NonFormatMethodTests
    {
        private static IEnumerable<string> NonFormatMethods() => new[] { "Debug", "Info", "Warn", "Error", "Fatal" };
        private static readonly DiagnosticDescriptor _nonFormatNoTemplateRule = SerilogAnalyzer.NonFormatNoTemplateRule;
        private static readonly DiagnosticDescriptor _nonFormatComplexMessageRule = SerilogAnalyzer.NonFormatComplexMessageRule;


        [TestCaseSource(nameof(NonFormatMethods))]
        public async Task TemplatePlaceholdersInMessage_ShouldReportDiagnostics(string methodName)
        {
            var properties = string.Join(" ", Enumerable.Range(1, 3).Select(i => $"{{Prop{i:00}}}"));

            var source = BuildTestSource(@$"{NL}{methodName}(""Hello, {properties}!"")");



            var expectedDiagnostic =
                VerifyCs.Diagnostic(_nonFormatNoTemplateRule)
                .WithLocation(10, 1)
                .WithArguments(methodName);

            await VerifyCs.VerifyAnalyzerAsync(source, expectedDiagnostic);


            //Test exception variant
            source = BuildTestSource(@$"{NL}{methodName}(""Hello, {properties}!"", new System.Exception(""BUM""))");
            await VerifyCs.VerifyAnalyzerAsync(source, expectedDiagnostic);
        }

        [TestCaseSource(nameof(NonFormatMethods))]
        public async Task NonConstantMessage_ShouldReportDiagnostics(string methodName)
        {
            var source = BuildTestSource(@$"{NL}{methodName}(""Value: "" + ""some value"")");

            var expectedDiagnostic =
                VerifyCs.Diagnostic(_nonFormatComplexMessageRule)
                .WithLocation(10, 1)
                .WithArguments(methodName);

            await VerifyCs.VerifyAnalyzerAsync(source, expectedDiagnostic);


            //Test exception variant
            source = BuildTestSource(@$"{NL}{methodName}(""Value: "" + ""some value"", new System.Exception(""BUM""))");
            await VerifyCs.VerifyAnalyzerAsync(source, expectedDiagnostic);
        }



        [TestCaseSource(nameof(NonFormatMethods))]
        public async Task TemplatePlaceholdersInMessage_Extension_ShouldReportDiagnostics(string methodName)
        {
            var properties = string.Join(" ", Enumerable.Range(1, 3).Select(i => $"{{Prop{i:00}}}"));

            var source = BuildTestSource(@$"{NL}{methodName}((IClientRequestInfo) null, ""Hello, {properties}!"")", LOG_HELPER_SOURCE);

            var expectedDiagnostic =
                VerifyCs.Diagnostic(_nonFormatNoTemplateRule)
                    .WithLocation(10, 1)
                    .WithArguments(methodName);

            await VerifyCs.VerifyAnalyzerAsync(source, expectedDiagnostic);


            //Test static method call
            source = BuildTestSourceExtension(methodName, @$"(IClientRequestInfo) null, ""Hello, {properties}!""");
            await VerifyCs.VerifyAnalyzerAsync(source, expectedDiagnostic);
        }

        [TestCaseSource(nameof(NonFormatMethods))]
        public async Task NonConstantMessage_Extension_ShouldReportDiagnostics(string methodName)
        {
            var source = BuildTestSource(@$"{NL}{methodName}((IClientRequestInfo) null, ""Value: "" + ""some value"")", LOG_HELPER_SOURCE);

            var expectedDiagnostic =
                VerifyCs.Diagnostic(_nonFormatComplexMessageRule)
                    .WithLocation(10, 1)
                    .WithArguments(methodName);

            await VerifyCs.VerifyAnalyzerAsync(source, expectedDiagnostic);


            //Test static method call
            source = BuildTestSourceExtension(methodName, @"(IClientRequestInfo) null, ""Value: "" + ""some value""");
            await VerifyCs.VerifyAnalyzerAsync(source, expectedDiagnostic);
        }
    }
}
