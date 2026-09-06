using R3;

namespace RPMailUI.Models;

public sealed class ExtraAttributeItemData
{
    public BindableReactiveProperty<string> Key { get; } = new("");
    public BindableReactiveProperty<string> Value { get; } = new("");
}
