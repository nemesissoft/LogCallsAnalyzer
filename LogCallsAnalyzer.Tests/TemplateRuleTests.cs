using NUnit.Framework;
using System.Threading.Tasks;
using VerifyCs = LogCallsAnalyzer.Tests.Verifiers.CSharpAnalyzerVerifier<LogCallsAnalyzer.SerilogAnalyzer>;
using static LogCallsAnalyzer.Tests.Helpers.SourceBuilder;
using Microsoft.CodeAnalysis.Testing;

namespace LogCallsAnalyzer.Tests
{
    [TestFixture]
    public class TemplateRuleTests
    {
        [TestCase("InfoFormat")]
        public async Task InvalidPropertyNames_Negative(string methodName)
        {
            var source = BuildTestSource($@"{methodName}(@""Hello {{&}} the World"", 8)");
            await VerifyCs.VerifyAnalyzerAsync(source,
                GetTemplateDiagnostic(9, 45, "Found invalid character '&' in property name"));


            source = BuildTestSource(@$"{methodName}(@""Hello,
'{{-1}}'
'{{<,5}}'
'{{(,-5}}'
'{{[:X3}}'
!"", 8, 16, 32, 64)");

            var expectedDiagnostic = new[]
            {
                GetTemplateDiagnostic(10, 3, "Found invalid character '-' in property name"),
                GetTemplateDiagnostic(11, 3, "Found invalid character '<' in property"),
                GetTemplateDiagnostic(12, 3, "Found invalid character '(' in property name"),
                GetTemplateDiagnostic(13, 3, "Found invalid character '[' in property name"),
            };

            await VerifyCs.VerifyAnalyzerAsync(source, expectedDiagnostic);


            //Extension
            source = BuildTestSource(@$"{methodName}((IClientRequestInfo) null, ""Hello {{&}} the World"", 8)", LOG_HELPER_SOURCE);
            await VerifyCs.VerifyAnalyzerAsync(source, GetTemplateDiagnostic(9, 71, "Found invalid character '&' in property name"));

            //Test static method call
            source = BuildTestSourceExtension(methodName, @"(IClientRequestInfo) null, ""Hello {&} the World"", 8");
            await VerifyCs.VerifyAnalyzerAsync(source, GetTemplateDiagnostic(10, 60, "Found invalid character '&' in property name"));
        }

        [TestCase("InfoFormat")]
        public async Task NoPropertyName_Negative(string methodName)
        {
            var source = BuildTestSource(@$"{methodName}(@""Hello  {{}}"", 64)");
            await VerifyCs.VerifyAnalyzerAsync(source, GetTemplateDiagnostic(9, 45, "Found property without name"));


            //Extension
            source = BuildTestSource(@$"{methodName}((IClientRequestInfo) null, ""Hello  {{}}"", 64)", LOG_HELPER_SOURCE);
            await VerifyCs.VerifyAnalyzerAsync(source, GetTemplateDiagnostic(9, 71, "Found property without name"));

            //Test static method call
            source = BuildTestSourceExtension(methodName, @"(IClientRequestInfo) null, ""Hello  {}"", 64");
            await VerifyCs.VerifyAnalyzerAsync(source, GetTemplateDiagnostic(10, 60, "Found property without name"));
        }

        [TestCase("InfoFormat")]
        public async Task TemplateWithDestructuringButMissingName_Negative(string methodName)
        {
            var source = BuildTestSource(@$"{methodName}(@""Hello  {{@}}"", 64)");

            await VerifyCs.VerifyAnalyzerAsync(source, GetTemplateDiagnostic(9, 45, "Found property with destructuring hint but without name"));

            //Extension
            source = BuildTestSource(@$"{methodName}((IClientRequestInfo) null, ""Hello  {{@}}"", 64)", LOG_HELPER_SOURCE);
            await VerifyCs.VerifyAnalyzerAsync(source, GetTemplateDiagnostic(9, 71, "Found property with destructuring hint but without name"));

            //Test static method call
            source = BuildTestSourceExtension(methodName, @"(IClientRequestInfo) null, ""Hello  {@}"", 64");
            await VerifyCs.VerifyAnalyzerAsync(source, GetTemplateDiagnostic(10, 60, "Found property with destructuring hint but without name"));
        }

