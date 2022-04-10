using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
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
    }
}
