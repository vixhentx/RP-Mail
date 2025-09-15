using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RPMailUI.Services;

public interface IPersistable
{
    #region Properties

    public Timer SaveTimer { get;}   // = CreateTimer();
    public string SettingsPath { get; }
    public PropertyInfo[] PropertyCache { get;}    // = GenCache();

    #endregion

    public Timer CreateTimer(double interval = 500)
    {
        Timer timer = new(interval)
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
    public PropertyInfo[] GenCache()
    {
        var properties = GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
        return properties.Where(p => p.GetCustomAttribute<Persisted>() != null)
            .ToArray();
    }
    
    public void Save()
    {
        SaveTimer.Stop();
        SaveTimer.Start();
    }

    public void SaveInstantly()
    {
        string json = SaveInner();
        try
        {
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            //
        }
    }
    public async Task SaveInstantlyAsync()
    {
        string json = SaveInner();
        try
        {
            await File.WriteAllTextAsync(SettingsPath, json);
        }
        catch
        {
            //
        }
    }

    public string SaveInner()
    {
        Dictionary<string, object?> map = [];
        foreach (var property in PropertyCache)
        {
            var name = property.Name;
            var value = property.GetValue(this);
            map[name] = value;
        }
        return JsonConvert.SerializeObject(map, Formatting.Indented);
    }

    public void Load()
    {
        if(File.Exists(SettingsPath))
            try
            {
                string json = File.ReadAllText(SettingsPath);
                LoadInner(json);
            }
            catch
            {
                //
            }
        Subscribe();
    }

    public void LoadInner(string json)
    {
        var map = JsonConvert.DeserializeObject<Dictionary<string, JToken?>>(json);
        if(map == null) return;
        foreach (var property in PropertyCache)
        {
            var data = map[property.Name];
            if (data == null) continue;
            var targetType = property.PropertyType;
            
            //IEnumerable<T>
            if (targetType.IsGenericType &&
                typeof(IEnumerable<>).IsAssignableFrom(targetType.GetGenericTypeDefinition()) &&
                data is JArray jArray)
            {
                var elementType = targetType.GetGenericArguments()[0];  //T of IEnumerable<T>
                var listType = typeof(List<>).MakeGenericType(elementType); //List<T>
                var listValue = jArray.ToObject(listType);  //(List<T>)
                
                var constructor = targetType.GetConstructor([typeof(IEnumerable<>).MakeGenericType(elementType)]);  //new(IEnumerable<T>)
                if (constructor != null)
                {
                    var collectionValue = constructor.Invoke([listValue]);
                    property.SetValue(this, collectionValue);
                    continue;
                }
            }
            //Others
            var convertedValue = data.ToObject(targetType);
            property.SetValue(this, convertedValue);
        }
    }

    public void Subscribe()
    {
        if (this is not INotifyPropertyChanged vm) return;
        HashSet<string> propertyNames = [];
        foreach (var property in PropertyCache)
        {
            var name =  property.Name;
            var type = property.PropertyType;
            propertyNames.Add(name);
            //For collection
            if (typeof(INotifyCollectionChanged).IsAssignableFrom(type) &&
                property.GetValue(this) is INotifyCollectionChanged coll)
            {
                //patch changed
                coll.CollectionChanged += (_, e) =>
                {
                    if (e.NewItems != null)
                    {
                        foreach (var item in e.NewItems)
                        {
                            if (item is INotifyPropertyChanged notifiable)
                                notifiable.PropertyChanged += SaveHandler;
                        }
                    }

                    if (e.OldItems != null)
                    {
                        foreach (var item in e.OldItems)
                        {
                            if (item is INotifyPropertyChanged notifiable)
                                notifiable.PropertyChanged -= SaveHandler;
                        }
                    }
                };
                //patch existing
                foreach (var item in (IEnumerable)coll)
                {
                    if (item is INotifyPropertyChanged changeable)
                        changeable.PropertyChanged += SaveHandler;
                }
            }
        }
        //notify
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