using RPMailCore.Models;

namespace RPMailUI.Models;

public sealed record TaskItemData(IReadOnlyDictionary<string, string> Data, MailTaskStatus Status, string Tooltip)
{
    public string this[string key] => Data[key];

    public List<string> SearchTokens => [.. Data.Values, Status.Text];
}
