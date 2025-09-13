using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using MsBox.Avalonia;
using Newtonsoft.Json;
using RPMailUI.Services.Attribute;

namespace RPMailUI.Services;

public static class PersistHelper
{
    private const string SettingsPath = "RPMailUI-Persisted.json";

    public static void ScheduleSave(IPersistable target)
    {
        target.IsDirty = true;
        if (target.SaveTimer?.Interval > 0)
        {
            target.SaveTimer.Interval = 500;
        }
        else
        {
            target.SaveTimer = new(500)
            {
                AutoReset = false
            };
            target.SaveTimer.Elapsed += async (sender, args) =>
            {
                if (target.IsDirty)
                {
                    await SaveInner(target);
                    target.IsDirty = false;
                }
                target.SaveTimer.Stop();
            };
        }
    }

    public static void SaveInstantly(IPersistable target)
    {
        string json = target.SaveInner();
        try
        {
            File.WriteAllText(SettingsPath, json);
        }
        catch (Exception e)
        {
            MessageBoxManager.GetMessageBoxStandard("Save Error", e.Message).ShowAsync().RunSynchronously();
        }
    }
    
    public static void Load(IPersistable target)
    {
        if(File.Exists(SettingsPath))
            try
            {
                string json = File.ReadAllText(SettingsPath);
                target.LoadInner(json);
            }
            catch
            {
                //
            }
        
        target.Subscribe();
    }
    
    private static async Task SaveInner(IPersistable target)
    {
        string json = target.SaveInner();
        try
        {
            await File.WriteAllTextAsync(SettingsPath, json);
        }
        catch (Exception e)
        {
            await MessageBoxManager.GetMessageBoxStandard("Save Error", e.Message).ShowAsync();
        }
    }
}