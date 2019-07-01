namespace WpfAnalyzers.Test.WPF0141UseContainingMemberComponentResourceKeyTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static class ValidCode
    {
        private static readonly DiagnosticAnalyzer Analyzer = new ComponentResourceKeyAnalyzer();

        [Test]
        public static void WhenExpectedLiteral()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;

    public static class ResourceKeys
    {
        public static readonly ComponentResourceKey FooKey = new ComponentResourceKey(
            typeof(ResourceKeys),
            ""FooKey"");
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void WhenExpectedNameof()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;

    public static class ResourceKeys
    {
        public static readonly ComponentResourceKey FooKey = new ComponentResourceKey(
            typeof(ResourceKeys),
            nameof(FooKey));
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }
    }
}
