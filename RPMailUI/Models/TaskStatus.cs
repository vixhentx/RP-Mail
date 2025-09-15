

using Avalonia.Media;

namespace RPMailUI.Models;

public class TaskStatus(string text, Color color, int ordinal)
{
    public string Text { get; } = text;
    public Color Color { get; } = color;
    
    public int Ordinal = ordinal;
    
    public static readonly TaskStatus Ready = new TaskStatus("Ready", Colors.White, 2);
    public static readonly TaskStatus Pending = new ("Pending", Colors.LightGray, 1);
    public static readonly TaskStatus Running = new("Running", Colors.Cyan,0);
    public static readonly TaskStatus Success = new ("Success", Colors.Green,3);
    public static readonly TaskStatus Failed = new ("Failed", Colors.Red,4);
}