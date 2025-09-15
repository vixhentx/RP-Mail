using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace RPMailUI.Controls;

public partial class TextPromptBarView : UserControl
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

    public static readonly StyledProperty<List<string>> PromptsSourceProperty = AvaloniaProperty.Register<TextPromptBarView, List<string>>(
        nameof(PromptsSource),[]);

    public List<string> PromptsSource
    {
        get => GetValue(PromptsSourceProperty);
        set => SetValue(PromptsSourceProperty, value);
    }
    
    public TextPromptBarView()
    {
        InitializeComponent();
    }
}