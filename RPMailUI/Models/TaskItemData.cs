using RPMailCore.Models;

namespace RPMailUI.Models;

public sealed record TaskItemData(string Text, MailTaskStatus Status, string TooltipText)
{
	public int Ordinal => Status.Ordinal;
}
