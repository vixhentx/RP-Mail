using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
using RPMailUI.Models;
using RPMailUI.Services;

namespace RPMailUI.ViewModels;

public partial class MainWindowViewModel : IPersistable
{
    public Timer? SaveTimer { get; set; }
    public bool IsDirty { get; set; }
    public HashSet<PropertyInfo>? PersistentProperties { get; set; }

    public void OnWindowClosing(object sender, CancelEventArgs e) => PersistHelper.SaveInstantly(this);
}