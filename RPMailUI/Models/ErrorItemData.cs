namespace RPMailUI.Models;

public sealed record ErrorItemData(string Message, string Key)
{
    public string ForegroundKey => Key + ".Foreground";
    public string BackgroundKey => Key + ".Background";
}
