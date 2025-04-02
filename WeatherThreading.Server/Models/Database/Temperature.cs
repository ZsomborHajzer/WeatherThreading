using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WeatherThreading.Models;

public class Temperature
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id {get; set;}

    public long LocationId {get; set;}
    
    public required double TemperatureMin {get; set;}

    public required double TemperatureMax {get; set;}

    public required double TemperatureAverage {get; set;}

    public required DateTime Date {get; set;}
}