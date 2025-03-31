using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WeatherThreading.Models;

public class Wind
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id {get; set;}

    public long LocationId {get; set;}

    public required double WindSpeedMax {get; set;}
}