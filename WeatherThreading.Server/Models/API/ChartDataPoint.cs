using System.Text.Json.Serialization;

namespace WeatherThreading.Models;

public class ChartDataPoint
{
    
    [JsonPropertyName("xaxis")]
    public DateTime xaxis { get; set; }

    [JsonPropertyName("yaxis")]
    public double yaxis { get; set; } = new();
}
