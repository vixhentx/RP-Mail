using System.Text;
using Avalonia.Controls;

namespace RPMailUI.Resources;

static class CharSets
{
    public static void Load(IResourceDictionary res)
    {
        var encodings = Encoding.GetEncodings();
        List<string> charSets = [];
        foreach (var encoding in encodings)
        {
            charSets.Add(encoding.Name);
        }
        res["AllCharSets"] = charSets;
    }
}