using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

using VerifyCs = LogCallsAnalyzer.Tests.Verifiers.CSharpCodeFixVerifier<LogCallsAnalyzer.SerilogAnalyzer, LogCallsAnalyzer.CodeFix.PascalCaseCodeFixProvider>;
using static LogCallsAnalyzer.Tests.Helpers.SourceBuilder;

namespace LogCallsAnalyzer.Tests
{
    [TestFixture]
    public class PascalCaseCodeFixTests
    {
        private static readonly DiagnosticDescriptor _pascalPropertyNameRule = SerilogAnalyzer.PascalPropertyNameRule;

        [Test]
        public async Task TestPascalCaseFixForString()
        {
            string src = BuildTestSource(@"InfoFormat(""Hello, {property}!"", 123)");

            var expectedDiagnostic =
                VerifyCs.Diagnostic(_pascalPropertyNameRule)
                    .WithLocation(9, 44)
                    .WithArguments("property");


            var fix = src.Replace("{property}", "{Property}");

            await VerifyCs.VerifyCodeFixAsync(src, expectedDiagnostic, fix);
        }

        [Test]
        public async Task TestPascalCaseFixForStringWithException()
        {
            string src = BuildTestSource(@"FatalFormat(""{DwgFileName} Crashed and burned. {stackTrace}"", ""filename.dwg"", new System.Exception("""").StackTrace)");

            var expectedDiagnostic =
                VerifyCs.Diagnostic(_pascalPropertyNameRule)
                    .WithLocation(9, 72)
                    .WithArguments("stackTrace");


            var fix = src.Replace("{stackTrace}", "{StackTrace}");

            await VerifyCs.VerifyCodeFixAsync(src, expectedDiagnostic, fix);
        }

        [Test]
        public async Task TestPascalCaseFixForSnakeCaseString()
        {
            string src = BuildTestSource(@"WarnFormat(""Hello {tester_name}"", ""Mike"")");

            var expectedDiagnostic =
                VerifyCs.Diagnostic(_pascalPropertyNameRule)
                    .WithLocation(9, 43)
                    .WithArguments("tester_name");


            var fix = src.Replace("{tester_name}", "{TesterName}");

            await VerifyCs.VerifyCodeFixAsync(src, expectedDiagnostic, fix);
        }

        [Test]
        public async Task TestPascalCaseFixForStringWithEscapes()
        {
            string src = BuildTestSource(@"WarnFormat(""Hello \""{tester}\"""", ""Mike"")");

            var expectedDiagnostic =
                VerifyCs.Diagnostic(_pascalPropertyNameRule)
                    .WithLocation(9, 45)
                    .WithArguments("tester");


            var fix = src.Replace("{tester}", "{Tester}");

            await VerifyCs.VerifyCodeFixAsync(src, expectedDiagnostic, fix);
        }

        [Test]
        public async Task TestPascalCaseFixForStringWithVerbatimEscapes()
        {
            string src = BuildTestSource(@"WarnFormat(@""Hello """"{tester}"""""", ""Mike"")");

            var expectedDiagnostic =
                VerifyCs.Diagnostic(_pascalPropertyNameRule)
                    .WithLocation(9, 46)
                    .WithArguments("tester");


            var fix = src.Replace("{tester}", "{Tester}");

            await VerifyCs.VerifyCodeFixAsync(src, expectedDiagnostic, fix);
        }

        [Test]
        public async Task TestPascalCaseFixForStringification()
        {
            string src = BuildTestSource(@"WarnFormat(""Hello {|#0:{$tester}|}"", ""Mike"")");

            var expectedDiagnostic =
                VerifyCs.Diagnostic(_pascalPropertyNameRule)
                    .WithLocation(0)
                    .WithArguments("tester");


            var fix = src.Replace("{$tester}", "{$Tester}");

            await VerifyCs.VerifyCodeFixAsync(src, expectedDiagnostic, fix);
        }
    }
}