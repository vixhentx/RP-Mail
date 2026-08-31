using System.Globalization;
using System.Text;
using CsvHelper;

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

    public static void WriteCsv(string path, IReadOnlyList<string> headers, IEnumerable<IReadOnlyDictionary<string, string>> rows, Encoding encoding)
    {
        using var writer = new StreamWriter(path, false, encoding);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        foreach (var header in headers)
            csv.WriteField(header);
        csv.NextRecord();
        foreach (var row in rows)
        {
            foreach (var header in headers)
                csv.WriteField(row.TryGetValue(header, out var value) ? value : "");
            csv.NextRecord();
        }
    }
}
