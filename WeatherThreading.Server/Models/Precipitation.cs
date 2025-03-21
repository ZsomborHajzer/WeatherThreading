namespace WeatherThreading.Models;

public class Precipitation
{
    public long Id {get; set;}
    public long LocationId {get; set;}
    public required double PrecipitationSum {get; set;}
    public required double RainfallSum {get; set;}
    public required double SnowfallSum {get; set;}
}