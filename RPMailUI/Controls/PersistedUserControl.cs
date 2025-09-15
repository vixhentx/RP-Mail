using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace RPMailUI.Controls;

public class PersistedUserControl : UserControl
{
    public static readonly StyledProperty<bool> IsDirtyProperty = AvaloniaProperty.Register<PersistedUserControl, bool>(
        nameof(IsDirty), defaultBindingMode: BindingMode.TwoWay);

    public bool IsDirty
    {
        get => GetValue(IsDirtyProperty);
        set => SetValue(IsDirtyProperty, value);
    }
}