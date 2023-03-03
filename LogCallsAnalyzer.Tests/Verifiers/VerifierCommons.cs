#nullable disable
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace LogCallsAnalyzer.Tests.Verifiers
{
    static class VerifierCommons
    {
        public static void Setup<TVerifier>(AnalyzerTest<TVerifier> test)
            where TVerifier : IVerifier, new()
        {
            //test.ReferenceAssemblies = ReferenceAssemblies.NetFramework.Net461.Default;
            test.ReferenceAssemblies =
                test.ReferenceAssemblies.AddPackages(ImmutableArray.Create(new PackageIdentity("Serilog", "2.10.0")));

            var additionalReferences = new HashSet<Assembly>();

            foreach (var type in new[] { typeof(Serilog.Log), typeof(LoggingAbstractions.ILog), typeof(LoggingAbstractions.Serilog.Log) })
                if (additionalReferences.Add(type.Assembly))
                    test.TestState.AdditionalReferences.Add(type.Assembly);

#if NETFRAMEWORK
            System.Reflection.Assembly? standardAssembly = null;
            foreach (var a in System.AppDomain.CurrentDomain.GetAssemblies())
                if (a.GetName().Name == "netstandard")
                    standardAssembly = a;


            test.TestState.AdditionalReferences.Add(standardAssembly
                ?? throw new System.NotSupportedException("netstandard is needed for legacy framework tests"));
#endif
            test.SolutionTransforms.Add((solution, projectId) =>
            {
                if (solution.GetProject(projectId)?.CompilationOptions is { } compilationOptions)
                {
                    compilationOptions = compilationOptions.WithSpecificDiagnosticOptions(
                        compilationOptions.SpecificDiagnosticOptions.SetItems(CSharpVerifierHelper
                            .NullableWarnings));
                    solution = solution.WithProjectCompilationOptions(projectId, compilationOptions);
                }
                return solution;
            });
        }

        public static AnalyzerOptions AddAnalyzerOptions(AnalyzerOptions analyzerOptions) =>
            new(analyzerOptions.AdditionalFiles, new KeyValueAnalyzerConfigOptionsProvider(new[]
            {
                (SerilogAnalyzer.LOGGER_ABSTRACTION_OPTION, "LoggingAbstractions.ILog")
            }));

        internal class KeyValueAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
        {
            public KeyValueAnalyzerConfigOptionsProvider(IEnumerable<(string, string)> options) => GlobalOptions = new KeyValueAnalyzerConfigOptions(options);

            public override AnalyzerConfigOptions GlobalOptions { get; }

            public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => GlobalOptions;

            public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => GlobalOptions;
        }

        internal class KeyValueAnalyzerConfigOptions : AnalyzerConfigOptions
        {
            private readonly Dictionary<string, string> _options;

            public KeyValueAnalyzerConfigOptions(IEnumerable<(string key, string value)> options) => _options = options.ToDictionary(e => e.key, e => e.value);

            public override bool TryGetValue(string key, out string value) => _options.TryGetValue(key, out value);
        }
    }
}
