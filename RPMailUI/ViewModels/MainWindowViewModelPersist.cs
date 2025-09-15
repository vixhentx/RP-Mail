using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Timers;
using DynamicData;
using RPMailUI.Models;
using RPMailUI.Services;

namespace RPMailUI.ViewModels;

public partial class MainWindowViewModel : IPersistable
{
    public Timer SaveTimer { get; }
    public string SettingsPath => "RPMailUI-Persisted.json";
    public PropertyInfo[] PropertyCache { get; }

    public MainWindowViewModel()
    {
        SaveTimer = Persistable.CreateTimer();
        PropertyCache = Persistable.GenCache();
        Persistable.Load();
    }
    
#pragma warning disable CA1859
    private IPersistable Persistable => this;
#pragma warning restore CA1859

}