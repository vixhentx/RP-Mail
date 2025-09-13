using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
using Newtonsoft.Json;
using RPMailUI.Services.Attribute;
using RPMailUI.ViewModels;

namespace RPMailUI.Services;

public interface IPersistable
{
    public Timer? SaveTimer { get; set; }
    public bool IsDirty { get; set; }

    public HashSet<PropertyInfo>? PersistentProperties  { get; set; }

    public HashSet<PropertyInfo> Persistent
    {
        get
        {
            if (PersistentProperties == null)
            {
                var properties = GetType().GetProperties();
                var fields = GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
                HashSet<PropertyInfo> propertyInfos = [];
        
                foreach (var field in fields)
                {
                    if (field.GetCustomAttribute<Persisted>() == null) continue;
                    var propertyName = field.Name.TrimStart('_');
                    propertyName = char.ToUpper(propertyName[0]) + propertyName.Substring(1);

                    var property = properties.FirstOrDefault(p => p.Name == propertyName);
                    if (property == null) continue;
                    propertyInfos.Add(property);
                }
        
                foreach (var property in properties)
                {
                    if (property.GetCustomAttribute<Persisted>() == null) continue;
                    propertyInfos.Add(property);
                }
                PersistentProperties = propertyInfos;
            }
            return PersistentProperties;
        }
    }

    public void Save()
    {
        PersistHelper.ScheduleSave(this);
    }

    public void Load()
    {
        PersistHelper.Load(this);
    }
    public string SaveInner()
    {
        Dictionary<string, object> data = [];
        foreach (var property in Persistent)
        {
            data[property.Name] = property.GetValue(this) ?? "";
        }

        return JsonConvert.SerializeObject(data, Formatting.Indented);
    }

    public void LoadInner(string json)
    {
        var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
        if(data == null) return;
        foreach (var property in Persistent)
        {
            if(data.TryGetValue(property.Name, out var value))
            {
                property.SetValue(this, value);
            }
        }
    }

    public void Subscribe()
    {
        if(this is not ViewModelBase vm) return;
        
        HashSet<string> propertyNames = new (Persistent.Select(p => p.Name));

        vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName!=null && propertyNames.Contains(args.PropertyName))
            {
                Save();
            }
        };
    }
}