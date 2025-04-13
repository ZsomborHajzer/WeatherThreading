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

    [JsonPropertyName("daily")]
    public Dictionary<string, List<object>> Daily { get; set; } = new();
} 

public class WeatherDataGraphResponse
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("x-axis-title")]
    public string XAxisTitle { get; set; } = string.Empty;

    [JsonPropertyName("y-axis-title")]
    public string YAxisTitle { get; set; } = string.Empty;

    [JsonPropertyName("daily")]
    public Dictionary<string, List<ChartDataPoint>> Daily { get; set; } = new();
} 