using System.Collections.Concurrent;
using Scriban;
using Scriban.Runtime;

namespace RPMailCore.Processors;

public sealed class TemplateEngine
{
    readonly ConcurrentDictionary<string, Template> _cache = [];

    public string Render(string pattern, IReadOnlyDictionary<string, string> user, IReadOnlyDictionary<string, string> extraAttributes)
    {
		var template = GetOrParse(pattern);

		var scriptObject = new ScriptObject();
		foreach (var kv in extraAttributes)
			scriptObject.SetValue(kv.Key, kv.Value, readOnly: true);

		var userObject = new ScriptObject();
		foreach (var kv in user)
			userObject.SetValue(kv.Key, kv.Value, readOnly: true);

		scriptObject.SetValue("user", userObject, readOnly: true);

		var context = new TemplateContext { StrictVariables = true };
		context.PushGlobal(scriptObject);
		return template.Render(context);
    }

    Template GetOrParse(string pattern) =>
		_cache.GetOrAdd(pattern, static pattern =>
		{
			var template = Template.Parse(pattern);
			if (template.HasErrors)
				throw new InvalidOperationException(string.Join("; ", template.Messages));
			return template;
		});
		
}
