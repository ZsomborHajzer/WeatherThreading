namespace WeatherThreading.Models;

public class Temperature
{
    public long Id {get; set;}
    public long LocationId {get; set;}
    public required double TemperatureMin {get; set;}
    public required double TemperatureMax {get; set;}
    public required double TemperatureAverage {get; set;}
}