        [TestCase("InfoFormat")]
        public async Task InvalidPropertyAlignment_Negative(string methodName)
        {
            var source = BuildTestSource(@$"{methodName}(@""Hello  {{Abc,b}}"", 64)");

            await VerifyCs.VerifyAnalyzerAsync(source, GetTemplateDiagnostic(9, 50, "Found invalid character 'b' in property alignment"));

            //Extension
            source = BuildTestSource(@$"{methodName}((IClientRequestInfo) null, @""Hello  {{Abc,b}}"", 64)", LOG_HELPER_SOURCE);
            await VerifyCs.VerifyAnalyzerAsync(source, GetTemplateDiagnostic(9, 77, "Found invalid character 'b' in property alignment"));

            //Test static method call
            source = BuildTestSourceExtension(methodName, @"(IClientRequestInfo) null, ""Hello  {Abc,b}"", 64");
            await VerifyCs.VerifyAnalyzerAsync(source, GetTemplateDiagnostic(10, 65, "Found invalid character 'b' in property alignment"));
        }

        [TestCase("InfoFormat")]
        public async Task InvalidPropertyFormat_Negative(string methodName)
        {
            var source = BuildTestSource(@$"{methodName}(@""Hello  {{Abc:$}}"", 64)");
            await VerifyCs.VerifyAnalyzerAsync(source, GetTemplateDiagnostic(9, 50, "Found invalid character '$' in property format"));


            source = BuildTestSource(@$"{methodName}(@""Hello  {{Abc,1:$}}"", 64)");
            await VerifyCs.VerifyAnalyzerAsync(source, GetTemplateDiagnostic(9, 52, "Found invalid character '$' in property format"));



            //Extension
            source = BuildTestSource(@$"{methodName}((IClientRequestInfo) null, ""Hello  {{Abc:$}}"", 64)", LOG_HELPER_SOURCE);
            await VerifyCs.VerifyAnalyzerAsync(source, GetTemplateDiagnostic(9, 76, "Found invalid character '$' in property format"));

            //Test static method call
            source = BuildTestSourceExtension(methodName, @"(IClientRequestInfo) null, ""Hello  {Abc:$}"", 64");
            await VerifyCs.VerifyAnalyzerAsync(source, GetTemplateDiagnostic(10, 65, "Found invalid character '$' in property format"));
        }

        [TestCase("InfoFormat")]
        public async Task PrematureEnd_Negative(string methodName)
        {
            var source = BuildTestSource(@$"{methodName}(@""Hello,{{"", 8)");

            var expectedDiagnostic = GetTemplateDiagnostic(9, 44, "Encountered end of messageTemplate while parsing property");

            await VerifyCs.VerifyAnalyzerAsync(source, expectedDiagnostic);



            source = BuildTestSource(@$"{methodName}(@""Hello {{Name to """"the"""" World"", 8)");
            await VerifyCs.VerifyAnalyzerAsync(source, expectedDiagnostic);

            source = BuildTestSource(@$"{methodName}(@""Hello {{Name to the World"", 8)");
            await VerifyCs.VerifyAnalyzerAsync(source, expectedDiagnostic);


            //Extension
            source = BuildTestSource(@$"{methodName}((IClientRequestInfo) null, ""Hello,{{"", 8)", LOG_HELPER_SOURCE);
            await VerifyCs.VerifyAnalyzerAsync(source, GetTemplateDiagnostic(9, 70, "Encountered end of messageTemplate while parsing property"));

            //Test static method call
            source = BuildTestSourceExtension(methodName, @"(IClientRequestInfo) null, ""Hello,{"", 8");
            await VerifyCs.VerifyAnalyzerAsync(source, GetTemplateDiagnostic(10, 59, "Encountered end of messageTemplate while parsing property"));
        }




        private static DiagnosticResult GetTemplateDiagnostic(int line, int column, params object[] arguments)
            => VerifyCs.Diagnostic(SerilogAnalyzer.TemplateRule).WithLocation(line, column).WithArguments(arguments);
    }
}
