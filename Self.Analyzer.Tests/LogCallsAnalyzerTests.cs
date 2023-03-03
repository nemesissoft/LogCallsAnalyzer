using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using LogCallsAnalyzer;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using NUnit.Framework;

namespace Self.Analyzer.Tests
{
    [TestFixture]
    public class LogCallsAnalyzerTests
    {
        [Test]
        public async Task AnalyzeSolution()
        {
            const string SOLUTION_FILE = "LogCallsAnalyzer.sln";
            var (solution, solutionPath) = await OpenSolution(SOLUTION_FILE);

            if (Path.GetDirectoryName(Path.GetFullPath(solutionPath)) is not { } directoryName || Path.Combine(directoryName, ".editorconfig") is not { } editorConfigFile || !File.Exists(editorConfigFile))
            {
                Assert.Fail("Cannot locate .editorconfig in solution directory");
                return;
            }

            var analyzerOptionsProvider = KeyValueAnalyzerConfigOptionsProvider.ReadEditorConfig(editorConfigFile);

            var diagnostics = new List<DiagnosticMeta>();

            foreach (var project in solution.Projects)
            {
                if (Path.GetFileNameWithoutExtension(project.FilePath) is { } fileName &&
                    (fileName.EndsWith(".Tests", StringComparison.Ordinal) || fileName.StartsWith("UnitTest", StringComparison.Ordinal) || fileName.StartsWith("LogCallsAnalyzer", StringComparison.Ordinal))
                    )
                    continue;

                var compilation = await project.GetCompilationAsync();
                if (compilation is null) continue;

                var configuredLoggerTypeExist =
                    analyzerOptionsProvider.GlobalOptions.TryGetValue(SerilogAnalyzer.LOGGER_ABSTRACTION_OPTION, out var loggerTypeName) &&
                    compilation.GetTypeByMetadataName(loggerTypeName) != null;

                var serilogAttributesAvailable = compilation.GetTypeByMetadataName("Serilog.Core.MessageTemplateFormatMethodAttribute") != null;

                if (!configuredLoggerTypeExist && !serilogAttributesAvailable)
                    Assert.Fail($"Project {project.Name}: Either Serilog attributes should be available in compilation time or top level .editorconfig file should have option {SerilogAnalyzer.LOGGER_ABSTRACTION_OPTION} configured to existing type");

                foreach (var syntaxTree in compilation.SyntaxTrees)
                {
                    var root = await syntaxTree.GetRootAsync();
                    if (root.DescendantTrivia().FirstOrDefault() is var firstTrivia && firstTrivia.IsKind(SyntaxKind.SingleLineCommentTrivia) &&
                       firstTrivia.ToString() is { } comment &&
                       comment.Contains("DO_NOT_ANALYZE_BY") && comment.Contains("LogCallsAnalyzer")
                       )
                        continue; //omit files starting with //DO_NOT_ANALYZE_BY: LogCallsAnalyzer


                    var semanticModel = compilation.GetSemanticModel(syntaxTree, true);
                    var invocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>();
                    var filename = Path.GetFileName(syntaxTree.FilePath);

                    foreach (var invocation in invocations)
                        foreach (var d in SerilogAnalyzer.GetDiagnostics(analyzerOptionsProvider, invocation, semanticModel))
                        {
                            if (
                                analyzerOptionsProvider.GetOptions(invocation.SyntaxTree) is { } options &&
                                options.TryGetValue($"dotnet_diagnostic.{d.Id.ToLower()}.severity", out var effectiveSeverity) &&
                                !string.IsNullOrWhiteSpace(effectiveSeverity)
                                )
                                diagnostics.Add(new(project.Name, filename, WithSeverity(d, effectiveSeverity)));
                            else
                                diagnostics.Add(new(project.Name, filename, d));
                        }

                }
            }

            var csv = WriteAsCsv(diagnostics);

            var errorAndWarnDiagnostics = diagnostics
                .Where(d => d.Diagnostic.Severity is DiagnosticSeverity.Warning or DiagnosticSeverity.Error)
                .ToList();

            Assert.That(errorAndWarnDiagnostics, Has.Count.EqualTo(0), () =>
"| Project | File | Diagnostic | Message |" + Environment.NewLine +
"|---------|------|------------|---------|" + Environment.NewLine +
string.Join(Environment.NewLine, errorAndWarnDiagnostics.Select(d =>
    $"|{d.Project}|{d.File}{FormatLocation(d.Diagnostic)}|{GetDiagnosticName(d.Diagnostic)}|{FormatDiagnostic(d.Diagnostic)}|"))
            );
        }

