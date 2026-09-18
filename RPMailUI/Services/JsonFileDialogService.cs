using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using R3;

namespace RPMailUI.Services;

public sealed class JsonFileDialogService : IDisposable
{
    private readonly Subject<string> _errors = new();

    public IStorageProvider? StorageProvider { get; set; }

    public Observable<string> Errors => _errors;

    public async Task<string?> ReadAsync(string title)
    {
        if (StorageProvider is not { } provider)
        {
            _errors.OnNext("Storage provider unavailable.");
            return null;
        }

        IReadOnlyList<IStorageFile> files;
        try
        {
            files = await provider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = false,
                FileTypeFilter = new[] { new FilePickerFileType("JSON") { Patterns = new[] { "*.json" } } },
            });
        }
        catch (Exception ex)
        {
            _errors.OnNext($"File picker failed: {ex.Message}");
            return null;
        }

        if (files is not { Count: > 0 })
            return null;

        try
        {
            await using var stream = await files[0].OpenReadAsync();
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }
        catch (Exception ex)
        {
            _errors.OnNext($"Failed to read file: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> WriteAsync(string title, string suggestedName, string json)
    {
        if (StorageProvider is not { } provider)
        {
            _errors.OnNext("Storage provider unavailable.");
            return false;
        }

        IStorageFile? file;
        try
        {
            file = await provider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = title,
                SuggestedFileName = suggestedName,
                DefaultExtension = "json",
                FileTypeChoices = new[] { new FilePickerFileType("JSON") { Patterns = new[] { "*.json" } } },
            });
        }
        catch (Exception ex)
        {
            _errors.OnNext($"File picker failed: {ex.Message}");
            return false;
        }

        if (file is null)
            return false;

        try
        {
            await using var stream = await file.OpenWriteAsync();
            await using var writer = new StreamWriter(stream);
            await writer.WriteAsync(json);
            return true;
        }
        catch (Exception ex)
        {
            _errors.OnNext($"Failed to write file: {ex.Message}");
            return false;
        }
    }

    public void Dispose() => _errors.Dispose();
}
