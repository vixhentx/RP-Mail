using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia;
using RPMailUI.Services;

namespace RPMailUI.Controls;

public partial class FileBarView : UserControl
{
    public static readonly StyledProperty<string> FilePathProperty = AvaloniaProperty.Register<FileBarView, string>(
        nameof(FilePath),defaultBindingMode:BindingMode.TwoWay);

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

    public static readonly StyledProperty<string> FileTypeProperty = AvaloniaProperty.Register<FileBarView, string>(
        nameof(FileType),"");

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

    public FileBarView()
    {
        InitializeComponent();
    }

    [RelayCommand]
    private async Task Browse()
    {
        var topLevel = TopLevel.GetTopLevel(this)!;
        var provider = topLevel.StorageProvider;

        if (!IsDirectory)
        {
            var fileTypeExt = string.IsNullOrWhiteSpace(FileType) ? "*" : FileType.ToLower();
            var fileTypeName = $"{(string.IsNullOrWhiteSpace(FileType) ? "Any" : FileType)} File";
            var files = await provider.OpenFilePickerAsync(new ()
            {
                Title = $"Open {FileType} File",
                AllowMultiple = false,
                FileTypeFilter = [new(fileTypeName)
                {
                    Patterns = [$"*.{fileTypeExt}"]
                }]
            });
        
            if (files.Count > 0)
            {
                FilePath = files[0].TryGetLocalPath() ?? "";
            }
        }
        else
        {
            var directories = await provider.OpenFolderPickerAsync(new()
            {
                Title = "Select Directory",
                AllowMultiple = false,
                SuggestedStartLocation = await provider.TryGetFolderFromPathAsync(AppContext.BaseDirectory)
            });

            if (directories.Count > 0)
            {
                FilePath = directories[0].TryGetLocalPath() ?? "";
            }
        }
    }

    [RelayCommand]
    private async Task Open()
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
            var box = MessageBoxManager
                .GetMessageBoxStandard("Error", $"Cannot open {FilePath}: {ex.Message}");
            await box.ShowAsync();
        }
    }
}