using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace RPMailUI.Controls;

public partial class FileBarView : UserControl
{
    public static readonly StyledProperty<string> FilePathProperty = AvaloniaProperty.Register<FileBarView, string>(
        nameof(FilePath));

    public string FilePath
    {
        get => GetValue(FilePathProperty);
        set => SetValue(FilePathProperty, value);
    }

    public static readonly StyledProperty<string> CaptionProperty = AvaloniaProperty.Register<FileBarView, string>(
        nameof(Caption),"Caption");

    public string Caption
    {
        get => GetValue(CaptionProperty);
        set => SetValue(CaptionProperty, value);
    }

    public FileBarView()
    {
        InitializeComponent();
    }
}