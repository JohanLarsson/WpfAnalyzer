namespace WpfAnalyzers.Test.WPF0010DefaultValueMustMatchRegisteredTypeTests
{
    using Gu.Roslyn.Asserts;
    using NUnit.Framework;

    internal class HappyPath
    {
        [Test]
        public void DependencyPropertyNoMetadata()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(double),
            typeof(FooControl));

        public double Value
        {
            get { return (double)this.GetValue(ValueProperty); }
            set { this.SetValue(ValueProperty, value); }
        }
    }
}";
            AnalyzerAssert.Valid<WPF0010DefaultValueMustMatchRegisteredType>(testCode);
        }

        [Test]
        public void DependencyPropertyMetadataWithCallbackOnly()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        public static readonly DependencyProperty BarProperty = DependencyProperty.Register(
            ""Bar"", 
            typeof(int), 
            typeof(FooControl), 
            new PropertyMetadata(OnValueChanged));

        public int Bar
        {
            get { return (int) this.GetValue(BarProperty); }
            set { this.SetValue(BarProperty, value); }
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // nop
        }
    }
}";
            AnalyzerAssert.Valid<WPF0010DefaultValueMustMatchRegisteredType>(testCode);
        }

        [TestCase("int", "new PropertyMetadata()")]
        [TestCase("int", "new FrameworkPropertyMetadata()")]
        [TestCase("int", "new PropertyMetadata(default(int))")]
        [TestCase("int", "new PropertyMetadata(1, OnValueChanged)")]
        [TestCase("int", "new PropertyMetadata(1)")]
        [TestCase("int?", "new PropertyMetadata(1)")]
        [TestCase("int?", "new PropertyMetadata(null)")]
        [TestCase("bool?", "new PropertyMetadata(null)")]
        [TestCase("bool?", "new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)")]
        [TestCase("int?", "new PropertyMetadata(default(int?))")]
        [TestCase("Nullable<int>", "new PropertyMetadata(default(int?))")]
        [TestCase("int", "new PropertyMetadata(CreateDefaultValue())")]
        [TestCase("int", "new PropertyMetadata(CreateObjectValue())")]
        [TestCase("ObservableCollection<int>", "new PropertyMetadata(null)")]
        [TestCase("ObservableCollection<int>", "new PropertyMetadata(new ObservableCollection<int>())")]
        [TestCase("ObservableCollection<int>", "new PropertyMetadata(default(ObservableCollection<int>))")]
        [TestCase("IEnumerable", "new PropertyMetadata(new ObservableCollection<int>())")]
        public void DependencyPropertyWithMetdadata(string typeName, string metadata)
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Windows;
    using System.Windows.Controls;

    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(double),
            typeof(FooControl),
            new PropertyMetadata(1));

        public double Value
        {
            get { return (double)this.GetValue(ValueProperty); }
            set { this.SetValue(ValueProperty, value); }
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // nop
        }

        private static double CreateDefaultValue() => default(double);
        private static object CreateObjectValue() => default(double);
    }
}";
            testCode = testCode.AssertReplace("new PropertyMetadata(1)", metadata)
                               .AssertReplace("double", typeName);
            AnalyzerAssert.Valid<WPF0010DefaultValueMustMatchRegisteredType>(testCode);
        }

        [Test]
        public void DependencyPropertyGeneric()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl<T> : Control
    {
        public static readonly DependencyProperty BarProperty = DependencyProperty.Register(
            ""Bar"",
            typeof(T),
            typeof(FooControl<T>),
            new PropertyMetadata(default(T)));

        public T Bar
        {
            get { return (T)GetValue(BarProperty); }
            set { SetValue(BarProperty, value); }
        }
    }
}";
            AnalyzerAssert.Valid<WPF0010DefaultValueMustMatchRegisteredType>(testCode);
        }

        [Test]
        public void DependencyPropertyWhenBoxed()
        {
            var booleanBoxesCode = @"
namespace RoslynSandbox
{
    internal static class BooleanBoxes
    {
        internal static readonly object True = true;
        internal static readonly object False = false;

        internal static object Box(bool value)
        {
            return value
                        ? True
                        : False;
        }
    }
}";

            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value),
            typeof(bool),
            typeof(FooControl),
            new PropertyMetadata(BooleanBoxes.Box(true)));

        public bool Value
        {
            get { return (bool)this.GetValue(ValueProperty); }
            set { this.SetValue(ValueProperty, value); }
        }
    }
}";
            AnalyzerAssert.Valid<WPF0010DefaultValueMustMatchRegisteredType>(booleanBoxesCode, testCode);
        }

        [Test]
        public void ReadOnlyDependencyProperty()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;
    using System.Windows.Controls;

    using System.Windows;
    using System.Windows.Controls;

    public class FooControl : Control
    {
        private static readonly DependencyPropertyKey ValuePropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(Value),
            typeof(double),
            typeof(FooControl),
            new PropertyMetadata(1.0));

        public static readonly DependencyProperty ValueProperty = ValuePropertyKey.DependencyProperty;

        public double Value
        {
            get { return (double)this.GetValue(ValueProperty); }
            set { this.SetValue(ValuePropertyKey, value); }
        }
    }
}";
            AnalyzerAssert.Valid<WPF0010DefaultValueMustMatchRegisteredType>(testCode);
        }

        [Test]
        public void AttachedProperty()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;

    public static class Foo
    {
        public static readonly DependencyProperty BarProperty = DependencyProperty.RegisterAttached(
            ""Bar"",
            typeof(int),
            typeof(Foo),
            new PropertyMetadata(default(int)));

        public static void SetBar(this FrameworkElement element, int value) => element.SetValue(BarProperty, value);

        public static int GetBar(this FrameworkElement element) => (int)element.GetValue(BarProperty);
    }
}";

            AnalyzerAssert.Valid<WPF0010DefaultValueMustMatchRegisteredType>(testCode);
        }

        [Test]
        public void AttachedPropertyWhenBoxed()
        {
            var booleanBoxesCode = @"
namespace RoslynSandbox
{
    internal static class BooleanBoxes
    {
        internal static readonly object True = true;
        internal static readonly object False = false;

        internal static object Box(bool value)
        {
            return value
                        ? True
                        : False;
        }
    }
}";

            var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.Windows;

    public static class Foo
    {
        public static readonly DependencyProperty BarProperty = DependencyProperty.RegisterAttached(
            ""Bar"",
            typeof(bool),
            typeof(Foo),
            new PropertyMetadata(BooleanBoxes.Box(true)));

        public static void SetBar(FrameworkElement element, bool value)
        {
            element.SetValue(BarProperty, value);
        }

        public static bool GetBar(FrameworkElement element)
        {
            return (bool)element.GetValue(BarProperty);
        }
    }
}";
            AnalyzerAssert.Valid<WPF0010DefaultValueMustMatchRegisteredType>(booleanBoxesCode, testCode);
        }

        [Test]
        public void ReadOnlyAttachedProperty()
        {
            var testCode = @"
namespace RoslynSandbox
{
    using System.Windows;

    public static class Foo
    {
        private static readonly DependencyPropertyKey BarPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            ""Bar"",
            typeof(int),
            typeof(Foo),
            new PropertyMetadata(default(int)));

            public static readonly DependencyProperty BarProperty = BarPropertyKey.DependencyProperty;

        public static void SetBar(this FrameworkElement element, int value) => element.SetValue(BarPropertyKey, value);

        public static int GetBar(this FrameworkElement element) => (int)element.GetValue(BarProperty);
    }
}";

            AnalyzerAssert.Valid<WPF0010DefaultValueMustMatchRegisteredType>(testCode);
        }
    }
}