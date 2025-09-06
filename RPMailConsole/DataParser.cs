using System.Globalization;
using System.Text.RegularExpressions;
using CsvHelper;

namespace RPMailConsole;

public class DataParser
{
    readonly Dictionary<string, List<string>> _dictionary = [];
    public Dictionary<string, List<string>> Dictionary => _dictionary;
    
    public DataParser(string dataFilePath)
    {
        //parse csv data file
        
        using StreamReader reader = new(dataFilePath);
        using CsvReader csv = new(reader, CultureInfo.InvariantCulture);
        
        if(!csv.Read()) throw new InvalidDataException("Incorrect data format");
        
        //header row
        if(!csv.ReadHeader()) throw new InvalidDataException("Cannot read header row");
        
        var headers = csv.HeaderRecord;
        if(headers is null) throw new InvalidDataException("Header row is null");
        
        foreach (var header in headers) _dictionary.Add(header, []);
        
        //data rows
        while (csv.Read())
        {
            foreach (var header in headers)
            {
                var value = csv.GetField(header);
                //value would not be null
                _dictionary[header].Add(value);
            }
        }

    }
    
    public static readonly Regex REGEX = new(@"\{\$([A-Za-z_][A-Za-z0-9_]*)\}"); //Such as {$Name}

    public string Format(string property) => "{$" + property + "}";

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
    
    public string Parse(string pattern, int index)
    {
        List<string> properties = ParseProperties(pattern);
        
        //Build result
        string parsed = pattern;
        foreach (var property in properties)
        {
            parsed = parsed.Replace(Format(property), _dictionary[property][index]);
        }
        
        return parsed;
    }

    public void AbstractParse(string pattern, int index, Action<string,string> replacer)
    {
        List<string> properties = ParseProperties(pattern);
        
        //Action
        foreach (var property in properties)
        {
            replacer(Format(property), _dictionary[property][index]);
        }
    }

    public List<string> GetProperties(string header) => _dictionary[header];
}