using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Timers;
using Newtonsoft.Json;

namespace RPMailUI.Services;

public interface IPersistable<T>
{
    public Timer? SaveTimer { get; set; }

    public bool IsDirty
    {
        set
        {
            if (value) Save();
        }
    }
    
    public T Data { get; set; }

    public Timer CreateTimer
    {
        get
        {
            Timer timer = new(500)
            {
                AutoReset = false
            };
            timer.Elapsed += (_, _) =>
            {
                SaveInstantly();
                timer.Stop();
            };
            return timer;
        }
    }
    
    public void Save()
    {
        PersistHelper.ScheduleSave(this);
    }

    public void SaveInstantly()
    {
        PersistHelper.SaveInstantly(this);
    }
    public async Task SaveInstantlyAsync()
    {
        await PersistHelper.SaveInner(this);
    }

    public void Load()
    {
        PersistHelper.Load(this);
    }
    public string SaveInner()
    {
        return JsonConvert.SerializeObject(Data, Formatting.Indented);
    }

    public void LoadInner(string json)
    {
        var data = JsonConvert.DeserializeObject<T>(json);
        if(data == null) return;
        Data = data;
    }

    public void Subscribe()
    {
        if (this is not INotifyPropertyChanged vm) return;
        var properties = typeof(T).GetProperties();
        HashSet<string> propertyNames = [];
        foreach (var dataProperty in properties)
        {
            var name =  dataProperty.Name;
            propertyNames.Add(name);
            var property = GetType().GetProperty(name)!;
            var type = property.PropertyType;
            //For collection
            if (typeof(INotifyCollectionChanged).IsAssignableFrom(type) &&
                property.GetValue(this) is INotifyCollectionChanged coll)
            {
                coll.CollectionChanged += (s, e) =>
                {
                    if (e.NewItems != null)
                    {
                        foreach (var item in e.NewItems)
                        {
                            if (item is INotifyPropertyChanged inpc)
                                inpc.PropertyChanged += SaveHandler;
                        }
                    }

                    if (e.OldItems != null)
                    {
                        foreach (var item in e.OldItems)
                        {
                            if (item is INotifyPropertyChanged inpc)
                                inpc.PropertyChanged -= SaveHandler;
                        }
                    }
                };

                foreach (var item in (IEnumerable)coll)
                {
                    if (item is INotifyPropertyChanged inpc)
                        inpc.PropertyChanged += (_, _) => Save();
                }
            }
        }
        vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName != null && propertyNames.Contains(args.PropertyName))
            {
                Save();
            }
        };
    }
    private void SaveHandler(object? sender, EventArgs e) =>
        Save();
}