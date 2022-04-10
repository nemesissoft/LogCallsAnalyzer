using NUnit.Framework;
using System.Threading.Tasks;
using VerifyCs = LogCallsAnalyzer.Tests.Verifiers.CSharpAnalyzerVerifier<LogCallsAnalyzer.SerilogAnalyzer>;
using static LogCallsAnalyzer.Tests.Helpers.SourceBuilder;
using Microsoft.CodeAnalysis.Testing;

namespace LogCallsAnalyzer.Tests
{
    [TestFixture]
    public class ConstantMessageTemplateRuleTests
    {
        [TestCase("InfoFormat", @"System.String.Format(""Hello {0}"", ""World"")", 10, 12, @"System.String.Format(""Hello {0}"", ""World"")")]
        [TestCase("DebugFormat", @"System.String.Format(""Hello {0} to {1}"", ""Name"", ""World"")", 10, 13, @"System.String.Format(""Hello {0} to {1}"", ""Name"", ""World"")")]
        [TestCase("WarnFormat", @"$""Hello {""World"".ToString()}""", 10, 12, @"$""Hello {""World"".ToString()}""")]
        [TestCase("ErrorFormat", @"System.String.Format(""Hello {0} to {1}"", ""World"")", 10, 13, @"System.String.Format(""Hello {0} to {1}"", ""World"")")]
        public async Task NonConstantTemplates_Negative(string methodName, string arguments, int line, int column, string expectedMessage)
        {
            var source = BuildTestSource($"{NL}{methodName}({arguments})");
            await VerifyCs.VerifyAnalyzerAsync(source, GetConstantDiagnostic(line, column, expectedMessage));


            source = BuildTestSource($"{NL}{methodName}((IClientRequestInfo) null, {arguments})", LOG_HELPER_SOURCE);
            await VerifyCs.VerifyAnalyzerAsync(source, GetConstantDiagnostic(line, column + "(IClientRequestInfo) null, ".Length, expectedMessage));

            source = BuildTestSourceExtension(methodName, $"(IClientRequestInfo) null, {arguments}");
            await VerifyCs.VerifyAnalyzerAsync(source, GetConstantDiagnostic(line, column + "(abstraction, (IClientRequestInfo) null,".Length, expectedMessage));
        }

        [Test]
        public async Task CallToMethod_Negative()
        {
            var source = $@"{IMPORTS}
namespace Tester
{{
    class LogTester
    {{
        static void Start()
        {{
            {CREATE_LOG}
            abstraction.ErrorFormat(TryToCheckOutOrder());
        }}

        public static string TryToCheckOutOrder() => ""Something bad happened"";
    }}
}}";
            await VerifyCs.VerifyAnalyzerAsync(source, GetConstantDiagnostic(9, 37, "TryToCheckOutOrder()"));
        }

        [Test]
        public async Task UsingStatic_Negative()
        {
            var source = $@"{IMPORTS}
using static System.String;
namespace Tester
{{
    class LogTester
    {{
        static void Start()
        {{
            {CREATE_LOG}
            abstraction.DebugFormat(Format(""Hello {{0}}"", ""World""));
        }}        
    }}
}}";
            await VerifyCs.VerifyAnalyzerAsync(source, GetConstantDiagnostic(10, 37, @"Format(""Hello {0}"", ""World"")"));
        }

        [Test]
        public async Task LocalVariableConcat_Negative()
        {
            var source = $@"{IMPORTS}
namespace Tester
{{
    class LogTester
    {{
        static void Start()
        {{
            {CREATE_LOG}
            var name = ""ABC"";
            abstraction.FatalFormat(""Hello World\nName: '"" + name + @""'"");
        }}        
    }}
}}";
            await VerifyCs.VerifyAnalyzerAsync(source, GetConstantDiagnostic(10, 37, @"""Hello World\nName: '"" + name + @""'"""));
        }

        [Test]
        public async Task ComplexLocals_Negative()
        {
            var source = $@"{IMPORTS}
namespace Tester
{{
    class LogTester
    {{
        static void Start()
        {{
            {CREATE_LOG}
            bool test = true; string name = ""Tester"";
            abstraction.FatalFormat(""Hello "" + name + "" to the {{Place}} "" + (test ? "" yes"" + "" no"" : "" no"" + "" yes"") + "" text"", ""party"");
        }}        
    }}
}}";
            await VerifyCs.VerifyAnalyzerAsync(source, GetConstantDiagnostic(10, 37, @"""Hello "" + name + "" to the {Place} "" + (test ? "" yes"" + "" no"" : "" no"" + "" yes"") + "" text"""));
        }

        [Test]
        public async Task CallToConst_Positive()
        {
            var source = $@"{IMPORTS}
namespace Tester
{{
    class LogTester
    {{
        static void Start()
        {{
            {CREATE_LOG}
            abstraction.FatalFormat(User.TEMPLATE, 123);
        }}        
    }}
    class User
    {{
        public const string TEMPLATE = ""Hello {{Property}}"";
    }}
}}";
            await VerifyCs.VerifyAnalyzerAsync(source);
        }

        [Test]
        public async Task CallToReadonly_Negative()
        {
            var source = $@"{IMPORTS}
namespace Tester
{{
    class LogTester
    {{
        static void Start()
        {{
            {CREATE_LOG}
            abstraction.FatalFormat(User.TEMPLATE, 123);
        }}        
    }}
    class User
    {{
        public static readonly string TEMPLATE = ""Hello {{Property}}"";
    }}
}}";
            await VerifyCs.VerifyAnalyzerAsync(source, GetConstantDiagnostic(9, 37, "User.TEMPLATE"));
        }

        [Test]
        public async Task SimpleLiteral_Positive()
        {
            var source = BuildTestSource(@"Fatal(123)");
            await VerifyCs.VerifyAnalyzerAsync(source);
        }


        private static DiagnosticResult GetConstantDiagnostic(int line, int column, params object[] arguments)
            => VerifyCs.Diagnostic(SerilogAnalyzer.ConstantMessageTemplateRule).WithLocation(line, column).WithArguments(arguments);
    }
}