        private Diagnostic WithSeverity(Diagnostic d, string effectiveSeverityText)
        {
            var effectiveSeverity = effectiveSeverityText.ToLower() switch
            {
                "error" => DiagnosticSeverity.Error,
                "warning" => DiagnosticSeverity.Warning,
                "suggestion" => DiagnosticSeverity.Info,
                _ => DiagnosticSeverity.Hidden,
            };
            if (effectiveSeverity == d.Severity)
            {
                return d;
            }
            else
            {
                var method = d.GetType().GetMethod("WithSeverity", BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? throw new MissingMethodException(d.GetType().FullName, "WithSeverity");
                return (Diagnostic)method.Invoke(d, new object[] { effectiveSeverity })!;
            }

        }

        record struct DiagnosticMeta(string Project, string File, Diagnostic Diagnostic);

        private static string WriteAsCsv(IEnumerable<DiagnosticMeta> diagnostics)
        {
            static string FormatAsCsv(string text) =>
                text.Replace(';', ' ').Replace('\r', ' ').Replace('\n', ' ');

            int i = 0;

            return diagnostics.Aggregate(
                new StringBuilder("Number;Project;File;Diagnostic;Message").AppendLine(),
                (sb, dm) => sb.AppendLine(
                    $"{++i};{FormatAsCsv(dm.Project)};{FormatAsCsv(dm.File + FormatLocation(dm.Diagnostic))};{FormatAsCsv(GetDiagnosticName(dm.Diagnostic))};{FormatAsCsv(FormatDiagnostic(dm.Diagnostic))}"
                ),
                sb => sb.ToString()
                );
        }

        private static string FormatDiagnostic(Diagnostic diagnostic)
        {
            if (diagnostic == null) throw new ArgumentNullException(nameof(diagnostic));

            return TryGetLocation(diagnostic.Location, out var line, out var character)
                ? FormattableString.Invariant(
                    $"({line},{character}): {diagnostic.GetMessage(CultureInfo.InvariantCulture)}")
                : diagnostic.GetMessage(CultureInfo.InvariantCulture);
        }

        private static string FormatLocation(Diagnostic diagnostic) =>
            TryGetLocation(diagnostic.Location, out var line, out var character) ? $" {line} {character}" : "";

        private static bool TryGetLocation(Location location, out int line, out int character)
        {
            line = character = 0;
            if ((location.Kind is LocationKind.SourceFile or LocationKind.XmlFile or LocationKind.ExternalFile) &&
                location.GetLineSpan() is FileLinePositionSpan span &&
                location.GetMappedLineSpan() is FileLinePositionSpan mappedSpan &&
                span.IsValid && mappedSpan.IsValid
            )
            {
                line = mappedSpan.Span.Start.Line + 1;
                character = mappedSpan.Span.Start.Character + 1;
                return true;
            }
            else return false;
        }

        private static string GetDiagnosticName(Diagnostic diagnostic)
        {
            var prefix = diagnostic.Severity switch
            {
                DiagnosticSeverity.Hidden => "hidden",
                DiagnosticSeverity.Info => "info",
                DiagnosticSeverity.Warning => "warning",
                DiagnosticSeverity.Error => "error",
                _ => throw new ArgumentOutOfRangeException(nameof(diagnostic), diagnostic.Severity, $"Severity of '{diagnostic.Severity}' is not supported")
            };

            return $"{prefix} {diagnostic.Id}";
        }

        private static async Task<(Solution solution, string solutionPath)> OpenSolution(string solutionName)
        {
            var instance = SelectVisualStudioInstance(MSBuildLocator.QueryVisualStudioInstances());
            Console.WriteLine($"Using MSBuild at '{instance.MSBuildPath}' to load projects.");
            MSBuildLocator.RegisterInstance(instance);


            using var workspace = MSBuildWorkspace.Create();
            workspace.WorkspaceFailed += (_, e) => Console.WriteLine(e.Diagnostic.Message);

            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new InvalidOperationException("Cannot obtain current directory");
            DirectoryInfo dir;
            do
                path = GetParentDirectory(path, out dir);
            while (!string.Equals(dir.Name, "bin", StringComparison.OrdinalIgnoreCase));

            var solutionPath = Path.Combine(GetParentDirectory(path, out _), solutionName);

            Console.WriteLine($"Loading solution '{solutionPath}'");
            var solution = await workspace.OpenSolutionAsync(solutionPath/*, new ConsoleProgressReporter()*/);
            Console.WriteLine($"Finished loading solution '{solutionPath}'");

            return (solution, solutionPath);

            static string GetParentDirectory(string path, out DirectoryInfo dir)
            {
                dir = new DirectoryInfo(path);
                return dir.Parent?.FullName ??
                       throw new InvalidOperationException($"Cannot obtain parent directory for '{path}'");
            }

            static VisualStudioInstance SelectVisualStudioInstance(IEnumerable<VisualStudioInstance> visualStudioInstances)
            {
                using var enumerator = visualStudioInstances.GetEnumerator();
                if (!enumerator.MoveNext()) throw new InvalidOperationException("No MS Build / Visual Studio installed to perform code analysis");
                var max = enumerator.Current;

                while (enumerator.MoveNext() && enumerator.Current is { } current)
                    if (max.Version < current.Version)
                        max = current;

                return max;
            }
        }
    }
}
