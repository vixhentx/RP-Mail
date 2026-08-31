using R3;

namespace RPMailUI.Models;

public sealed class AttachmentItemData
{
    public BindableReactiveProperty<string> SourceText { get; } = new("");
    public BindableReactiveProperty<string> DestinationText { get; } = new("");
}
