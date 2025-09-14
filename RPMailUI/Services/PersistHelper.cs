using System;
using System.IO;
using System.Threading.Tasks;
using MsBox.Avalonia;

namespace RPMailUI.Services;

public static class PersistHelper
{
    private const string SettingsPath = "RPMailUI-Persisted.json";

    public static void ScheduleSave<T>(IPersistable<T> target)
    {
        target.SaveTimer.Stop();
        target.SaveTimer.Start();
    }

    public static void SaveInstantly<T>(IPersistable<T> target)
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
    
    public static void Load<T>(IPersistable<T> target)
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
    
    public static async Task SaveInner<T>(IPersistable<T> target)
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