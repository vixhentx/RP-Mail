using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Avalonia.Platform.Storage;
using R3;

namespace RPMailUI.Services;

public sealed class JsonFileDialogService(IStorageProvider provider) : IDisposable
{
    private readonly Subject<string> _errors = new();

    public Observable<string> Errors => _errors;

	public async ValueTask<T?> ReadConf<T>(JsonTypeInfo<T> info)
		where T : class
	{
		string? text = await ReadAsync($"Select Config File: {typeof(T).Name}");
		if(text is null) return null;

		try
		{
			T? conf = JsonSerializer.Deserialize<T>(text, info);
			return conf;
		}
        catch (Exception ex)
        {
            _errors.OnNext($"Json Deserializer failed: {ex.Message}");
            return null;
        }
	}


    public async Task<string?> ReadAsync(string title)
    {
        IReadOnlyList<IStorageFile> files;
        try
        {
            files = await provider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = false,
                FileTypeFilter = [new("JSON") { Patterns = ["*.json"] }],
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

	public async ValueTask WriteConf<T>(T conf, JsonTypeInfo<T> info, string suggestedName)
		where T : class
	{
		string text = JsonSerializer.Serialize<T>(conf, info);
		_ = await WriteAsync($"Select Config File: {typeof(T).Name}", suggestedName, text);
	}

    public async Task<bool> WriteAsync(string title, string suggestedName, string json)
    {
        IStorageFile? file;
        try
        {
            file = await provider.SaveFilePickerAsync(
				new FilePickerSaveOptions
				{
					Title = title,
					SuggestedFileName = suggestedName,
					DefaultExtension = "json",
					FileTypeChoices = [new("JSON") { Patterns = [ "*.json" ] } ],
				}
			);
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
