using System;

namespace RPMailUI.Services;

public class Persisted : Attribute
{
    public string? PropertyName { get; set; }
}