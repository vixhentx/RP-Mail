using System.Text.RegularExpressions;

namespace RPMailConsole;

public class DataParser
{
    readonly Dictionary<string, List<string>> _dictionary = [];
    public int DataCount { get; private set; }
    
    public DataParser(string dataFile)
    {
        var lines =  File.ReadAllLines(dataFile);
        var headers = lines[0].Split(',').Select(s => s.Trim()).ToArray();
        for(int i = 1; i < lines.Length; i++)
        {
            var values = lines[i].Split(',').Select(s => s.Trim()).ToArray();
            if (values.Length != headers.Length)
            {
                throw new InvalidDataException("CSV Header count mismatch!");
            }

            for (int j = 0; j < values.Length; j++)
            {
                _dictionary.TryAdd(headers[j], new());
                _dictionary[headers[j]].Add(values[j]);
            }
        }
        DataCount = lines.Length - 1;
    }
    
    static Regex regex = new(@"\{\$([A-Za-z_][A-Za-z0-9_]*)\}"); //Such as {$Name}
    
    public string Parse(string pattern, int index)
    {
        List<string> properties = [];
        
        //Parse pattern
        {
            var matches = regex.Matches(pattern);

            //Unwrap matches
            foreach (Match match in matches)
            {
                properties.Add(match.Groups[1].Value);
            }
        }
        
        //Build result
        string parsed = pattern;
        foreach (var property in properties)
        {
            parsed = parsed.Replace("{$" + property + "}", _dictionary[property][index]);
        }
        
        return parsed;
    }

    public List<string> GetProperties(string header) => _dictionary[header];
}