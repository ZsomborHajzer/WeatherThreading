namespace WeatherThreading.Models;

public class Location
{
    public long Id {get; set;}
    public required string LocationName {get; set;}
    public required double Latitude {get; set;}
    public required double Longitude {get; set;}
}