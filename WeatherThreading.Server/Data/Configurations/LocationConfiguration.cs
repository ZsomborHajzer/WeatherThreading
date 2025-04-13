using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeatherThreading.Models;

namespace WeatherThreading.Server.Data.Configurations
{
    public class LocationConfiguration : IEntityTypeConfiguration<Location>
    {
        public void Configure(EntityTypeBuilder<Location> builder)
        {
            builder.HasKey(l => l.Id);
            builder.HasIndex(l => new { l.Latitude, l.Longitude }).IsUnique();
            builder.Property(l => l.LocationName).IsRequired();
            builder.Property(l => l.Latitude).IsRequired();
            builder.Property(l => l.Longitude).IsRequired();
        }
    }
}