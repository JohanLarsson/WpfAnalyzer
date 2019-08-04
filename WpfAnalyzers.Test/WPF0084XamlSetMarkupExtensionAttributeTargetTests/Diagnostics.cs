namespace WpfAnalyzers.Test.WPF0084XamlSetMarkupExtensionAttributeTargetTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static class Diagnostics
    {
        private static readonly DiagnosticAnalyzer Analyzer = new AttributeAnalyzer();
        private static readonly ExpectedDiagnostic ExpectedDiagnostic = ExpectedDiagnostic.Create(Descriptors.WPF0084XamlSetMarkupExtensionAttributeTarget);

        [Test]
        public static void Message()
        {
            var testCode = @"
namespace N
{
    using System.Windows.Controls;
    using System.Windows.Markup;

    [XamlSetMarkupExtension(↓nameof(ReceiveMarkupExtension))]
    public class WithSetMarkupExtensionAttribute : Control
    {
        public static int ReceiveMarkupExtension(object targetObject, XamlSetMarkupExtensionEventArgs eventArgs)
        {
            return 1;
        }
    }
}";

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic.WithMessage("Expected a method with signature void ReceiveMarkupExtension(object, XamlSetMarkupExtensionEventArgs)."), testCode);
        }

        [Test]
        public static void WhenReturningInt()
        {
            var testCode = @"
namespace N
{
    using System.Windows.Controls;
    using System.Windows.Markup;

    [XamlSetMarkupExtension(↓nameof(ReceiveMarkupExtension))]
    public class WithSetMarkupExtensionAttribute : Control
    {
        public static int ReceiveMarkupExtension(object targetObject, XamlSetMarkupExtensionEventArgs eventArgs)
        {
            return 1;
        }
    }
}";

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public static void WhenNoParameters()
        {
            var testCode = @"
namespace N
{
    using System.Windows.Controls;
    using System.Windows.Markup;

    [XamlSetMarkupExtension(↓nameof(ReceiveMarkupExtension))]
    public class WithSetMarkupExtensionAttribute : Control
    {
        public static void ReceiveMarkupExtension()
        {
        }
    }
}";

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public static void WhenMOreThanTwoParameters()
        {
            var testCode = @"
namespace N
{
    using System.Windows.Controls;
    using System.Windows.Markup;

    [XamlSetMarkupExtension(↓nameof(ReceiveMarkupExtension))]
    public class WithSetMarkupExtensionAttribute : Control
    {
        public static void ReceiveMarkupExtension(object targetObject, XamlSetMarkupExtensionEventArgs eventArgs, int i)
        {
        }
    }
}";

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public static void WhenFirstParameterIsInt()
        {
            var testCode = @"
namespace N
{
    using System.Windows.Controls;
    using System.Windows.Markup;

    [XamlSetMarkupExtension(↓nameof(ReceiveMarkupExtension))]
    public class WithSetMarkupExtensionAttribute : Control
    {
        public static void ReceiveMarkupExtension(int targetObject, XamlSetMarkupExtensionEventArgs eventArgs)
        {
        }
    }
}";

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }

        [Test]
        public static void WhenSecondParameterIsInt()
        {
            var testCode = @"
namespace N
{
    using System.Windows.Controls;
    using System.Windows.Markup;

    [XamlSetMarkupExtension(↓nameof(ReceiveMarkupExtension))]
    public class WithSetMarkupExtensionAttribute : Control
    {
        public static void ReceiveMarkupExtension(object targetObject, int eventArgs)
        {
        }
    }
}";

            RoslynAssert.Diagnostics(Analyzer, ExpectedDiagnostic, testCode);
        }
    }
}
