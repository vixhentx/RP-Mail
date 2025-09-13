using System;

namespace RPMailUI.Services.Attribute;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class Persisted : System.Attribute;