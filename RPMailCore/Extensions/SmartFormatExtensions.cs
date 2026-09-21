using SmartFormat;
namespace RPMailCore.Extensions;

public static class SmartFormatExtensions
{
	extension(Smart)
	{
		public static string FormatDict(string format, Dictionary<string,object> dict) =>
			Smart.Format(format, dict);
	}
}