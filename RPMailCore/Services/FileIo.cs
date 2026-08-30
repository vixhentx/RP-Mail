using System.Text;

namespace RPMailCore.Services;

public static class FileIo
{
    public static string ReadAllText(string path, Encoding encoding)
    {
        using var reader = new StreamReader(path, encoding, true);
        return reader.ReadToEnd();
    }

    public static void WriteAllText(string path, string content, Encoding encoding)
    {
        CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content, encoding);
    }

    public static void WriteAllBytes(string path, byte[] bytes)
    {
        CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, bytes);
    }

    public static void CreateDirectory(string path) => Directory.CreateDirectory(path);

    public static void Delete(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }
}
