using Microsoft.EntityFrameworkCore;
using WeatherThreading.Server;
using WeatherThreading.Models;

namespace WeatherThreading.Models;

public class WeatherContext : DbContext
{
    public WeatherContext(DbContextOptions<WeatherContext> options)
        : base(options)
    {
    }

    public DbSet<Location> Location { get; set; } = default!;
    public DbSet<Temperature> Temperature { get; set; } = default!;
    public DbSet<Precipitation> Precipitation { get; set; } = default!;
    public DbSet<Wind> Wind { get; set; } = default!;
    public DbSet<PrecipitationHours> PrecipitationHours { get; set; } = default!;
    public DbSet<Radiation> Radiation { get; set; } = default!;
}