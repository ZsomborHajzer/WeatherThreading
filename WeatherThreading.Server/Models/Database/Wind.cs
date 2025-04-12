using System.ComponentModel.DataAnnotations.Schema;

namespace WeatherThreading.Models;

public class Wind
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id {get; set;}

    public long LocationId {get; set;}

    public required double WindSpeedMax {get; set;}

    public required DateTime Date {get; set;}
}