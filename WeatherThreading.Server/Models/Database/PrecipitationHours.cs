using System.ComponentModel.DataAnnotations.Schema;

namespace WeatherThreading.Models;

public class PrecipitationHours
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id {get; set;}

    public long LocationId {get; set;}

    public required double PrecipitationHoursValue {get; set;}

    public required DateTime Date {get; set;}
}