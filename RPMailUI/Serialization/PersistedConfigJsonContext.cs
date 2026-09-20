using System.Text.Json.Serialization;
using RPMailUI.Models;

namespace RPMailUI.Serialization;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true)]
[JsonSerializable(typeof(PersistedConfig))]
public partial class PersistedConfigJsonContext : JsonSerializerContext;
