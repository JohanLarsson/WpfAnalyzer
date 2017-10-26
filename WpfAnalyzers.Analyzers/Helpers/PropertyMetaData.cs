﻿namespace WpfAnalyzers
{
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class PropertyMetadata
    {
        internal static bool TryGetConstructor(ObjectCreationExpressionSyntax objectCreation, SemanticModel semanticModel, CancellationToken cancellationToken, out IMethodSymbol constructor)
        {
            return Constructor.TryGet(objectCreation, KnownSymbol.PropertyMetadata, semanticModel, cancellationToken, out constructor) ||
                   Constructor.TryGet(objectCreation, KnownSymbol.UIPropertyMetadata, semanticModel, cancellationToken, out constructor) ||
                   Constructor.TryGet(objectCreation, KnownSymbol.FrameworkPropertyMetadata, semanticModel, cancellationToken, out constructor);
        }

        internal static bool TryGetDefaultValue(ObjectCreationExpressionSyntax objectCreation, SemanticModel semanticModel, CancellationToken cancellationToken, out ArgumentSyntax defaultValueArg)
        {
            defaultValueArg = null;
            if (objectCreation?.ArgumentList == null ||
                objectCreation.ArgumentList.Arguments.Count == 0)
            {
                return false;
            }

            return TryGetConstructor(objectCreation, semanticModel, cancellationToken, out var constructor) &&
                   constructor.Parameters.TryGetFirst(out var parameter) &&
                   parameter.Type == KnownSymbol.Object &&
                   objectCreation.ArgumentList.Arguments.TryGetFirst(out defaultValueArg);
        }

        internal static bool TryGetPropertyChangedCallback(ObjectCreationExpressionSyntax objectCreation, SemanticModel semanticModel, CancellationToken cancellationToken, out ArgumentSyntax callback)
        {
            return TryGetCallback(objectCreation, KnownSymbol.PropertyChangedCallback, semanticModel, cancellationToken, out callback);
        }

        internal static bool TryGetCoerceValueCallback(ObjectCreationExpressionSyntax objectCreation, SemanticModel semanticModel, CancellationToken cancellationToken, out ArgumentSyntax callback)
        {
            return TryGetCallback(objectCreation, KnownSymbol.CoerceValueCallback, semanticModel, cancellationToken, out callback);
        }

        internal static bool TryGetRegisteredName(ObjectCreationExpressionSyntax objectCreation, SemanticModel semanticModel, CancellationToken cancellationToken, out string registeredName)
        {
            registeredName = null;
            return TryGetConstructor(objectCreation, semanticModel, cancellationToken, out _) &&
                   DependencyProperty.TryGetRegisteredName(objectCreation?.FirstAncestorOrSelf<InvocationExpressionSyntax>(), semanticModel, cancellationToken, out registeredName);
        }

        internal static bool TryGetDependencyProperty(ObjectCreationExpressionSyntax objectCreation, SemanticModel semanticModel, CancellationToken cancellationToken, out BackingFieldOrProperty dependencyProperty)
        {
            return BackingFieldOrProperty.TryCreate(semanticModel.GetDeclaredSymbolSafe(objectCreation.FirstAncestorOrSelf<FieldDeclarationSyntax>(), cancellationToken), out dependencyProperty) ||
                   BackingFieldOrProperty.TryCreate(semanticModel.GetDeclaredSymbolSafe(objectCreation.FirstAncestorOrSelf<PropertyDeclarationSyntax>(), cancellationToken), out dependencyProperty);
        }

        private static bool TryGetCallback(ObjectCreationExpressionSyntax objectCreation, QualifiedType callbackType, SemanticModel semanticModel, CancellationToken cancellationToken, out ArgumentSyntax callback)
        {
            callback = null;
            if (objectCreation?.ArgumentList == null ||
                objectCreation.ArgumentList.Arguments.Count == 0)
            {
                return false;
            }

            return TryGetConstructor(objectCreation, semanticModel, cancellationToken, out var constructor) &&
                   Argument.TryGetArgument(constructor.Parameters, objectCreation.ArgumentList, callbackType, out callback);
        }

        public static bool TryFindObjectCreationAncestor(SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken, out ObjectCreationExpressionSyntax objectCreation)
        {
            objectCreation = null;
            var parent = node?.Parent;
            while (parent != null)
            {
                if (parent is ObjectCreationExpressionSyntax candidate &&
                    TryGetConstructor(candidate, semanticModel, cancellationToken, out _))
                {
                    objectCreation = candidate;
                    return true;
                }

                parent = parent.Parent;
            }

            return false;
        }
    }
}
