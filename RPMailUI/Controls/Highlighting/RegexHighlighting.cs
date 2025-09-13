using System.Collections.Generic;
using System.Text.RegularExpressions;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Rendering;

namespace RPMailUI.Controls.Highlighting;

public class RegexHighlighting : IHighlightingDefinition
{
    private readonly Regex _regex;
    private readonly HashSet<string> _wordLibrary;
    private readonly HighlightingColor _correctColor;
    private readonly HighlightingColor _incorrectColor;
    
    public HighlightingRuleSet GetNamedRuleSet(string name)
    {
        throw new System.NotImplementedException();
    }

    public HighlightingColor GetNamedColor(string name)
    {
        throw new System.NotImplementedException();
    }

    public string Name { get; } = "RegexHighLighting";

    public HighlightingRuleSet MainRuleSet { get; }
    public IEnumerable<HighlightingColor> NamedHighlightingColors { get; }
    public IDictionary<string, string> Properties { get; } = new Dictionary<string, string>();

    public RegexHighlighting(Regex regex,
        HashSet<string>? wordLibrary = null, Color? correct = null,Color? incorrect = null)
    {
        _regex = regex;
        _wordLibrary = wordLibrary?? [];
        _correctColor = new () { Foreground = new SimpleHighlightingBrush(correct?? Colors.Gray) };
        _incorrectColor =  new () { Foreground = new SimpleHighlightingBrush(incorrect?? Colors.Red) };
        
        HighlightingRuleSet ruleSet = new();
        ruleSet.Rules.Add(new()
        {
            Regex = regex,
            Color = _correctColor,
        });
        MainRuleSet = ruleSet;

        NamedHighlightingColors = [  _correctColor, _incorrectColor ];
    }

    // public void ApplyMatchColor(TextEditor editor)
    // {
    //     var doc =  editor.Document;
    //
    //     for (var i = 0; i < doc.LineCount; i++)
    //     {
    //         var line = doc.GetLineByNumber(i+1);
    //         
    //         var lineText = doc.GetText(line);
    //         var matches = _regex.Matches(lineText);
    //
    //         foreach (Match match in matches)
    //         {
    //             var word = match.Groups[1].Value;
    //             var color = _wordLibrary.Contains(word)? _correctColor : _incorrectColor;
    //             
    //             var startOffset = line.Offset + match.Index;
    //             var endOffset = startOffset + match.Length;
    //             
    //             TextRange range = new (startOffset, endOffset);
    //             var view = editor.TextArea.TextView;
    //             TextLineBackground highlight = new (color, range);
    //             view.LineTransformers.Add();
    //         }
    //     }
    // }
}