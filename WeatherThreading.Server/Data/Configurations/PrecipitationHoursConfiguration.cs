using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeatherThreading.Models;

namespace WeatherThreading.Server.Data.Configurations
{
    public class PrecipitationHoursConfiguration : IEntityTypeConfiguration<PrecipitationHours>
    {
        public void Configure(EntityTypeBuilder<PrecipitationHours> builder)
        {
            builder.HasKey(p => p.Id);
            builder.HasOne<Location>()
                .WithMany()
                .HasForeignKey(p => p.LocationId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Property(p => p.PrecipitationHoursValue).IsRequired();
        }
    }
}
