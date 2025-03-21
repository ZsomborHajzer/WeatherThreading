namespace WeatherThreading.Models;

public class Wind
{
    public long Id {get; set;}
    public long LocationId {get; set;}
    public required double WindSpeedMax {get; set;}
    public required double GustSpeedMax {get; set;}
}