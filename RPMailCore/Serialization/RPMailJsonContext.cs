using System.Text.Json.Serialization;
using RPMailCore.Models;

namespace RPMailCore.Serialization;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true)]
[JsonSerializable(typeof(MailConfig))]
public partial class RPMailJsonContext : JsonSerializerContext;
