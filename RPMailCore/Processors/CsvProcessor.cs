using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using CsvHelper;

namespace RPMailCore.Processors;

public sealed record CsvData(ImmutableArray<string> Headers, ImmutableArray<ImmutableDictionary<string, string>> Rows);

public static class CsvProcessor
{
    public static CsvData Read(string path, Encoding encoding)
    {
        using var reader = new StreamReader(path, encoding, true);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        if (!csv.Read()) throw new InvalidDataException("Incorrect data format");
        if (!csv.ReadHeader()) throw new InvalidDataException("Cannot read header row");
        var headers = csv.HeaderRecord ?? throw new InvalidDataException("Header row is null");

        var rows = ImmutableArray.CreateBuilder<ImmutableDictionary<string, string>>();
        while (csv.Read())
        {
            var builder = ImmutableDictionary.CreateBuilder<string, string>();
            foreach (var header in headers)
                builder[header] = csv.GetField(header) ?? "";
            rows.Add(builder.ToImmutable());
        }
        return new CsvData(headers.ToImmutableArray(), rows.ToImmutable());
    }
}
