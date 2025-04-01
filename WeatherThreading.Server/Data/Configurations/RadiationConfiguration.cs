using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeatherThreading.Models;

namespace WeatherThreading.Server.Data.Configurations
{
    public class RadiationConfiguration : IEntityTypeConfiguration<Radiation>
    {
        public void Configure(EntityTypeBuilder<Radiation> builder)
        {
            builder.HasKey(r => r.Id);
            builder.HasOne<Location>()
                .WithMany()
                .HasForeignKey(r => r.LocationId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Property(r => r.ShortWaveRadiationSum).IsRequired();
        }
    }
}