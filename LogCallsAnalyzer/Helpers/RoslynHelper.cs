using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LogCallsAnalyzer.Helpers
{
    static class RoslynHelper
    {
        /// <summary>
        /// Returns the parameter to which this argument is passed. If <paramref name="allowParams"/>
        /// is true, the last parameter will be returned if it is params parameter and the index of
        /// the specified argument is greater than the number of parameters.
        /// </summary>
        /// <remarks>Lifted from http://source.roslyn.io/#Microsoft.CodeAnalysis.CSharp.Workspaces/Extensions/ArgumentSyntaxExtensions.cs,af94352fb5da7056 </remarks>
        public static IParameterSymbol? DetermineParameter(ArgumentSyntax argument, SemanticModel semanticModel, bool allowParams = false, CancellationToken cancellationToken = default)
        {
            if (argument.Parent is not BaseArgumentListSyntax { Parent: ExpressionSyntax invocableExpression } argumentList ||
                semanticModel.GetSymbolInfo(invocableExpression, cancellationToken).Symbol is not { } symbol)
                return null;


            var parameters = (symbol as IMethodSymbol)?.Parameters ?? (symbol as IPropertySymbol)?.Parameters ?? ImmutableArray<IParameterSymbol>.Empty;

            // Handle named argument
            if (argument.NameColon is { IsMissing: false })
            {
                var name = argument.NameColon.Name.Identifier.ValueText;
                return parameters.FirstOrDefault(p => p.Name == name);
            }

            // Handle positional argument
            var index = argumentList.Arguments.IndexOf(argument);
            if (index < 0)
                return null;

            if (index < parameters.Length)
                return parameters[index];

            if (allowParams)
            {
                var lastParameter = parameters.LastOrDefault();
                if (lastParameter == null)
                    return null;

                if (lastParameter.IsParams)
                    return lastParameter;
            }

            return null;
        }

        /// <summary>
        /// Given an expression node, tries to generate an appropriate name that can be used for
        /// that expression. 
        /// </summary>
        /// <remarks>Lifted from https://github.com/dotnet/roslyn/blob/c5c72d57af0ee9c615ee6a810394ea4e92d8d913/src/Workspaces/CSharp/Portable/Extensions/SemanticModelExtensions.cs#L200 </remarks>
        public static string? GenerateNameForExpression(this SemanticModel semanticModel, ExpressionSyntax expression, bool capitalize = false)
        {
            // Try to find a usable name node that we can use to name the
            // parameter.  If we have an expression that has a name as part of it
            // then we try to use that part.
            var current = expression;
            while (true)
            {
                current = current.WalkDownParentheses();

                if (current.Kind() == SyntaxKind.IdentifierName)
                {
                    return ((IdentifierNameSyntax)current).Identifier.ValueText.ToPascalCase();
                }
                else if (current is MemberAccessExpressionSyntax memberAccessExpressionSyntax)
                {
                    return memberAccessExpressionSyntax.Name.Identifier.ValueText.ToPascalCase();
                }
                else if (current is MemberBindingExpressionSyntax memberBindingExpressionSyntax)
                {
                    return memberBindingExpressionSyntax.Name.Identifier.ValueText.ToPascalCase();
                }
                else if (current is ConditionalAccessExpressionSyntax conditionalAccessExpressionSyntax)
                {
                    current = conditionalAccessExpressionSyntax.WhenNotNull;
                }
                else if (current is CastExpressionSyntax castExpressionSyntax)
                {
                    current = castExpressionSyntax.Expression;
                }
                //else if (current is DeclarationExpressionSyntax)
                //{
                //    var decl = (DeclarationExpressionSyntax)current;
                //    var name = decl.Designation as SingleVariableDesignationSyntax;
                //    if (name == null)
                //    {
                //        break;
                //    }

                //    return name.Identifier.ValueText.ToCamelCase();
                //}
                else
                {
                    break;
                }
            }

            // Otherwise, figure out the type of the expression and generate a name from that
            // instead.
            var info = semanticModel.GetTypeInfo(expression);

            // If we can't determine the type, then fallback to some placeholders.
            var type = info.Type;
            return type?.CreateParameterName(capitalize);
        }

        public static string CreateParameterName(this ITypeSymbol type, bool capitalize = false)
        {
            while (true)
            {
                if (type is IArrayTypeSymbol arrayType)
                {
                    type = arrayType.ElementType;
                    continue;
                }

                if (type is IPointerTypeSymbol pointerType)
                {
                    type = pointerType.PointedAtType;
                    continue;
                }

                break;
            }

            var shortName = GetParameterName(type);
            return capitalize ? shortName.ToPascalCase() : shortName.ToCamelCase();
        }

        private const string DEFAULT_PARAMETER_NAME = "p";
        private const string DEFAULT_BUILT_IN_PARAMETER_NAME = "v";
        private static string GetParameterName(ITypeSymbol type)
        {
            if (type == null || type.IsAnonymousType /*|| type.IsTupleType*/)
                return DEFAULT_PARAMETER_NAME;

            if (type.IsSpecialType() || type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                return DEFAULT_BUILT_IN_PARAMETER_NAME;

            var shortName = type.GetShortName();
            return shortName.Length == 0 ? DEFAULT_PARAMETER_NAME : shortName;
        }

        public static bool IsSpecialType(this ITypeSymbol symbol)
        {
            if (symbol != null)
            {
                switch (symbol.SpecialType)
                {
                    case SpecialType.System_Object:
                    case SpecialType.System_Void:
                    case SpecialType.System_Boolean:
                    case SpecialType.System_SByte:
                    case SpecialType.System_Byte:
                    case SpecialType.System_Decimal:
                    case SpecialType.System_Single:
                    case SpecialType.System_Double:
                    case SpecialType.System_Int16:
                    case SpecialType.System_Int32:
                    case SpecialType.System_Int64:
                    case SpecialType.System_Char:
                    case SpecialType.System_String:
                    case SpecialType.System_UInt16:
                    case SpecialType.System_UInt32:
                    case SpecialType.System_UInt64:
                        return true;
                }
            }

            return false;
        }

        private static readonly SymbolDisplayFormat _shortNameFormat = new(miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.ExpandNullable);

        public static string GetShortName(this INamespaceOrTypeSymbol symbol)
            => symbol.ToDisplayString(_shortNameFormat);

        public static ExpressionSyntax WalkDownParentheses(this ExpressionSyntax expression)
        {
            while (expression.IsKind(SyntaxKind.ParenthesizedExpression))
            {
                expression = ((ParenthesizedExpressionSyntax)expression).Expression;
            }

            return expression;
        }

        private static readonly Func<char, char> _toLower = char.ToLower;
        private static readonly Func<char, char> _toUpper = char.ToUpper;

        public static string ToPascalCase(this string shortName, bool trimLeadingTypePrefix = true)
            => ConvertCase(shortName, trimLeadingTypePrefix, _toUpper);

        public static string ToCamelCase(this string shortName, bool trimLeadingTypePrefix = true)
            => ConvertCase(shortName, trimLeadingTypePrefix, _toLower);

        private static string ConvertCase(this string shortName, bool trimLeadingTypePrefix, Func<char, char> convert)
        {
            // Special case the common .net pattern of "IFoo" as a type name.  In this case we
            // want to generate "foo" as the parameter name.  
            if (!string.IsNullOrEmpty(shortName))
            {
                if (trimLeadingTypePrefix && (shortName.LooksLikeInterfaceName() || shortName.LooksLikeTypeParameterName()))
                    return convert(shortName[1]) + shortName.Substring(2);

                if (convert(shortName[0]) != shortName[0])
                    return convert(shortName[0]) + shortName.Substring(1);
            }

            return shortName;
        }

        public static bool LooksLikeInterfaceName(this string name)
            => name.Length >= 3 && name[0] == 'I' && char.IsUpper(name[1]) && char.IsLower(name[2]);

        public static bool LooksLikeTypeParameterName(this string name)
            => name.Length >= 3 && name[0] == 'T' && char.IsUpper(name[1]) && char.IsLower(name[2]);

        public static bool DerivesFromException(ITypeSymbol type, ISymbol exceptionSymbol)
        {
            for (ITypeSymbol? symbol = type; symbol != null; symbol = symbol.BaseType)
                if (SymbolEqualityComparer.Default.Equals(symbol, exceptionSymbol))
                    return true;
            return false;
        }
    }
}
