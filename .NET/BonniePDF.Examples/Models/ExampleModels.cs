using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BonniePDF.Examples.Models;

public class ExampleCategory
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<ExampleInfo> Examples { get; set; } = new();
}

public class ExampleInfo
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    [JsonIgnore]
    public string FileName { get; set; } = string.Empty;

    [JsonIgnore]
    public string[]? AdditionalInfo { get; set; }

    [JsonIgnore]
    public Action<string>? GenerateAction { get; set; }
}
