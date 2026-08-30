using Scriban;
using Scriban.Runtime;

namespace RPMailCore.Processors;

public sealed class TemplateEngine
{
    private readonly Dictionary<string, Template> _cache = [];
    private readonly object _gate = new();

    public string Render(string pattern, IReadOnlyDictionary<string, string> row)
    {
        var template = GetOrParse(pattern);

        var scriptObject = new ScriptObject();
        foreach (var kv in row)
            scriptObject.SetValue(kv.Key, kv.Value, readOnly: true);

        var context = new TemplateContext { StrictVariables = true };
        context.PushGlobal(scriptObject);
        return template.Render(context);
    }

    private Template GetOrParse(string pattern)
    {
        lock (_gate)
        {
            if (!_cache.TryGetValue(pattern, out var template))
            {
                template = Template.Parse(pattern);
                if (template.HasErrors)
                    throw new InvalidOperationException(string.Join("; ", template.Messages));
                _cache[pattern] = template;
            }
            return template;
        }
    }
}
