using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace LogCallsAnalyzer.Helpers
{
    internal readonly struct LoggingMethodMeta
    {
        public IMethodSymbol Method { get; }
        //public string MethodName { get; } public string ContainingType { get; }
        public bool IsFormatMethod { get; }
        public string MessageTemplateName { get; }

        private LoggingMethodMeta(IMethodSymbol method) : this()
        {
            Method = method;

            //MethodName = Method.Name; ContainingType = Method.ContainingType.ToString();
            IsFormatMethod = Method.Name.EndsWith("Format");
            MessageTemplateName = IsFormatMethod ? "format" : "message";
        }

        private LoggingMethodMeta(IMethodSymbol method, string messageTemplateName)
        {
            Method = method;
            IsFormatMethod = Method.Name.EndsWith("Format");
            MessageTemplateName = messageTemplateName;
        }
        
        public static bool TryBuildMeta(in SyntaxNode node, in SemanticModel semanticModel,
            [NotNullWhen(returnValue: true)] out InvocationExpressionSyntax? invocation,
            [NotNullWhen(returnValue: true)] out Compilation? compilation,
            out LoggingMethodMeta meta, CancellationToken token)
        {
            invocation = default; compilation = default; meta = default;

            if (node is not InvocationExpressionSyntax inv) { return false; }
            invocation = inv;

            var info = semanticModel.GetSymbolInfo(invocation, token);
            if (info.Symbol is not IMethodSymbol method) { return false; }

            compilation = semanticModel.Compilation;

            // is it appropriate logging method?
            if (!LoggerMethods.Contains(method.Name)) return false;

            if (compilation.GetTypeByMetadataName(LOGGER_TYPE_NAME1) is not { } type1) { return false; }
            if (compilation.GetTypeByMetadataName(LOGGER_TYPE_NAME2) is not { } type2) { return false; }

            var instanceType = method.ContainingType;
            if (SymbolEqualityComparer.Default.Equals(instanceType, type1) || SymbolEqualityComparer.Default.Equals(instanceType, type2))
            {
                meta = new LoggingMethodMeta(method);
                return true;
            }

            // is serilog even present in the compilation?
            var messageTemplateAttribute = semanticModel.Compilation.GetTypeByMetadataName("Serilog.Core.MessageTemplateFormatMethodAttribute");
            if (messageTemplateAttribute == null) return false;

            // is it a serilog logging method?
            var attributes = method.GetAttributes();
            var attributeData = attributes.FirstOrDefault(x => SymbolEqualityComparer.Default.Equals(x.AttributeClass, messageTemplateAttribute));
            if (attributeData?.ConstructorArguments.FirstOrDefault().Value is not string messageTemplateName || messageTemplateName.Length == 0) return false;

            meta = new LoggingMethodMeta(method, messageTemplateName);
            return true;
        }

        private const string LOGGER_TYPE_NAME1 = "LoggingAbstractions.ILog";
        private const string LOGGER_TYPE_NAME2 = "LoggingAbstractions.Serilog.Log";

        public static readonly HashSet<string> LoggerMethods = new(new[]
        {
            "Debug", "Info", "Warn", "Error", "Fatal",
            "DebugFormat", "InfoFormat", "WarnFormat", "ErrorFormat", "FatalFormat"
        });
    }
}
