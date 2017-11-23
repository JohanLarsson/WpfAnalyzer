﻿// ReSharper disable InconsistentNaming
namespace WpfAnalyzers
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ImplementValueConverterCodeFixProvider))]
    [Shared]
    internal class ImplementValueConverterCodeFixProvider : CodeFixProvider
    {
        private static readonly MethodDeclarationSyntax IValueConverterConvert = ParseMethod(
            @"        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }");

        private static readonly MethodDeclarationSyntax IValueConverterConvertBack = ParseMethod(
            @"        object System.Windows.Data.IValueConverter.ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new System.NotSupportedException($""{nameof(FooConverter)} can only be used in OneWay bindings"");
        }");

        private static readonly MethodDeclarationSyntax IMultiValueConverterConvert = ParseMethod(
            @"        public object Convert(object[] values, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }");

        private static readonly MethodDeclarationSyntax IMultiValueConverterConvertBack = ParseMethod(
            @"        object[] System.Windows.Data.IMultiValueConverter.ConvertBack(object value, System.Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new System.NotSupportedException($""{ nameof(FooConverter) } can only be used in OneWay bindings"");
        }");

        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create("CS0535");

        /// <inheritdoc/>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var syntaxRoot = await document.GetSyntaxRootAsync(context.CancellationToken)
                                           .ConfigureAwait(false);
            foreach (var diagnostic in context.Diagnostics)
            {
                var token = syntaxRoot.FindToken(diagnostic.Location.SourceSpan.Start);
                if (string.IsNullOrEmpty(token.ValueText))
                {
                    continue;
                }

                var classDeclaration = syntaxRoot.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<ClassDeclarationSyntax>();
                if (classDeclaration == null)
                {
                    continue;
                }

                if (HasInterface(classDeclaration, KnownSymbol.IValueConverter))
                {
                    if (diagnostic.GetMessage(CultureInfo.InvariantCulture)
                                  .Contains("does not implement interface member 'IValueConverter.Convert(object, Type, object, CultureInfo)'"))
                    {
                        context.RegisterDocumentEditorFix(
                            "Implement IValueConverter.Convert for one way bindings.",
                            (editor, _) => editor.AddMethod(classDeclaration, IValueConverterConvert),
                            "Implement IValueConverter",
                            diagnostic);
                    }

                    if (diagnostic.GetMessage(CultureInfo.InvariantCulture)
                                  .Contains("does not implement interface member 'IValueConverter.ConvertBack(object, Type, object, CultureInfo)'"))
                    {
                        context.RegisterDocumentEditorFix(
                            "Implement IValueConverter.ConvertBack for one way bindings.",
                            (editor, _) => editor.AddMethod(classDeclaration, IValueConverterConvertBack),
                            "Implement IValueConverter",
                            diagnostic);
                    }
                }

                if (HasInterface(classDeclaration, KnownSymbol.IMultiValueConverter))
                {
                    if (diagnostic.GetMessage(CultureInfo.InvariantCulture)
                                  .Contains("does not implement interface member 'IMultiValueConverter.Convert(object[], Type, object, CultureInfo)'"))
                    {
                        context.RegisterDocumentEditorFix(
                            "Implement IMultiValueConverter.Convert for one way bindings.",
                            (editor, _) => editor.AddMethod(classDeclaration, IMultiValueConverterConvert),
                            "Implement IMultiValueConverter",
                            diagnostic);
                    }

                    if (diagnostic.GetMessage(CultureInfo.InvariantCulture)
                                  .Contains("does not implement interface member 'IMultiValueConverter.ConvertBack(object, Type[], object, CultureInfo)'"))
                    {
                        context.RegisterDocumentEditorFix(
                            "Implement IMultiValueConverter.ConvertBack for one way bindings.",
                            (editor, _) => editor.AddMethod(classDeclaration, IMultiValueConverterConvertBack),
                            "Implement IMultiValueConverter",
                            diagnostic);
                    }
                }
            }
        }

        private static bool HasInterface(ClassDeclarationSyntax classDeclaration, QualifiedType type)
        {
            if (classDeclaration.BaseList == null)
            {
                return false;
            }

            foreach (var typeSyntax in classDeclaration.BaseList.Types)
            {
                if (typeSyntax.Type is SimpleNameSyntax name &&
                    name.Identifier.ValueText == type.Type)
                {
                    return true;
                }

                if (typeSyntax.Type is QualifiedNameSyntax qualifiedName &&
                    qualifiedName.Right is SimpleNameSyntax simpleName &&
                    simpleName.Identifier.ValueText == type.Type)
                {
                    return true;
                }
            }

            return false;
        }

        private static MethodDeclarationSyntax ParseMethod(string code)
        {
            return (MethodDeclarationSyntax)SyntaxFactory.ParseCompilationUnit(code)
                                                         .Members
                                                         .Single()
                                                         .WithSimplifiedNames()
                                                         .WithLeadingTrivia(SyntaxFactory.ElasticMarker)
                                                         .WithTrailingTrivia(SyntaxFactory.ElasticMarker);
        }
    }
}