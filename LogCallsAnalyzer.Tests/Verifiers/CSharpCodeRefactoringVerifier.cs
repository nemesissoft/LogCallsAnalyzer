﻿using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Testing;

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace LogCallsAnalyzer.Tests.Verifiers
{
    public static class CSharpCodeRefactoringVerifier<TCodeRefactoring>
        where TCodeRefactoring : CodeRefactoringProvider, new()
    {
        /// <inheritdoc cref="CodeRefactoringVerifier{TCodeRefactoring, TTest, TVerifier}.VerifyRefactoringAsync(string, string)"/>
        public static async Task VerifyRefactoringAsync(string source, string fixedSource)
        {
            await VerifyRefactoringAsync(source, DiagnosticResult.EmptyDiagnosticResults, fixedSource);
        }

        /// <inheritdoc cref="CodeRefactoringVerifier{TCodeRefactoring, TTest, TVerifier}.VerifyRefactoringAsync(string, DiagnosticResult, string)"/>
        public static async Task VerifyRefactoringAsync(string source, DiagnosticResult expected, string fixedSource)
        {
            await VerifyRefactoringAsync(source, new[] { expected }, fixedSource);
        }

        /// <inheritdoc cref="CodeRefactoringVerifier{TCodeRefactoring, TTest, TVerifier}.VerifyRefactoringAsync(string, DiagnosticResult[], string)"/>
        public static async Task VerifyRefactoringAsync(string source, DiagnosticResult[] expected, string fixedSource)
        {
            var test = new Test
            {
                TestCode = source,
                FixedCode = fixedSource,
            };

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync(CancellationToken.None);
        }

        public class Test : CSharpCodeRefactoringTest<TCodeRefactoring, NUnitVerifier>
        {
            public Test()
            {
                SolutionTransforms.Add((solution, projectId) =>
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
}
