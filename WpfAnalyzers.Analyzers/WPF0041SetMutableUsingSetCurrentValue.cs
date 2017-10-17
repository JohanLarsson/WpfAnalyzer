﻿namespace WpfAnalyzers
{
    using System.Collections.Immutable;
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class WPF0041SetMutableUsingSetCurrentValue : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "WPF0041";

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "Set mutable dependency properties using SetCurrentValue.",
            messageFormat: "Use SetCurrentValue({0}, {1})",
            category: AnalyzerCategory.DependencyProperties,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: AnalyzerConstants.EnabledByDefault,
            description: "Prefer setting mutable dependency properties using SetCurrentValue.",
            helpLinkUri: HelpLink.ForId(DiagnosticId));

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(HandleAssignment, SyntaxKind.SimpleAssignmentExpression);
            context.RegisterSyntaxNodeAction(HandleInvocation, SyntaxKind.InvocationExpression);
        }

        private static void HandleAssignment(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (IsInObjectInitializer(context.Node) ||
                IsInConstructor(context.Node))
            {
                return;
            }

            var assignment = context.Node as AssignmentExpressionSyntax;
            if (assignment == null ||
                assignment.IsMissing ||
                context.SemanticModel == null)
            {
                return;
            }

            var property = context.SemanticModel.GetSymbolSafe(assignment.Left, context.CancellationToken) as IPropertySymbol;
            if (property == KnownSymbol.FrameworkElement.DataContext)
            {
                return;
            }

            if (ClrProperty.TryGetSingleBackingField(property, context.SemanticModel, context.CancellationToken, out IFieldSymbol field))
            {
                if (IsCalleePotentiallyCreatedInScope(assignment.Left as MemberAccessExpressionSyntax, context.SemanticModel, context.CancellationToken))
                {
                    return;
                }

                var propertyArg = DependencyProperty.CreateArgument(field, context.SemanticModel, context.Node.SpanStart);
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, assignment.GetLocation(), propertyArg, assignment.Right));
            }
        }

        private static void HandleInvocation(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            var invocation = context.Node as InvocationExpressionSyntax;
            if (invocation == null || context.SemanticModel == null)
            {
                return;
            }

            if (IsInObjectInitializer(context.Node) ||
                IsInConstructor(context.Node))
            {
                return;
            }

            if (!DependencyObject.TryGetSetValueArguments(invocation, context.SemanticModel, context.CancellationToken, out ArgumentSyntax property, out IFieldSymbol setField, out ArgumentSyntax value))
            {
                return;
            }

            if (setField == null ||
                setField.Type != KnownSymbol.DependencyProperty ||
                setField == KnownSymbol.FrameworkElement.DataContextProperty)
            {
                return;
            }

            var clrProperty = context.ContainingProperty();
            if (ClrProperty.IsDependencyPropertyAccessor(clrProperty, context.SemanticModel, context.CancellationToken))
            {
                return;
            }

            var clrMethod = context.ContainingSymbol as IMethodSymbol;
            if (ClrMethod.IsAttachedSetMethod(clrMethod, context.SemanticModel, context.CancellationToken, out setField))
            {
                return;
            }

            if (IsCalleePotentiallyCreatedInScope(invocation.Expression as MemberAccessExpressionSyntax, context.SemanticModel, context.CancellationToken))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Descriptor, invocation.GetLocation(), property, value));
        }

        private static bool IsCalleePotentiallyCreatedInScope(MemberAccessExpressionSyntax memberAccess, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (memberAccess == null ||
                !memberAccess.IsKind(SyntaxKind.SimpleMemberAccessExpression) ||
                memberAccess.Expression.IsKind(SyntaxKind.ThisExpression))
            {
                return false;
            }

            var callee = memberAccess.Expression as IdentifierNameSyntax;
            if (callee == null)
            {
                return false;
            }

            var symbol = semanticModel.GetSymbolSafe(callee, cancellationToken);
            if (symbol.Kind != SymbolKind.Local)
            {
                return false;
            }

            if (!symbol.DeclaringSyntaxReferences.TryGetSingle(out SyntaxReference reference))
            {
                return false;
            }

            var declarator = reference.GetSyntax(cancellationToken) as VariableDeclaratorSyntax;
            var objectCreation = declarator?.Initializer?.Value as ObjectCreationExpressionSyntax;
            if (objectCreation == null)
            {
                return false;
            }

            return true;
        }

        private static bool IsInObjectInitializer(SyntaxNode node)
        {
            return node.Parent.IsKind(SyntaxKind.ObjectInitializerExpression);
        }

        private static bool IsInConstructor(SyntaxNode node)
        {
            var statement = node.Parent as StatementSyntax;
            var blockSyntax = statement?.Parent as BlockSyntax;
            if (blockSyntax == null)
            {
                return false;
            }

            return blockSyntax.Parent.IsKind(SyntaxKind.ConstructorDeclaration);
        }
    }
}