using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeatherThreading.Models;

namespace WeatherThreading.Server.Data.Configurations
{
    public class PrecipitationConfiguration : IEntityTypeConfiguration<Precipitation>
    {
        public void Configure(EntityTypeBuilder<Precipitation> builder)
        {
            builder.HasKey(p => p.Id);
            builder.HasOne<Location>()
                .WithMany()
                .HasForeignKey(p => p.LocationId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Property(p => p.PrecipitationSum).IsRequired();
        }
    }
}
