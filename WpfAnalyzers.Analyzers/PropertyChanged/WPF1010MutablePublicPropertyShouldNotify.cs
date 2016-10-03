﻿namespace WpfAnalyzers.PropertyChanged
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;
    using WpfAnalyzers.DependencyProperties;
    using WpfAnalyzers.PropertyChanged.Helpers;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class WPF1010MutablePublicPropertyShouldNotify : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "WPF1010";
        private const string Title = "Mutable public property should notify.";
        private const string MessageFormat = "Property '{0}' must notify when value changes.";
        private const string Description = "All mutable public properties should notify when their value changes.";
        private static readonly string HelpLink = WpfAnalyzers.HelpLink.ForId(DiagnosticId);

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            AnalyzerCategory.DependencyProperties,
            DiagnosticSeverity.Warning,
            AnalyzerConstants.EnabledByDefault,
            Description,
            HelpLink);

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(HandleDeclaration, SyntaxKind.PropertyDeclaration);
        }

        private static void HandleDeclaration(SyntaxNodeAnalysisContext context)
        {
            var declaration = (PropertyDeclarationSyntax)context.Node;
            if (declaration.IsMissing ||
                !declaration.Modifiers.Any(SyntaxKind.PublicKeyword) ||
                declaration.Modifiers.Any(SyntaxKind.StaticKeyword))
            {
                return;
            }

            var declaringType = declaration.DeclaringType();
            if (declaringType is StructDeclarationSyntax ||
                !declaringType.Modifiers.Any(SyntaxKind.PublicKeyword))
            {
                return;
            }

            var propertySymbol = (IPropertySymbol)context.ContainingSymbol;
            if (propertySymbol.IsIndexer ||
                propertySymbol.IsReadOnly ||
                propertySymbol.IsAbstract)
            {
                return;
            }

            if (IsAutoProperty(declaration))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, declaration.Identifier.GetLocation(), context.ContainingSymbol.Name));
                return;
            }

            AccessorDeclarationSyntax setter;
            if (declaration.TryGetSetAccessorDeclaration(out setter))
            {
                if (declaration.IsDependencyPropertyAccessor())
                {
                    return;
                }

                foreach (var name in setter.RaisesPropertyChangedFor(context.SemanticModel))
                {
                    if (string.IsNullOrEmpty(name) ||
                        name == propertySymbol.Name)
                    {
                        // raising with null or empty means that all properties notifies
                        // this is how a wpf binding sees it.
                        return;
                    }
                }
                
                context.ReportDiagnostic(Diagnostic.Create(Descriptor, declaration.Identifier.GetLocation(), context.ContainingSymbol.Name));
            }
        }

        private static bool IsAutoProperty(PropertyDeclarationSyntax property)
        {
            var accessors = property?.AccessorList?.Accessors;
            if (accessors?.Count != 2)
            {
                return false;
            }

            foreach (var accessor in accessors.Value)
            {
                if (accessor.Body != null)
                {
                    return false;
                }
            }

            return true;
        }
    }
}