﻿#pragma warning disable 1573
namespace WpfAnalyzers
{
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class DependencyObject
    {
        internal static bool IsPotentialSetValueCall(InvocationExpressionSyntax invocation)
        {
            return invocation.InvokedMethodName() == "SetValue";
        }

        internal static bool IsPotentialSetCurrentValueCall(InvocationExpressionSyntax invocation)
        {
            return invocation.InvokedMethodName() == "SetCurrentValue";
        }

        internal static bool IsPotentialGetValueCall(InvocationExpressionSyntax invocation)
        {
            return invocation.InvokedMethodName() == "GetValue";
        }

        /// <summary>
        /// Check if <paramref name="invocation"/> is a call to dependencyObject.GetValue(FooProperty, value)
        /// </summary>
        /// <param name="property">The DependencyProperty used as argument</param>
        /// <param name="field">The field symbol for <paramref name="property"/></param>
        internal static bool TryGetGetValueArgument(InvocationExpressionSyntax invocation, SemanticModel semanticModel, CancellationToken cancellationToken, out ArgumentSyntax property, out IFieldSymbol field)
        {
            property = null;
            field = null;
            if (!IsPotentialGetValueCall(invocation))
            {
                return false;
            }

            var methodSymbol = semanticModel.GetSymbolSafe(invocation, cancellationToken) as IMethodSymbol;
            if (methodSymbol != KnownSymbol.DependencyObject.GetValue ||
                methodSymbol?.Parameters.Length != 1)
            {
                return false;
            }

            property = invocation.ArgumentList.Arguments[0];
            field = semanticModel.GetSymbolSafe(property.Expression, cancellationToken) as IFieldSymbol;
            return true;
        }

        /// <summary>
        /// Check if <paramref name="invocation"/> is a call to dependencyObject.SetValue(FooProperty, value)
        /// </summary>
        internal static bool TryGetSetValueArguments(InvocationExpressionSyntax invocation, SemanticModel semanticModel, CancellationToken cancellationToken, out ArgumentListSyntax arguments)
        {
            arguments = null;
            if (!IsPotentialSetValueCall(invocation))
            {
                return false;
            }

            var setter = semanticModel.GetSymbolSafe(invocation, cancellationToken) as IMethodSymbol;
            if (setter != KnownSymbol.DependencyObject.SetValue ||
                setter?.Parameters.Length != 2)
            {
                return false;
            }

            arguments = invocation.ArgumentList;
            return true;
        }

        /// <summary>
        /// Check if <paramref name="invocation"/> is a call to dependencyObject.SetValue(FooProperty, value)
        /// </summary>
        /// <param name="property">The DependencyProperty used as argument</param>
        /// <param name="field">The field symbol for <paramref name="property"/></param>
        /// <param name="value">The value argument</param>
        internal static bool TryGetSetValueArguments(InvocationExpressionSyntax invocation, SemanticModel semanticModel, CancellationToken cancellationToken, out ArgumentSyntax property, out IFieldSymbol field, out ArgumentSyntax value)
        {
            if (TryGetSetValueArguments(invocation, semanticModel, cancellationToken, out ArgumentListSyntax argumentList))
            {
                property = argumentList.Arguments[0];
                value = argumentList.Arguments[1];
                field = semanticModel.GetSymbolSafe(property.Expression, cancellationToken) as IFieldSymbol;
                return true;
            }

            property = null;
            value = null;
            field = null;
            return false;
        }

        /// <summary>
        /// Check if <paramref name="invocation"/> is a call to dependencyObject.SetCurrentValue(FooProperty, value)
        /// </summary>
        internal static bool TryGetSetCurrentValueArguments(InvocationExpressionSyntax invocation, SemanticModel semanticModel, CancellationToken cancellationToken, out ArgumentListSyntax result)
        {
            result = null;
            if (!IsPotentialSetCurrentValueCall(invocation))
            {
                return false;
            }

            var methodSymbol = semanticModel.GetSymbolSafe(invocation, cancellationToken) as IMethodSymbol;
            if (methodSymbol != KnownSymbol.DependencyObject.SetCurrentValue ||
                methodSymbol?.Parameters.Length != 2)
            {
                return false;
            }

            result = invocation.ArgumentList;
            return true;
        }

        /// <summary>
        /// Check if <paramref name="invocation"/> is a call to dependencyObject.SetCurrentValue(FooProperty, value)
        /// </summary>
        /// <param name="property">The DependencyProperty used as argument</param>
        /// <param name="field">The field symbol for <paramref name="property"/></param>
        /// <param name="value">The value argument</param>
        internal static bool TryGetSetCurrentValueArguments(InvocationExpressionSyntax invocation, SemanticModel semanticModel, CancellationToken cancellationToken, out ArgumentSyntax property, out IFieldSymbol field, out ArgumentSyntax value)
        {
            if (TryGetSetCurrentValueArguments(invocation, semanticModel, cancellationToken, out ArgumentListSyntax argumentList))
            {
                property = argumentList.Arguments[0];
                value = argumentList.Arguments[1];
                field = semanticModel.GetSymbolSafe(property.Expression, cancellationToken) as IFieldSymbol;
                return true;
            }

            property = null;
            value = null;
            field = null;
            return false;
        }
    }
}