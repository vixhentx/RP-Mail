using Avalonia;
using Avalonia.Controls;

namespace RPMailUI.Controls;

public partial class FileBarView : UserControl
{
    public string Caption { get; set; } = "Caption";
    public string FilePath { get; set; } = "";
    public FileBarView()
    {
        InitializeComponent();
        DataContext = this;
    }
}