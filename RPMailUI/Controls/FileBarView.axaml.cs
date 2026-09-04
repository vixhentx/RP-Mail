using System.Diagnostics;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using R3;
using RPMailUI.Resources;
using RPMailUI.Services;

namespace RPMailUI.Controls;

public partial class FileBarView : UserControl
{
    private readonly DisposableBag _disposables = new();

    public static readonly StyledProperty<string> FilePathProperty = AvaloniaProperty.Register<FileBarView, string>(
        nameof(FilePath), defaultBindingMode: BindingMode.TwoWay);

    public string FilePath
    {
        get => GetValue(FilePathProperty);
        set => SetValue(FilePathProperty, value);
    }

    public static readonly StyledProperty<string> CaptionProperty = AvaloniaProperty.Register<FileBarView, string>(
        nameof(Caption), "Caption");

    public string Caption
    {
        get => GetValue(CaptionProperty);
        set => SetValue(CaptionProperty, value);
    }

    public static readonly StyledProperty<string> FileTypeProperty = AvaloniaProperty.Register<FileBarView, string>(
        nameof(FileType), "");

    public string FileType
    {
        get => GetValue(FileTypeProperty);
        set => SetValue(FileTypeProperty, value);
    }

    public static readonly StyledProperty<bool> IsDirectoryProperty = AvaloniaProperty.Register<FileBarView, bool>(
        nameof(IsDirectory));

    public bool IsDirectory
    {
        get => GetValue(IsDirectoryProperty);
        set => SetValue(IsDirectoryProperty, value);
    }

    public static readonly RoutedEvent<TextChangedEventArgs> TextChangedEvent =
        RoutedEvent.Register<TextBox, TextChangedEventArgs>(
            nameof(TextChanged), RoutingStrategies.Bubble);

    public event EventHandler<TextChangedEventArgs>? TextChanged
    {
        add => AddHandler(TextChangedEvent, value);
        remove => RemoveHandler(TextChangedEvent, value);
    }

    protected void OnTextChanged(object? sender, TextChangedEventArgs e) =>
        RaiseEvent(e);

    public ReactiveCommand BrowseCommand { get; } = new();
    public ReactiveCommand OpenCommand { get; } = new();

    public FileBarView()
    {
        InitializeComponent();
        BrowseCommand.SubscribeAwait(async (_, _) => await BrowseAsync()).AddTo(ref _disposables);
        OpenCommand.Subscribe(_ => Open()).AddTo(ref _disposables);
    }

    private async Task BrowseAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this)!;
        var provider = topLevel.StorageProvider;

        string selectedPath = "";
        if (!IsDirectory)
        {
            var fileTypeExt = string.IsNullOrWhiteSpace(FileType) ? "*" : FileType.ToLower();
            var fileTypeName = string.IsNullOrWhiteSpace(FileType) ? Strings.AnyFile : string.Format(Strings.FileTypeName, FileType);
            var files = await provider.OpenFilePickerAsync(new()
            {
                Title = string.Format(Strings.OpenFilePickerTitle, FileType),
                AllowMultiple = false,
                FileTypeFilter = [new(fileTypeName)
                {
                    Patterns = [$"*.{fileTypeExt}"]
                }]
            });

            if (files.Count > 0)
            {
                selectedPath = files[0].TryGetLocalPath() ?? "";
            }
        }
        else
        {
            var directories = await provider.OpenFolderPickerAsync(new()
            {
                Title = Strings.SelectDirectory,
                AllowMultiple = false,
                SuggestedStartLocation = await provider.TryGetFolderFromPathAsync(AppContext.BaseDirectory)
            });

            if (directories.Count > 0)
            {
                selectedPath = directories[0].TryGetLocalPath() ?? "";
            }
        }
        string filePath = selectedPath;
        if (!string.IsNullOrEmpty(filePath))
        {
            string currentDir = Directory.GetCurrentDirectory();
            string relativePath = GetRelativePath(currentDir, selectedPath);
            if (GetPathLayerCount(relativePath) <= 2)
            {
                filePath = relativePath;
            }
        }
        FilePath = filePath;
    }

    private void Open()
    {
        try
        {
            if (!IsDirectory)
            {
                PathOpenHelper.OpenFilePath(FilePath);
            }
            else
            {
                PathOpenHelper.OpenDirectory(FilePath);
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine(string.Format(Strings.CannotOpen, FilePath, ex.Message));
        }
    }

    //Utils
    static string GetRelativePath(string basePath, string targetPath)
    {
        Uri baseUri = new Uri(basePath + Path.DirectorySeparatorChar);
        Uri targetUri = new Uri(targetPath);

        Uri relativeUri = baseUri.MakeRelativeUri(targetUri);

        return Uri.UnescapeDataString(relativeUri.ToString());
    }
    static int GetPathLayerCount(string path)
    {
        char separator = Path.DirectorySeparatorChar;

        string[] parts = path.Split(separator, StringSplitOptions.RemoveEmptyEntries);

        return parts.Length;
    }
}
