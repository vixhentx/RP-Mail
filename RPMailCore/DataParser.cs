using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CsvHelper;

namespace RPMailCore;

public class DataParser
{
    readonly String[] _headers;
    readonly List<Dictionary<string,string>> _rows = [];
    public string[] Headers => _headers;
    public List<Dictionary<string,string>> Rows => _rows;
    
    public DataParser(StreamReader reader)
    {
        //parse csv data file
        using CsvReader csv = new(reader, CultureInfo.InvariantCulture);
        
        if(!csv.Read()) throw new InvalidDataException("Incorrect data format");
        
        //header row
        if(!csv.ReadHeader()) throw new InvalidDataException("Cannot read header row");
        
        var headers = csv.HeaderRecord;
        _headers = headers ?? throw new InvalidDataException("Header row is null");
        
        while (csv.Read())
        {
            var obj = new Dictionary<string,string>();
            foreach(var header in headers)
            {
                obj[header] = csv.GetField(header);
            }
            _rows.Add(obj);
        }

    }
    
    public static readonly Regex REGEX = new(@"\{\$([A-Za-z_][A-Za-z0-9_]*)\}"); //Such as {$Name}

    public static string Format(string property) => "{$" + property + "}";

    public List<string> ParseProperties(string pattern)
    {
        List<string> properties = [];
        
        //Parse pattern
        var matches = REGEX.Matches(pattern);

        //Unwrap matches
        foreach (Match match in matches)
        {
            properties.Add(match.Groups[1].Value);
        }
        
        return properties;
    }
    
    public string Parse(string pattern, Dictionary<string,string> row)
    {
        List<string> properties = ParseProperties(pattern);
        
        //Build result
        string parsed = pattern;
        foreach (var property in properties)
        {
            parsed = parsed.Replace(Format(property), row[property]);
        }
        
        return parsed;
    }

    public void AbstractParse(string pattern, Dictionary<string,string> row, Action<string,string> replacer)
    {
        List<string> properties = ParseProperties(pattern);
        
        //Action
        foreach (var property in properties)
        {
            replacer(Format(property), row[property]);
        }
    }

    public List<string> GetPropertiesOf(Dictionary<string,string> row) => _headers.Select(header => row[header]).ToList();
    public Dictionary<string,string>? FindRow(string property, string value) => _rows.Find(row => row[property] == value);
    public List<string> GetProperties(string header) =>_rows.Select(row => row[header]).ToList();
}