using System.Globalization;
using CsvHelper;
using RPMailCore.Processors;

namespace RPMailCore;

public class DataParser
{
    private string[] _headers = [];
    public string[] Headers => _headers;

    public List<Dictionary<string,string>> Rows { get; } = [];

    private readonly TemplateEngine _templateEngine = new();

    public void Parse(StreamReader reader, Action<(int index,Dictionary<string, string> row)>? rowHandler = null)
    {
        //parse csv data file
        using CsvReader csv = new(reader, CultureInfo.InvariantCulture);

        if(!csv.Read()) throw new InvalidDataException("Incorrect data format");

        //header row
        if(!csv.ReadHeader()) throw new InvalidDataException("Cannot read header row");

        var headers = csv.HeaderRecord;
        _headers = headers ?? throw new InvalidDataException("Header row is null");

        int index = 0;
        while (csv.Read())
        {
            var obj = new Dictionary<string,string>();
            foreach(var header in headers)
            {
                obj[header] = csv.GetField(header)??"";
            }
            rowHandler?.Invoke((index,obj));
            Rows.Add(obj);
            index++;
        }

    }

    public string Parse(string pattern, Dictionary<string,string> row) =>
        _templateEngine.Render(pattern, row);

    public List<string> GetPropertiesOf(Dictionary<string,string> row) => Headers.Select(header => row[header]).ToList();
    public Dictionary<string,string>? FindRow(string property, string value) => Rows.Find(row => row[property] == value);
    public List<string> GetProperties(string header) =>Rows.Select(row => row[header]).ToList();
}
