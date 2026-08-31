using System.Text;

namespace RPMailCore.Services;

public static class EncodingResolver
{
    public static Encoding Resolve(string? charsetName)
    {
        if (string.IsNullOrWhiteSpace(charsetName)) return Encoding.UTF8;
        try
        {
            return Encoding.GetEncoding(charsetName);
        }
        catch (ArgumentException)
        {
            return Encoding.UTF8;
        }
    }
}
