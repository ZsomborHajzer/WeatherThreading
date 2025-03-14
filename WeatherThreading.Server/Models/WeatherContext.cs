using Microsoft.EntityFrameworkCore;
using WeatherThreading.Server;

namespace TodoApi.Models;

public class WeatherContext : DbContext
{
    public WeatherContext(DbContextOptions<WeatherContext> options)
        : base(options)
    {
    }

    public DbSet<WeatherData> WeatherDatas { get; set; } = null!;
}