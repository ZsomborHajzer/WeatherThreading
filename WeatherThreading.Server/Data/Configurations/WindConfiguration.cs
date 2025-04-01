using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeatherThreading.Models;

namespace WeatherThreading.Server.Data.Configurations
{
    public class WindConfiguration : IEntityTypeConfiguration<Wind>
    {
        public void Configure(EntityTypeBuilder<Wind> builder)
        {
            builder.HasKey(w => w.Id);
            builder.HasOne<Location>()
                .WithMany()
                .HasForeignKey(w => w.LocationId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Property(w => w.WindSpeedMax).IsRequired();
        }
    }
}