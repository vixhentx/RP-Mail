using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.Logging;
using R3;
using RPMailCore.Extensions;
using SmartFormat;
using RPMailUI.Models;
using RPMailUI.Resources;

namespace RPMailUI.Services;

public sealed record LoadedConfig<T>(T Value, string Directory)
	where T : class;

public sealed class JsonFileDialogService(
	IStorageProvider provider,
	ErrorRouteService es
)
{
	public async ValueTask<LoadedConfig<T>?> ReadConf<T>(JsonTypeInfo<T> info, string workspaceDirectory, CancellationToken ct = default)
		where T : class
	{
		IReadOnlyList<IStorageFile> files;
		try
		{
			files = await provider.OpenFilePickerAsync(new()
			{
				Title = Smart.FormatDict(
					Strings.OpenFilePickerTitle,
					new ()
					{
						["FileType"] = Strings.Json,
					}),
				AllowMultiple = false,
				FileTypeFilter =
				[
					new(Smart.FormatDict(
						Strings.FileTypeName,
						new ()
						{
							["FileType"] = Strings.Json,
						}))
					{
						Patterns = ["*.json"],
					}
				],
				SuggestedStartLocation = await provider.TryGetFolderFromPathAsync(workspaceDirectory),
			});
		}
		catch (Exception ex)
		{
			es.Error.OnNext(ErrorItemData.Create(
				LogLevel.Error,
				Smart.FormatDict(
					Strings.FilePickerFailed,
					new ()
					{
						["Message"] = ex.Message,
					})));
			return null;
		}

		if (files is not [var file])
			return null;

		try
		{
			var path = file.TryGetLocalPath();
			var directory = path is null ? null : Path.GetDirectoryName(path);
			if (directory is null)
			{
				es.Error.OnNext(ErrorItemData.Create(
					LogLevel.Error,
					Strings.SelectedConfigNoDirectory));
				return null;
			}

			await using var stream = await file.OpenReadAsync();
			var conf = await JsonSerializer.DeserializeAsync(stream, info, ct);
			return conf is null ? null : new(conf, directory);
		}
		catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
		{
			es.Error.OnNext(ErrorItemData.Create(
				LogLevel.Error,
				Smart.FormatDict(
					Strings.FailedToReadConfig,
					new ()
					{
						["Message"] = ex.Message,
					})));
			return null;
		}
	}

	public async ValueTask WriteConf<T>(
		T conf,
		JsonTypeInfo<T> info,
		string suggestedName,
		string workspaceDirectory,
		CancellationToken ct = default
	)
		where T : class
	{
		var text = JsonSerializer.Serialize(conf, info);
		_ = await WriteAsync(
			Smart.FormatDict(
				Strings.OpenFilePickerTitle,
				new ()
				{
					["FileType"] = Strings.Json,
				}),
			suggestedName,
			text,
			workspaceDirectory,
			ct);
	}

	public async ValueTask<string?> PickDirectory(string workspaceDirectory)
	{
		try
		{
			var directories = await provider.OpenFolderPickerAsync(new()
			{
				Title = Strings.SelectDirectory,
				AllowMultiple = false,
				SuggestedStartLocation = await provider.TryGetFolderFromPathAsync(workspaceDirectory),
			});
			return directories is [var directory] ? directory.TryGetLocalPath() : null;
		}
		catch (Exception ex)
		{
			es.Error.OnNext(ErrorItemData.Create(
				LogLevel.Error,
				Smart.FormatDict(
					Strings.FilePickerFailed,
					new ()
					{
						["Message"] = ex.Message,
					})));
			return null;
		}
	}

	public async Task<bool> WriteAsync(
		string title,
		string suggestedName,
		string json,
		string workspaceDirectory,
		CancellationToken ct = default
	)
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
					FileTypeChoices =
					[
						new(Smart.FormatDict(
							Strings.FileTypeName,
							new ()
							{
								["FileType"] = Strings.Json,
							}))
						{
							Patterns = ["*.json"],
						}
					],
					SuggestedStartLocation = await provider.TryGetFolderFromPathAsync(workspaceDirectory),
				}
			);
		}
		catch (Exception ex)
		{
			es.Error.OnNext(ErrorItemData.Create(
				LogLevel.Error,
				Smart.FormatDict(
					Strings.FilePickerFailed,
					new()
					{
						["Message"] = ex.Message,
					})));
			return false;
		}

		if (file is null)
			return false;

		try
		{
			await using var stream = await file.OpenWriteAsync();
			await using var writer = new StreamWriter(stream);
			await writer.WriteAsync(json.AsMemory(), ct);
			return true;
		}
		catch (Exception ex)
		{
			es.Error.OnNext(ErrorItemData.Create(
				LogLevel.Error,
				Smart.FormatDict(
					Strings.FailedToWriteFile,
					new ()
					{
						["Message"] = ex.Message,
					})));
			return false;
		}
	}
}
