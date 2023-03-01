using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Self.Analyzer.Tests
{
    internal class KeyValueAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {
        public static KeyValueAnalyzerConfigOptionsProvider ReadEditorConfig(string editorConfigFile)
        {
            var editorConfig = LogCallsAnalyzer.Parser.AnalyzerConfig.Parse(File.ReadAllText(editorConfigFile), editorConfigFile);
            var editorConfigDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (editorConfig.GlobalSection is { } globalSection)
                foreach (var (key, value) in globalSection.Properties)
                    editorConfigDict[key] = value;

            foreach (var section in editorConfig.NamedSections)
                foreach (var (key, value) in section.Properties)
                    editorConfigDict[key] = value;

            return new KeyValueAnalyzerConfigOptionsProvider(editorConfigDict.Select(kvp => (kvp.Key, kvp.Value)));
        }

        public KeyValueAnalyzerConfigOptionsProvider(IEnumerable<(string, string)> options) =>
            GlobalOptions = new KeyValueAnalyzerConfigOptions(options);

        public override AnalyzerConfigOptions GlobalOptions { get; }

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => GlobalOptions;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => GlobalOptions;

        internal class KeyValueAnalyzerConfigOptions : AnalyzerConfigOptions
        {
            private readonly Dictionary<string, string> _options;

            public KeyValueAnalyzerConfigOptions(IEnumerable<(string key, string value)> options) =>
                _options = options.ToDictionary(e => e.key, e => e.value);

            public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value) => _options.TryGetValue(key, out value);
        }
    }
}
