using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
using Microsoft.CodeAnalysis.Diagnostics;
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

            var editorConfig = LogCallsAnalyzer.Parser.AnalyzerConfig.Parse(await File.ReadAllTextAsync(editorConfigFile), editorConfigFile);
            var editorConfigDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (editorConfig.GlobalSection is { } globalSection)
                foreach (var (key, value) in globalSection.Properties)
                    editorConfigDict[key] = value;

            foreach (var section in editorConfig.NamedSections)
                foreach (var (key, value) in section.Properties)
                    editorConfigDict[key] = value;

            var analyzerOptionsProvider = new KeyValueAnalyzerConfigOptionsProvider(editorConfigDict.Select(kvp => (kvp.Key, kvp.Value)));



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
                    editorConfigDict.TryGetValue(SerilogAnalyzer.LOGGER_ABSTRACTION_OPTION, out var loggerTypeName) && compilation.GetTypeByMetadataName(loggerTypeName) != null;
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
                            diagnostics.Add(new(project.Name, filename, d));
                }
            }

            var csv = WriteAsCsv(diagnostics);

            Assert.That(diagnostics, Has.Count.EqualTo(0), () =>
"| Project | File | Diagnostic | Message |" + Environment.NewLine +
"|---------|------|------------|---------|" + Environment.NewLine +
string.Join(Environment.NewLine, diagnostics.Select(d => "|" + d.Project + "|" + d.File + "|" + GetDiagnosticName(d.Diagnostic) + "|" + FormatDiagnostic(d.Diagnostic) + "|"))
            );
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
                    $"{++i};{FormatAsCsv(dm.Project)};{FormatAsCsv(dm.File)};{FormatAsCsv(GetDiagnosticName(dm.Diagnostic))};{FormatAsCsv(FormatDiagnostic(dm.Diagnostic))}"
                ),
                sb => sb.ToString()
                );
        }

        private static string FormatDiagnostic(Diagnostic diagnostic)
        {
            if (diagnostic == null) throw new ArgumentNullException(nameof(diagnostic));

            var culture = CultureInfo.InvariantCulture;

            var location = diagnostic.Location;

            return (location.Kind is LocationKind.SourceFile or LocationKind.XmlFile or LocationKind.ExternalFile) &&
                    location.GetLineSpan() is FileLinePositionSpan span &&
                    location.GetMappedLineSpan() is FileLinePositionSpan mappedSpan &&
                    span.IsValid && mappedSpan.IsValid
                ? FormattableString.Invariant(
                    $"({mappedSpan.Span.Start.Line + 1},{mappedSpan.Span.Start.Character + 1}): {diagnostic.GetMessage(culture)}")
                : diagnostic.GetMessage(culture);
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

            public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value) => _options.TryGetValue(key, out value);
        }

        /*private class ConsoleProgressReporter : IProgress<ProjectLoadProgress>
        {
            public void Report(ProjectLoadProgress loadProgress)
            {
                var projectDisplay = Path.GetFileName(loadProgress.FilePath);
                if (loadProgress.TargetFramework != null)
                {
                    projectDisplay += $" ({loadProgress.TargetFramework})";
                }

                Console.WriteLine($"{loadProgress.Operation,-15} {loadProgress.ElapsedTime,-15:m\\:ss\\.fffffff} {projectDisplay}");
            }
        }*/
    }
}
