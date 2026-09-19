using Microsoft.Extensions.Logging;

namespace RPMailUI.Models;

public sealed record ErrorItemData(string Message, string Key)
{
    public string ForegroundKey => Key + ".Foreground";
    public string BackgroundKey => Key + ".Background";

	public static ErrorItemData Create(LogLevel level, string message)
	{
		string key = level >= LogLevel.Error ? "Flyout.Error" : "Flyout.Warning";
		return new(message, key);
	}
}
