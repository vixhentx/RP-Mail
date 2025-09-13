
using Avalonia;

namespace RPMailUI.Resources;

public static class ResourcesEntry
{
    public static void Load()
    {
        if (Application.Current != null)
        {
            var res = Application.Current.Resources;
            CharSets.Load(res);
        }
    }
}