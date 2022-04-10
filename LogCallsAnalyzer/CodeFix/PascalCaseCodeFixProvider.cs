using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LogCallsAnalyzer.CodeFix
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PascalCaseCodeFixProvider)), Shared]
    public class PascalCaseCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(SerilogAnalyzer.PascalPropertyNameRule.Id);

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var declaration = root.FindNode(diagnosticSpan);
            
            const string TITLE = "Pascal case Serilog property";
            context.RegisterCodeFix(
                CodeAction.Create(
                    TITLE,
                    c => PascalCaseTheProperties(context.Document, declaration.DescendantNodesAndSelf().OfType<LiteralExpressionSyntax>().First(), c),
                    TITLE),
                diagnostic);
        }

        private static async Task<Solution> PascalCaseTheProperties(Document document, LiteralExpressionSyntax node, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var oldToken = node.Token;

            var sb = new StringBuilder();
            if (oldToken.Text.StartsWith("@", StringComparison.Ordinal))
            {
                sb.Append('@');
            }
            sb.Append('"');

            var interpolatedString = (InterpolatedStringExpressionSyntax)SyntaxFactory.ParseExpression("$" + oldToken);
            foreach (var child in interpolatedString.Contents)
            {
                switch (child)
                {
                    case InterpolatedStringTextSyntax text:
                        sb.Append(text.TextToken.ToString());
                        break;
                    case InterpolationSyntax interpolation:
                        AppendAsPascalCase(sb, interpolation.ToString());
                        break;
                }
            }
            sb.Append('"');

            var newToken = SyntaxFactory.ParseToken(sb.ToString());
            root = root.ReplaceToken(oldToken, newToken);

            document = document.WithSyntaxRoot(root);
            return document.Project.Solution;
        }

        private static void AppendAsPascalCase(StringBuilder sb, string input)
        {
            bool uppercaseChar = true;
            bool skipTheRest = false;
            const char STRINGIFICATION_PREFIX = '$', DESTRUCTURING_PREFIX = '@';

            for (int i = 0; i < input.Length; i++)
            {
                char current = input[i];
                if (i < 2 && current == '{' || current == STRINGIFICATION_PREFIX || current == DESTRUCTURING_PREFIX)
                {
                    sb.Append(current);
                    continue;
                }
                if (skipTheRest || current is ',' or ':' or '}')
                {
                    skipTheRest = true;
                    sb.Append(current);
                    continue;
                }
                if (current == '_')
                {
                    uppercaseChar = true;
                    continue;
                }
                sb.Append(uppercaseChar ? char.ToUpper(current) : current);
                uppercaseChar = false;
            }
        }
    }
}