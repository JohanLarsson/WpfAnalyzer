﻿namespace WpfAnalyzers.Refactorings
{
    using System;
    using System.Composition;
    using System.Threading;
    using System.Threading.Tasks;

    using Gu.Roslyn.AnalyzerExtensions;
    using Gu.Roslyn.CodeFixExtensions;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeRefactorings;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(AutoPropertyRefactoring))]
    [Shared]
    internal class AutoPropertyRefactoring : CodeRefactoringProvider
    {
        private static readonly UsingDirectiveSyntax SystemWindows = SyntaxFactory.UsingDirective(
            name: SyntaxFactory.QualifiedName(
                left: SyntaxFactory.IdentifierName("System"),
                right: SyntaxFactory.IdentifierName("Windows")));

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken)
                                             .ConfigureAwait(false);

            if (syntaxRoot.FindNode(context.Span) is { } node &&
                node.FirstAncestorOrSelf<PropertyDeclarationSyntax>() is { } property &&
                property.IsAutoProperty() &&
                property.Parent is ClassDeclarationSyntax containingClass &&
                semanticModel is { })
            {
                if (property.Modifiers.Any(SyntaxKind.StaticKeyword))
                {
                    context.RegisterRefactoring(
                        CodeAction.Create(
                            "Change to attached dependency property",
                            _ => WithAttachedProperty(),
                            "Change to attached dependency property"));

                    Task<Document> WithAttachedProperty()
                    {
                        var updatedClass = containingClass.RemoveNode(property, SyntaxRemoveOptions.KeepUnbalancedDirectives)
                                                          .AddMethod(GetMethod(property))
                                                          .AddMethod(SetMethod(property))
                                                          .AddField(
                                                              Field(
                                                                  withStandardDocs: false,
                                                                  KnownSymbols.DependencyProperty,
                                                                  property,
                                                                  Register(
                                                                      "RegisterAttached",
                                                                      SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(property.Identifier.ValueText)),
                                                                      property.Type,
                                                                      containingClass)));

                        if (syntaxRoot is CompilationUnitSyntax compilationUnit)
                        {
                            return Task.FromResult(
                                context.Document.WithSyntaxRoot(
                                    compilationUnit.ReplaceNode(containingClass, updatedClass)
                                                   .AddUsing(SystemWindows, semanticModel!)));
                        }

                        return Task.FromResult(
                            context.Document.WithSyntaxRoot(
                                syntaxRoot.ReplaceNode(containingClass, updatedClass)));
                    }

                    static MethodDeclarationSyntax GetMethod(PropertyDeclarationSyntax property)
                    {
                        return SyntaxFactory.MethodDeclaration(
                            attributeLists: SyntaxFactory.SingletonList(
                                SyntaxFactory.AttributeList(
                                    openBracketToken: SyntaxFactory.Token(
                                        leading: SyntaxFactory.TriviaList(
                                            SyntaxFactory.Trivia(
                                                SyntaxFactory.DocumentationCommentTrivia(
                                                    kind: SyntaxKind.SingleLineDocumentationCommentTrivia,
                                                    content: SyntaxFactory.List(
                                                        new XmlNodeSyntax[]
                                                        {
                                                            SyntaxFactory.XmlText(
                                                                textTokens: SyntaxFactory.TokenList(
                                                                    SyntaxFactory.XmlTextLiteral(
                                                                        leading: SyntaxFactory.TriviaList(
                                                                            SyntaxFactory.DocumentationCommentExterior("///")),
                                                                        text: " ",
                                                                        value: " ",
                                                                        trailing: default))),
                                                            SyntaxFactory.XmlElement(
                                                                startTag: SyntaxFactory.XmlElementStartTag(
                                                                    SyntaxFactory.XmlName("summary")),
                                                                content: SyntaxFactory.List(
                                                                    new XmlNodeSyntax[]
                                                                    {
                                                                        SyntaxFactory.XmlText("Helper for getting "),
                                                                        SyntaxFactory.XmlEmptyElement(
                                                                            name: SyntaxFactory.XmlName("see"),
                                                                            attributes: SyntaxFactory.SingletonList<XmlAttributeSyntax>(
                                                                                SyntaxFactory.XmlCrefAttribute(
                                                                                    cref: SyntaxFactory.NameMemberCref(
                                                                                        name: SyntaxFactory.IdentifierName(property.Identifier.ValueText + "Property"))))),
                                                                        SyntaxFactory.XmlText(" from "),
                                                                        SyntaxFactory.XmlEmptyElement(
                                                                            name: SyntaxFactory.XmlName("paramref"),
                                                                            attributes: SyntaxFactory.SingletonList<XmlAttributeSyntax>(
                                                                                SyntaxFactory.XmlNameAttribute("element"))),
                                                                        SyntaxFactory.XmlText("."),
                                                                    }),
                                                                endTag: SyntaxFactory.XmlElementEndTag(
                                                                    name: SyntaxFactory.XmlName("summary"))),
                                                            SyntaxFactory.XmlText(
                                                                textTokens: SyntaxFactory.TokenList(
                                                                    SyntaxFactory.XmlTextNewLine(
                                                                        leading: default,
                                                                        text: "\r\n",
                                                                        value: "\r\n",
                                                                        trailing: default),
                                                                    SyntaxFactory.XmlTextLiteral(
                                                                        leading: SyntaxFactory.TriviaList(
                                                                            SyntaxFactory.DocumentationCommentExterior("///")),
                                                                        text: " ",
                                                                        value: " ",
                                                                        trailing: default))),
                                                            SyntaxFactory.XmlElement(
                                                                startTag: SyntaxFactory.XmlElementStartTag(
                                                                    name: SyntaxFactory.XmlName("param"),
                                                                    attributes: SyntaxFactory.SingletonList<XmlAttributeSyntax>(
                                                                        SyntaxFactory.XmlNameAttribute("element"))),
                                                                content: SyntaxFactory.List(
                                                                    new XmlNodeSyntax[]
                                                                    {
                                                                        SyntaxFactory.XmlEmptyElement(
                                                                            name: SyntaxFactory.XmlName("see"),
                                                                            attributes: SyntaxFactory.SingletonList<XmlAttributeSyntax>(
                                                                                SyntaxFactory.XmlCrefAttribute(
                                                                                    cref: SyntaxFactory.NameMemberCref(
                                                                                        name: SyntaxFactory.IdentifierName("DependencyObject"))))),
                                                                        SyntaxFactory.XmlText(" to read "),
                                                                        SyntaxFactory.XmlEmptyElement(
                                                                            name: SyntaxFactory.XmlName("see"),
                                                                            attributes: SyntaxFactory.SingletonList<XmlAttributeSyntax>(
                                                                                SyntaxFactory.XmlCrefAttribute(
                                                                                    cref: SyntaxFactory.NameMemberCref(
                                                                                        name: SyntaxFactory.IdentifierName(property.Identifier.ValueText + "Property"))))),
                                                                        SyntaxFactory.XmlText(" from."),
                                                                    }),
                                                                endTag: SyntaxFactory.XmlElementEndTag(
                                                                    name: SyntaxFactory.XmlName("param"))),
                                                            SyntaxFactory.XmlText(
                                                                textTokens: SyntaxFactory.TokenList(
                                                                    SyntaxFactory.XmlTextNewLine(
                                                                        leading: default,
                                                                        text: "\r\n",
                                                                        value: "\r\n",
                                                                        trailing: default),
                                                                    SyntaxFactory.XmlTextLiteral(
                                                                        leading: SyntaxFactory.TriviaList(
                                                                            SyntaxFactory.DocumentationCommentExterior("///")),
                                                                        text: " ",
                                                                        value: " ",
                                                                        trailing: default))),
                                                            SyntaxFactory.XmlElement(
                                                                startTag: SyntaxFactory.XmlElementStartTag(
                                                                    name: SyntaxFactory.XmlName("returns")),
                                                                content: SyntaxFactory.SingletonList<XmlNodeSyntax>(
                                                                    SyntaxFactory.XmlText("Style property value.")),
                                                                endTag: SyntaxFactory.XmlElementEndTag(
                                                                    name: SyntaxFactory.XmlName("returns"))),
                                                            SyntaxFactory.XmlText(
                                                                textTokens: SyntaxFactory.TokenList(
                                                                    SyntaxFactory.XmlTextNewLine(
                                                                        leading: default,
                                                                        text: "\r\n",
                                                                        value: "\r\n",
                                                                        trailing: default))),
                                                        }),
                                                    endOfComment: SyntaxFactory.Token(SyntaxKind.EndOfDocumentationCommentToken)))),
                                        kind: SyntaxKind.OpenBracketToken,
                                        trailing: default),
                                    target: default,
                                    attributes: SyntaxFactory.SingletonSeparatedList<AttributeSyntax>(
                                        SyntaxFactory.Attribute(
                                            name: SyntaxFactory.IdentifierName(
                                                identifier: SyntaxFactory.Identifier(
                                                    leading: default,
                                                    text: "AttachedPropertyBrowsableForType",
                                                    trailing: default)),
                                            argumentList: SyntaxFactory.AttributeArgumentList(
                                                arguments: SyntaxFactory.SingletonSeparatedList<AttributeArgumentSyntax>(
                                                    SyntaxFactory.AttributeArgument(
                                                        expression: SyntaxFactory.TypeOfExpression(
                                                            type: SyntaxFactory.IdentifierName("DependencyObject"))))))),
                                    closeBracketToken: SyntaxFactory.Token(
                                        leading: default,
                                        kind: SyntaxKind.CloseBracketToken,
                                        trailing: SyntaxFactory.TriviaList(SyntaxFactory.LineFeed)))),
                            modifiers: SyntaxFactory.TokenList(
                                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                                SyntaxFactory.Token(SyntaxKind.StaticKeyword)),
                            returnType: property.Type,
                            explicitInterfaceSpecifier: default,
                            identifier: SyntaxFactory.Identifier("Get" + property.Identifier.ValueText),
                            typeParameterList: default,
                            parameterList: SyntaxFactory.ParameterList(
                                parameters: SyntaxFactory.SingletonSeparatedList<ParameterSyntax>(
                                    SyntaxFactory.Parameter(
                                        attributeLists: default,
                                        modifiers: default,
                                        type: SyntaxFactory.IdentifierName("DependencyObject"),
                                        identifier: SyntaxFactory.Identifier("element"),
                                        @default: default))),
                            constraintClauses: default,
                            body: SyntaxFactory.Block(
                                statements: SyntaxFactory.SingletonList<StatementSyntax>(
                                    SyntaxFactory.ReturnStatement(
                                        expression: SyntaxFactory.CastExpression(
                                            type: property.Type,
                                            expression: SyntaxFactory.InvocationExpression(
                                                expression: SyntaxFactory.MemberAccessExpression(
                                                    kind: SyntaxKind.SimpleMemberAccessExpression,
                                                    expression: SyntaxFactory.IdentifierName("element"),
                                                    name: SyntaxFactory.IdentifierName("GetValue")),
                                                argumentList: SyntaxFactory.ArgumentList(
                                                    arguments: SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(
                                                        SyntaxFactory.Argument(
                                                            expression: SyntaxFactory.IdentifierName(property.Identifier.ValueText + "Property"))))))))),
                            expressionBody: default,
                            semicolonToken: default);
                    }

                    static MethodDeclarationSyntax SetMethod(PropertyDeclarationSyntax property)
                    {
                        return SyntaxFactory.MethodDeclaration(
                            attributeLists: default,
                            modifiers: SyntaxFactory.TokenList(
                                SyntaxFactory.Token(
                                    leading: SyntaxFactory.TriviaList(
                                        SyntaxFactory.Whitespace("        "),
                                        SyntaxFactory.Trivia(
                                            SyntaxFactory.DocumentationCommentTrivia(
                                                kind: SyntaxKind.SingleLineDocumentationCommentTrivia,
                                                content: SyntaxFactory.List(
                                                    new XmlNodeSyntax[]
                                                    {
                                                        SyntaxFactory.XmlText(
                                                            textTokens: SyntaxFactory.TokenList(
                                                                SyntaxFactory.XmlTextLiteral(
                                                                    leading: SyntaxFactory.TriviaList(
                                                                        SyntaxFactory.DocumentationCommentExterior("///")),
                                                                    text: " ",
                                                                    value: " ",
                                                                    trailing: default))),
                                                        SyntaxFactory.XmlElement(
                                                            startTag: SyntaxFactory.XmlElementStartTag(
                                                                SyntaxFactory.XmlName("summary")),
                                                            content: SyntaxFactory.List(
                                                                new XmlNodeSyntax[]
                                                                {
                                                                    SyntaxFactory.XmlText(
                                                                        textTokens: SyntaxFactory.TokenList(
                                                                            SyntaxFactory.XmlTextLiteral("Helper for setting "))),
                                                                    SyntaxFactory.XmlEmptyElement(
                                                                        lessThanToken: SyntaxFactory.Token(SyntaxKind.LessThanToken),
                                                                        name: SyntaxFactory.XmlName(
                                                                            prefix: default,
                                                                            localName: SyntaxFactory.Identifier(
                                                                                leading: default,
                                                                                text: "see",
                                                                                trailing: default)),
                                                                        attributes: SyntaxFactory.SingletonList<XmlAttributeSyntax>(
                                                                            SyntaxFactory.XmlCrefAttribute(
                                                                                cref: SyntaxFactory.NameMemberCref(
                                                                                    name: SyntaxFactory.IdentifierName(property.Identifier.ValueText + "Property")))),
                                                                        slashGreaterThanToken: SyntaxFactory.Token(SyntaxKind.SlashGreaterThanToken)),
                                                                    SyntaxFactory.XmlText(" on "),
                                                                    SyntaxFactory.XmlEmptyElement(
                                                                        lessThanToken: SyntaxFactory.Token(SyntaxKind.LessThanToken),
                                                                        name: SyntaxFactory.XmlName("paramref"),
                                                                        attributes: SyntaxFactory.SingletonList<XmlAttributeSyntax>(
                                                                            SyntaxFactory.XmlNameAttribute("element")),
                                                                        slashGreaterThanToken: SyntaxFactory.Token(SyntaxKind.SlashGreaterThanToken)),
                                                                    SyntaxFactory.XmlText(
                                                                        textTokens: SyntaxFactory.TokenList(
                                                                            SyntaxFactory.XmlTextLiteral("."))),
                                                                }),
                                                            endTag: SyntaxFactory.XmlElementEndTag(
                                                                SyntaxFactory.XmlName("summary"))),
                                                        SyntaxFactory.XmlText(
                                                            textTokens: SyntaxFactory.TokenList(
                                                                SyntaxFactory.XmlTextNewLine(
                                                                    leading: default,
                                                                    text: "\r\n",
                                                                    value: "\r\n",
                                                                    trailing: default),
                                                                SyntaxFactory.XmlTextLiteral(
                                                                    leading: SyntaxFactory.TriviaList(
                                                                        SyntaxFactory.DocumentationCommentExterior("///")),
                                                                    text: " ",
                                                                    value: " ",
                                                                    trailing: default))),
                                                        SyntaxFactory.XmlElement(
                                                            startTag: SyntaxFactory.XmlElementStartTag(
                                                                name: SyntaxFactory.XmlName("param"),
                                                                attributes: SyntaxFactory.SingletonList<XmlAttributeSyntax>(
                                                                    SyntaxFactory.XmlNameAttribute("element"))),
                                                            content: SyntaxFactory.List(
                                                                new XmlNodeSyntax[]
                                                                {
                                                                    SyntaxFactory.XmlEmptyElement(
                                                                        lessThanToken: SyntaxFactory.Token(SyntaxKind.LessThanToken),
                                                                        name: SyntaxFactory.XmlName("see"),
                                                                        attributes: SyntaxFactory.SingletonList<XmlAttributeSyntax>(
                                                                            SyntaxFactory.XmlCrefAttribute(
                                                                                cref: SyntaxFactory.NameMemberCref(
                                                                                    name: SyntaxFactory.IdentifierName("DependencyObject")))),
                                                                        slashGreaterThanToken: SyntaxFactory.Token(SyntaxKind.SlashGreaterThanToken)),
                                                                    SyntaxFactory.XmlText(" to set "),
                                                                    SyntaxFactory.XmlEmptyElement(
                                                                        lessThanToken: SyntaxFactory.Token(SyntaxKind.LessThanToken),
                                                                        name: SyntaxFactory.XmlName("see"),
                                                                        attributes: SyntaxFactory.SingletonList<XmlAttributeSyntax>(
                                                                            SyntaxFactory.XmlCrefAttribute(
                                                                                cref: SyntaxFactory.NameMemberCref(
                                                                                    name: SyntaxFactory.IdentifierName(
                                                                                        identifier: SyntaxFactory.Identifier(
                                                                                            leading: default,
                                                                                            text: property.Identifier.ValueText + "Property",
                                                                                            trailing: default)),
                                                                                    parameters: default))),
                                                                        slashGreaterThanToken: SyntaxFactory.Token(SyntaxKind.SlashGreaterThanToken)),
                                                                    SyntaxFactory.XmlText(" on."),
                                                                }),
                                                            endTag: SyntaxFactory.XmlElementEndTag(
                                                                SyntaxFactory.XmlName("param"))),
                                                        SyntaxFactory.XmlText(
                                                            textTokens: SyntaxFactory.TokenList(
                                                                SyntaxFactory.XmlTextNewLine(
                                                                    leading: default,
                                                                    text: "\r\n",
                                                                    value: "\r\n",
                                                                    trailing: default),
                                                                SyntaxFactory.XmlTextLiteral(
                                                                    leading: SyntaxFactory.TriviaList(
                                                                        SyntaxFactory.DocumentationCommentExterior("///")),
                                                                    text: " ",
                                                                    value: " ",
                                                                    trailing: default))),
                                                        SyntaxFactory.XmlElement(
                                                            startTag: SyntaxFactory.XmlElementStartTag(
                                                                name: SyntaxFactory.XmlName("param"),
                                                                attributes: SyntaxFactory.SingletonList<XmlAttributeSyntax>(
                                                                    SyntaxFactory.XmlNameAttribute("value"))),
                                                            content: SyntaxFactory.SingletonList<XmlNodeSyntax>(
                                                                SyntaxFactory.XmlText(property.Identifier.ValueText + " property value.")),
                                                            endTag: SyntaxFactory.XmlElementEndTag(
                                                                SyntaxFactory.XmlName("param"))),
                                                        SyntaxFactory.XmlText(
                                                            textTokens: SyntaxFactory.TokenList(
                                                                SyntaxFactory.XmlTextNewLine(
                                                                    leading: default,
                                                                    text: "\r\n",
                                                                    value: "\r\n",
                                                                    trailing: default))),
                                                    }),
                                                endOfComment: SyntaxFactory.Token(SyntaxKind.EndOfDocumentationCommentToken))),
                                        SyntaxFactory.Whitespace("        ")),
                                    kind: SyntaxKind.PublicKeyword,
                                    trailing: SyntaxFactory.TriviaList(SyntaxFactory.Space)),
                                SyntaxFactory.Token(
                                    leading: default,
                                    kind: SyntaxKind.StaticKeyword,
                                    trailing: SyntaxFactory.TriviaList(SyntaxFactory.Space))),
                            returnType: SyntaxFactory.PredefinedType(
                                keyword: SyntaxFactory.Token(
                                    leading: default,
                                    kind: SyntaxKind.VoidKeyword,
                                    trailing: SyntaxFactory.TriviaList(SyntaxFactory.Space))),
                            explicitInterfaceSpecifier: default,
                            identifier: SyntaxFactory.Identifier("Set" + property.Identifier.ValueText),
                            typeParameterList: default,
                            parameterList: SyntaxFactory.ParameterList(
                                parameters: SyntaxFactory.SeparatedList(
                                    new[]
                                    {
                                        SyntaxFactory.Parameter(
                                            attributeLists: default,
                                            modifiers: default,
                                            type: SyntaxFactory.IdentifierName("DependencyObject"),
                                            identifier: SyntaxFactory.Identifier("element"),
                                            @default: default),
                                        SyntaxFactory.Parameter(
                                            attributeLists: default,
                                            modifiers: default,
                                            type: property.Type,
                                            identifier: SyntaxFactory.Identifier("value"),
                                            @default: default),
                                    },
                                    new[]
                                    {
                                        SyntaxFactory.Token(
                                            leading: default,
                                            kind: SyntaxKind.CommaToken,
                                            trailing: SyntaxFactory.TriviaList(SyntaxFactory.Space)),
                                    })),
                            constraintClauses: default,
                            body: SyntaxFactory.Block(
                                statements: SyntaxFactory.SingletonList<StatementSyntax>(
                                    SyntaxFactory.ExpressionStatement(
                                        expression: SyntaxFactory.InvocationExpression(
                                            expression: SyntaxFactory.MemberAccessExpression(
                                                kind: SyntaxKind.SimpleMemberAccessExpression,
                                                expression: SyntaxFactory.IdentifierName(
                                                    identifier: SyntaxFactory.Identifier(
                                                        leading: SyntaxFactory.TriviaList(SyntaxFactory.Whitespace("            ")),
                                                        text: "element",
                                                        trailing: default)),
                                                name: SyntaxFactory.IdentifierName("SetValue")),
                                            argumentList: SyntaxFactory.ArgumentList(
                                                arguments: SyntaxFactory.SeparatedList(
                                                    new[]
                                                    {
                                                        SyntaxFactory.Argument(
                                                            expression: SyntaxFactory.IdentifierName(property.Identifier.ValueText + "Property")),
                                                        SyntaxFactory.Argument(
                                                            expression: SyntaxFactory.IdentifierName("value")),
                                                    },
                                                    new SyntaxToken[]
                                                    {
                                                        SyntaxFactory.Token(
                                                            leading: default,
                                                            kind: SyntaxKind.CommaToken,
                                                            trailing: SyntaxFactory.TriviaList(SyntaxFactory.Space)),
                                                    }))),
                                        semicolonToken: SyntaxFactory.Token(
                                            leading: default,
                                            kind: SyntaxKind.SemicolonToken,
                                            trailing: SyntaxFactory.TriviaList(SyntaxFactory.LineFeed))))),
                            expressionBody: default,
                            semicolonToken: default);
                    }
                }
                else if (semanticModel.TryGetType(containingClass, context.CancellationToken, out var containingType) &&
                         containingType.IsAssignableTo(KnownSymbols.DependencyObject, semanticModel.Compilation))
                {
                    context.RegisterRefactoring(
                        CodeAction.Create(
                            "Change to dependency property",
                            c => WithDependencyProperty(c),
                            "Change to dependency property"));

                    async Task<Document> WithDependencyProperty(CancellationToken cancellationToken)
                    {
                        var qualifyMethodAccess = await context.Document.QualifyMethodAccessAsync(cancellationToken)
                                                               .ConfigureAwait(false);
                        var updatedClass = containingClass.ReplaceNode(
                                                              property,
                                                              Property(
                                                                  property!.Identifier.ValueText + "Property",
                                                                  property.Identifier.ValueText + "Property",
                                                                  qualifyMethodAccess != CodeStyleResult.No))
                                                          .AddField(
                                                              Field(
                                                                  withStandardDocs: !Descriptors.WPF0060DocumentDependencyPropertyBackingMember.IsSuppressed(semanticModel!),
                                                                  KnownSymbols.DependencyProperty,
                                                                  property,
                                                                  Register("Register", Nameof(property), property.Type, containingClass)));

                        if (syntaxRoot is CompilationUnitSyntax compilationUnit)
                        {
                            return context.Document.WithSyntaxRoot(
                                compilationUnit.ReplaceNode(containingClass, updatedClass)
                                               .AddUsing(SystemWindows, semanticModel!));
                        }

                        return context.Document.WithSyntaxRoot(syntaxRoot.ReplaceNode(containingClass, updatedClass));
                    }

                    if (property.Setter() is { } setter &&
                        setter.Modifiers.Any())
                    {
                        context.RegisterRefactoring(
                            CodeAction.Create(
                                "Change to readonly dependency property",
                                c => WithReadOnlyDependencyProperty(c),
                                "Change to readonly dependency property"));

                        async Task<Document> WithReadOnlyDependencyProperty(CancellationToken cancellationToken)
                        {
                            var qualifyMethodAccess = await context.Document.QualifyMethodAccessAsync(cancellationToken)
                                                                   .ConfigureAwait(false);
                            var updatedClass = containingClass.ReplaceNode(
                                                                  property,
                                                                  Property(
                                                                      property!.Identifier.ValueText + "Property",
                                                                      property.Identifier.ValueText + "PropertyKey",
                                                                      qualifyMethodAccess != CodeStyleResult.No))
                                                              .AddField(
                                                                  Field(
                                                                      withStandardDocs: false,
                                                                      KnownSymbols.DependencyPropertyKey,
                                                                      property,
                                                                      Register("RegisterReadOnly", Nameof(property), property.Type, containingClass)))
                                                              .AddField(
                                                                  Field(
                                                                      withStandardDocs: !Descriptors.WPF0060DocumentDependencyPropertyBackingMember.IsSuppressed(semanticModel!),
                                                                      KnownSymbols.DependencyProperty,
                                                                      property,
                                                                      SyntaxFactory.MemberAccessExpression(
                                                                          SyntaxKind.SimpleMemberAccessExpression,
                                                                          SyntaxFactory.IdentifierName(property.Identifier.ValueText + "PropertyKey"),
                                                                          SyntaxFactory.IdentifierName("DependencyProperty"))));

                            if (syntaxRoot is CompilationUnitSyntax compilationUnit)
                            {
                                return context.Document.WithSyntaxRoot(
                                    compilationUnit.ReplaceNode(containingClass, updatedClass)
                                                   .AddUsing(SystemWindows, semanticModel!));
                            }

                            return context.Document.WithSyntaxRoot(syntaxRoot.ReplaceNode(containingClass, updatedClass));
                        }
                    }

                    PropertyDeclarationSyntax Property(string field, string keyField, bool qualifyMethodAccess)
                    {
                        return property.WithIdentifier(property.Identifier.WithTrailingTrivia(SyntaxFactory.LineFeed))
                                       .WithAccessorList(
                            SyntaxFactory.AccessorList(
                                openBraceToken: SyntaxFactory.Token(
                                    leading: SyntaxFactory.TriviaList(SyntaxFactory.Whitespace("        ")),
                                    kind: SyntaxKind.OpenBraceToken,
                                    trailing: SyntaxFactory.TriviaList(SyntaxFactory.LineFeed)),
                                accessors: SyntaxFactory.List(
                                    new[]
                                    {
                                    SyntaxFactory.AccessorDeclaration(
                                        kind: SyntaxKind.GetAccessorDeclaration,
                                        attributeLists: property.Getter()!.AttributeLists,
                                        modifiers: property.Getter()!.Modifiers,
                                        keyword: SyntaxFactory.Token(SyntaxKind.GetKeyword),
                                        body: default,
                                        expressionBody: SyntaxFactory.ArrowExpressionClause(
                                            expression: SyntaxFactory.CastExpression(
                                                type: property.Type,
                                                expression: SyntaxFactory.InvocationExpression(
                                                    expression: MethodAccess("GetValue"),
                                                    argumentList: SyntaxFactory.ArgumentList(
                                                        arguments: SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(
                                                            SyntaxFactory.Argument(
                                                                expression: SyntaxFactory.IdentifierName(field))))))),
                                        semicolonToken: SyntaxFactory.Token(
                                            leading: default,
                                            kind: SyntaxKind.SemicolonToken,
                                            trailing: SyntaxFactory.TriviaList(SyntaxFactory.LineFeed)))
                                                 .WithLeadingTrivia(SyntaxFactory.Whitespace("            ")),
                                    SyntaxFactory.AccessorDeclaration(
                                        kind: SyntaxKind.SetAccessorDeclaration,
                                        attributeLists: property.Setter()!.AttributeLists,
                                        modifiers: property.Setter()!.Modifiers,
                                        keyword: SyntaxFactory.Token(SyntaxKind.SetKeyword),
                                        body: default,
                                        expressionBody: SyntaxFactory.ArrowExpressionClause(
                                            expression: SyntaxFactory.InvocationExpression(
                                                expression: MethodAccess("SetValue"),
                                                argumentList: SyntaxFactory.ArgumentList(
                                                    arguments: SyntaxFactory.SeparatedList(
                                                        new[]
                                                        {
                                                            SyntaxFactory.Argument(
                                                                expression: SyntaxFactory.IdentifierName(keyField)),
                                                            SyntaxFactory.Argument(
                                                                expression: SyntaxFactory.IdentifierName("value")),
                                                        })))),
                                        semicolonToken: SyntaxFactory.Token(
                                            leading: default,
                                            kind: SyntaxKind.SemicolonToken,
                                            trailing: SyntaxFactory.TriviaList(SyntaxFactory.LineFeed)))
                                                 .WithLeadingTrivia(SyntaxFactory.Whitespace("            ")),
                                    }),
                                closeBraceToken: SyntaxFactory.Token(
                                    leading: SyntaxFactory.TriviaList(SyntaxFactory.Whitespace("        ")),
                                    kind: SyntaxKind.CloseBraceToken,
                                    trailing: SyntaxFactory.TriviaList(SyntaxFactory.LineFeed))));

                        ExpressionSyntax MethodAccess(string name)
                        {
                            return qualifyMethodAccess
                                ? (ExpressionSyntax)SyntaxFactory.MemberAccessExpression(
                                    kind: SyntaxKind.SimpleMemberAccessExpression,
                                    expression: SyntaxFactory.ThisExpression(),
                                    name: SyntaxFactory.IdentifierName(name))
                                : SyntaxFactory.IdentifierName(name);
                        }
                    }
                }

                static FieldDeclarationSyntax Field(bool withStandardDocs, QualifiedType type, PropertyDeclarationSyntax property, ExpressionSyntax value)
                {
                    var name = type switch
                    {
                        { Type: "DependencyProperty" } _ => property.Identifier.ValueText + "Property",
                        { Type: "DependencyPropertyKey" } _ => property.Identifier.ValueText + "PropertyKey",
                        _ => throw new InvalidOperationException(),
                    };

                    return SyntaxFactory.FieldDeclaration(
                        attributeLists: default,
                        modifiers: SyntaxFactory.TokenList(
                            PublicOrPrivate(),
                            SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                            SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)),
                        declaration: SyntaxFactory.VariableDeclaration(
                            type: SyntaxFactory.IdentifierName(type.Type),
                            variables: SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.VariableDeclarator(
                                    identifier: SyntaxFactory.Identifier(name),
                                    argumentList: default,
                                    initializer: SyntaxFactory.EqualsValueClause(
                                        value: value)))),
                        semicolonToken: SyntaxFactory.Token(default, SyntaxKind.SemicolonToken, default));

                    SyntaxToken PublicOrPrivate()
                    {
                        var keyword = type switch
                        {
                            { Type: "DependencyProperty" } _ => SyntaxKind.PublicKeyword,
                            { Type: "DependencyPropertyKey" } _ => SyntaxKind.PrivateKeyword,
                            _ => throw new InvalidOperationException(),
                        };

                        if (!withStandardDocs)
                        {
                            return SyntaxFactory.Token(default, keyword, SyntaxFactory.TriviaList(SyntaxFactory.Space));
                        }

                        return SyntaxFactory.Token(
                            leading: SyntaxFactory.TriviaList(
                                SyntaxFactory.Trivia(
                                    SyntaxFactory.DocumentationCommentTrivia(
                                        kind: SyntaxKind.SingleLineDocumentationCommentTrivia,
                                        content: SyntaxFactory.List(
                                            new XmlNodeSyntax[]
                                            {
                                                SyntaxFactory.XmlText(
                                                    textTokens: SyntaxFactory.TokenList(
                                                        SyntaxFactory.XmlTextLiteral(
                                                            leading: SyntaxFactory.TriviaList(
                                                                SyntaxFactory.DocumentationCommentExterior("///")),
                                                            text: " ",
                                                            value: " ",
                                                            trailing: default))),
                                                SyntaxFactory.XmlElement(
                                                    startTag: SyntaxFactory.XmlElementStartTag(
                                                        name: SyntaxFactory.XmlName("summary")),
                                                    content: SyntaxFactory.List(
                                                        new XmlNodeSyntax[]
                                                        {
                                                            SyntaxFactory.XmlText("Identifies the "),
                                                            SyntaxFactory.XmlEmptyElement(
                                                                name: SyntaxFactory.XmlName("see"),
                                                                attributes: SyntaxFactory.SingletonList<XmlAttributeSyntax>(
                                                                    SyntaxFactory.XmlCrefAttribute(
                                                                        cref: SyntaxFactory.NameMemberCref(
                                                                            name: SyntaxFactory.IdentifierName(
                                                                                identifier: property.Identifier.WithoutTrivia()))))),
                                                            SyntaxFactory.XmlText(" dependency property."),
                                                        }),
                                                    endTag: SyntaxFactory.XmlElementEndTag(
                                                        name: SyntaxFactory.XmlName("summary"))),
                                                SyntaxFactory.XmlText(
                                                    textTokens: SyntaxFactory.TokenList(
                                                        SyntaxFactory.XmlTextNewLine(
                                                            leading: default,
                                                            text: "\r\n",
                                                            value: "\r\n",
                                                            trailing: default))),
                                            }),
                                        endOfComment: SyntaxFactory.Token(SyntaxKind.EndOfDocumentationCommentToken)))),
                            kind: keyword,
                            trailing: SyntaxFactory.TriviaList(SyntaxFactory.Space));
                    }
                }

                static InvocationExpressionSyntax Register(string methodName, ExpressionSyntax name, TypeSyntax type, ClassDeclarationSyntax containingClass)
                {
                    return SyntaxFactory.InvocationExpression(
                        expression: SyntaxFactory.MemberAccessExpression(
                            kind: SyntaxKind.SimpleMemberAccessExpression,
                            expression: SyntaxFactory.IdentifierName("DependencyProperty"),
                            name: SyntaxFactory.IdentifierName(methodName)),
                        argumentList: SyntaxFactory.ArgumentList(
                            openParenToken: SyntaxFactory.Token(
                                leading: default,
                                kind: SyntaxKind.OpenParenToken,
                                trailing: SyntaxFactory.TriviaList(SyntaxFactory.LineFeed)),
                            arguments: SyntaxFactory.SeparatedList(
                                new[]
                                {
                                    SyntaxFactory.Argument(expression: name.WithLeadingTrivia(SyntaxFactory.Whitespace("            "))),
                                    SyntaxFactory.Argument(
                                        nameColon: default,
                                        refKindKeyword: default,
                                        expression: SyntaxFactory.TypeOfExpression(
                                            keyword: SyntaxFactory.Token(
                                                leading: SyntaxFactory.TriviaList(SyntaxFactory.Whitespace("            ")),
                                                kind: SyntaxKind.TypeOfKeyword,
                                                trailing: default),
                                            openParenToken: SyntaxFactory.Token(SyntaxKind.OpenParenToken),
                                            type: type,
                                            closeParenToken: SyntaxFactory.Token(SyntaxKind.CloseParenToken))),
                                    SyntaxFactory.Argument(
                                        nameColon: default,
                                        refKindKeyword: default,
                                        expression: SyntaxFactory.TypeOfExpression(
                                            keyword: SyntaxFactory.Token(
                                                leading: SyntaxFactory.TriviaList(SyntaxFactory.Whitespace("            ")),
                                                kind: SyntaxKind.TypeOfKeyword,
                                                trailing: default),
                                            openParenToken: SyntaxFactory.Token(SyntaxKind.OpenParenToken),
                                            type: SyntaxFactory.IdentifierName(containingClass.Identifier.WithoutTrivia()),
                                            closeParenToken: SyntaxFactory.Token(SyntaxKind.CloseParenToken))),
                                    SyntaxFactory.Argument(
                                        nameColon: default,
                                        refKindKeyword: default,
                                        expression: SyntaxFactory.ObjectCreationExpression(
                                            newKeyword: SyntaxFactory.Token(
                                                leading: SyntaxFactory.TriviaList(SyntaxFactory.Whitespace("            ")),
                                                kind: SyntaxKind.NewKeyword,
                                                trailing: SyntaxFactory.TriviaList(SyntaxFactory.Space)),
                                            type: SyntaxFactory.IdentifierName("PropertyMetadata"),
                                            argumentList: SyntaxFactory.ArgumentList(
                                                arguments: SyntaxFactory.SingletonSeparatedList(
                                                    SyntaxFactory.Argument(expression: SyntaxFactory.DefaultExpression(type)))),
                                            initializer: default)),
                                },
                                new[]
                                {
                                    SyntaxFactory.Token(
                                        leading: default,
                                        kind: SyntaxKind.CommaToken,
                                        trailing: SyntaxFactory.TriviaList(SyntaxFactory.LineFeed)),
                                    SyntaxFactory.Token(
                                        leading: default,
                                        kind: SyntaxKind.CommaToken,
                                        trailing: SyntaxFactory.TriviaList(SyntaxFactory.LineFeed)),
                                    SyntaxFactory.Token(
                                        leading: default,
                                        kind: SyntaxKind.CommaToken,
                                        trailing: SyntaxFactory.TriviaList(SyntaxFactory.LineFeed)),
                                }),
                            closeParenToken: SyntaxFactory.Token(SyntaxKind.CloseParenToken)));
                }

                static InvocationExpressionSyntax Nameof(PropertyDeclarationSyntax property)
                {
                    return SyntaxFactory.InvocationExpression(
                        expression: SyntaxFactory.IdentifierName("nameof"),
                        argumentList: SyntaxFactory.ArgumentList(
                            arguments: SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Argument(
                                    SyntaxFactory.IdentifierName(identifier: property.Identifier)))));
                }
            }
        }
    }
}