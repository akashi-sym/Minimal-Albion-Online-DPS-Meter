using System.Text.Json.Serialization;

namespace AlbionDpsMeter.Models;

public class ItemData
{
    [JsonPropertyName("Index")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int Index { get; set; }

    [JsonPropertyName("UniqueName")]
    public string UniqueName { get; set; } = string.Empty;
}
