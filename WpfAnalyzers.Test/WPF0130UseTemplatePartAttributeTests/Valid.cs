namespace WpfAnalyzers.Test.WPF0130UseTemplatePartAttributeTests
{
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;

    public static class Valid
    {
        private static readonly DiagnosticAnalyzer Analyzer = new GetTemplateChildAnalyzer();

        [Test]
        public static void StringLiterals()
        {
            var testCode = @"
namespace N
{
    using System.Windows;
    using System.Windows.Controls;

    [TemplatePart(Name = ""PART_Bar"", Type = typeof(Border))]
    public class FooControl : Control
    {
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            var bar = (Border)this.GetTemplateChild(""PART_Bar"");
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void Constant()
        {
            var testCode = @"
namespace N
{
    using System.Windows;
    using System.Windows.Controls;

    [TemplatePart(Name = PartBar, Type = typeof(Border))]
    public class FooControl : Control
    {
        private const string PartBar = ""PART_Bar"";

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            var bar = (Border)this.GetTemplateChild(PartBar);
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void TemplatePartAttribute()
        {
            var testCode = @"
namespace N
{
    using System.Windows;
    using System.Windows.Controls;

    [TemplatePartAttribute(Name = ""PART_Bar"", Type = typeof(Border))]
    public class FooControl : Control
    {
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            var bar = (Border)this.GetTemplateChild(""PART_Bar"");
        }
    }
}";
            RoslynAssert.Valid(Analyzer, testCode);
        }

        [Test]
        public static void BaseClassLiteral()
        {
            var baseCode = @"
namespace N
{
    using System.Windows;
    using System.Windows.Controls;

    [TemplatePart(Name = ""PART_Bar"", Type = typeof(Border))]
    public class BaseControl : Control
    {
    }
}";

            var testCode = @"
namespace N
{
    using System.Windows.Controls;

    public class FooControl : BaseControl
    {
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            var bar = (Border)this.GetTemplateChild(""PART_Bar"");
        }
    }
}";
            RoslynAssert.Valid(Analyzer, baseCode, testCode);
            RoslynAssert.Valid(Analyzer, testCode, baseCode);
        }

        [Test]
        public static void BaseClassConstant()
        {
            var baseCode = @"
namespace N
{
    using System.Windows;
    using System.Windows.Controls;

    [TemplatePart(Name = PartBar, Type = typeof(Border))]
    public class BaseControl : Control
    {
        protected const string PartBar = ""PART_Bar"";
    }
}";

            var testCode = @"
namespace N
{
    using System.Windows.Controls;

    public class FooControl : BaseControl
    {
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            var bar = (Border)this.GetTemplateChild(PartBar);
        }
    }
}";
            RoslynAssert.Valid(Analyzer, baseCode, testCode);
            RoslynAssert.Valid(Analyzer, testCode, baseCode);
        }

        [Test]
        public static void IsPatternStringLiteral()
        {
            var testCode = @"
namespace N
{
    using System.Windows;
    using System.Windows.Controls;

    [TemplatePart(Name = PartBar, Type = typeof(Border))]
    public class FooControl : Control
    {
        private const string PartBar = ""PART_Bar"";

        private Border bar;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            this.bar = null;
            if (this.GetTemplateChild(PartBar) is Border border)
            {
                this.bar = border;
            }
        }
    }
}";

            RoslynAssert.Valid(Analyzer, testCode);
        }

        [TestCase("as FrameworkElement")]
        [TestCase("as UIElement")]
        [TestCase("as Control")]
        public static void AsCastStringLiteral(string cast)
        {
            var testCode = @"
namespace N
{
    using System.Windows;
    using System.Windows.Controls;

    [TemplatePart(Name = PartBar, Type = typeof(FrameworkElement))]
    public class FooControl : Control
    {
        private const string PartBar = ""PART_Bar"";

        private object bar;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            this.bar = this.GetTemplateChild(PartBar) as FrameworkElement;
        }
    }
}".AssertReplace("as FrameworkElement", cast);

            RoslynAssert.Valid(Analyzer, testCode);
        }
    }
}