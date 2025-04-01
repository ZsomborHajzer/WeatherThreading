using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeatherThreading.Models;

namespace WeatherThreading.Server.Data.Configurations
{
    public class TemperatureConfiguration : IEntityTypeConfiguration<Temperature>
    {
        public void Configure(EntityTypeBuilder<Temperature> builder)
        {
            builder.HasKey(t => t.Id);
            builder.HasOne<Location>()
                .WithMany()
                .HasForeignKey(t => t.LocationId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Property(t => t.TemperatureMin).IsRequired();
            builder.Property(t => t.TemperatureMax).IsRequired();
            builder.Property(t => t.TemperatureAverage).IsRequired();
        }
    }
}
