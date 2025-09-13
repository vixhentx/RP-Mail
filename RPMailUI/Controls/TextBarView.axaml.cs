using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml;

namespace RPMailUI.Controls;

public partial class TextBarView : UserControl
{
    public static readonly StyledProperty<string> CaptionProperty = AvaloniaProperty.Register<TextBarView, string>(
        nameof(Caption),"Caption");

    public string Caption
    {
        get => GetValue(CaptionProperty);
        set => SetValue(CaptionProperty, value);
    }

    public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<TextBarView, string>(
        nameof(Text),"",defaultBindingMode:BindingMode.TwoWay);

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
    public TextBarView()
    {
        InitializeComponent();
    }
}