using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Timers;
using Newtonsoft.Json;

namespace RPMailUI.Services;

public interface IPersistable<T>
{
    public Timer SaveTimer { get; set; }

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
                SaveTimer.Stop();
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
        SaveTimer = CreateTimer;
    }

    public void Subscribe()
    {
        if(this is not INotifyPropertyChanged vm) return;
        var properties = typeof(T).GetProperties();
        HashSet<string> propertyNames = [];
        foreach (var property in properties)
        {
            propertyNames.Add(property.Name);
        }
        
        // if (this is ReactiveObject reactiveObject)
        // {
        //     foreach(var propertyName in propertyNames)
        //     {
        //         var property = GetType().GetProperty(propertyName);
        //         if (property == null) continue;
        //         reactiveObject.WhenAnyValue(x=> property.GetValue(x))
        //             .Throttle(TimeSpan.FromMicroseconds(500))
        //             .Subscribe(_=>SaveInstantly());
        //     }
        // }
        // else
        {
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName!=null && propertyNames.Contains(args.PropertyName))
                {
                    Save();
                }
            };
        }
    }
}