using System.Text.Json.Serialization;

namespace WeatherThreading.Models;

public class WeatherDataRequest
{
    [JsonPropertyName("location")]
    public required string Location { get; set; }

    [JsonPropertyName("startDate")]
    public DateTime StartDate { get; set; }

    [JsonPropertyName("endDate")]
    public DateTime EndDate { get; set; }

    [JsonPropertyName("parameters")]
    public List<string> Parameters { get; set; } = new();
}

public class WeatherDataResponse
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("timezone")]
    public string Timezone { get; set; } = string.Empty;

    [JsonPropertyName("daily")]
    public Dictionary<string, List<object>> Daily { get; set; } = new();
} 