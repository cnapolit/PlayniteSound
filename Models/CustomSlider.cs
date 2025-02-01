using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace PlayniteSounds.Models;

public class CustomSlider : Slider
{
    //protected override void OnThumbDragStarted(DragStartedEventArgs e) => FormatToolTipContent(e, base.OnThumbDragStarted);
    //protected override void OnThumbDragDelta(DragDeltaEventArgs e) => FormatToolTipContent(e, base.OnThumbDragDelta);
    //protected override void OnThumbDragCompleted(DragCompletedEventArgs e) => FormatToolTipContent(e, base.OnThumbDragCompleted);

    //private void FormatToolTipContent<T>(T arg, Action<T> action)
    //{

    //    action(arg);
    //}

    public static readonly DependencyProperty AutoToolTipProperty = DependencyProperty.Register(
        nameof(AutoToolTip), typeof(string), typeof(CustomSlider));

    //[Bindable(true)]
    //[Category("Appearance")]
    public string AutoToolTip
    {
        get => (string)GetValue(AutoToolTipProperty);
        set
        {
            const BindingFlags privateVariable = BindingFlags.NonPublic | BindingFlags.Instance;
            var toolTip = (ToolTip)typeof(Slider).GetField("_autoToolTip", privateVariable).GetValue(this);
            toolTip.Content = value;
            SetValue(AutoToolTipProperty, value);
        }
    }
}