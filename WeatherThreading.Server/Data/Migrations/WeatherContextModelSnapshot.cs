﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using WeatherThreading.Models;

#nullable disable

namespace WeatherThreading.Server.Data.Migrations
{
    [DbContext(typeof(WeatherContext))]
    partial class WeatherContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.2")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("WeatherThreading.Models.Location", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    b.Property<double>("Latitude")
                        .HasColumnType("double");

                    b.Property<string>("LocationName")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<double>("Longitude")
                        .HasColumnType("double");

                    b.HasKey("Id");

                    b.ToTable("Location");
                });

            modelBuilder.Entity("WeatherThreading.Models.Precipitation", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    b.Property<DateTime>("Date")
                        .HasColumnType("datetime(6)");

                    b.Property<long>("LocationId")
                        .HasColumnType("bigint");

                    b.Property<double>("PrecipitationSum")
                        .HasColumnType("double");

                    b.HasKey("Id");

                    b.ToTable("Precipitation");
                });

            modelBuilder.Entity("WeatherThreading.Models.PrecipitationHours", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    b.Property<DateTime>("Date")
                        .HasColumnType("datetime(6)");

                    b.Property<long>("LocationId")
                        .HasColumnType("bigint");

                    b.Property<double>("PrecipitationHoursValue")
                        .HasColumnType("double");

                    b.HasKey("Id");

                    b.ToTable("PrecipitationHours");
                });

            modelBuilder.Entity("WeatherThreading.Models.Radiation", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    b.Property<DateTime>("Date")
                        .HasColumnType("datetime(6)");

                    b.Property<long>("LocationId")
                        .HasColumnType("bigint");

                    b.Property<double>("ShortWaveRadiationSum")
                        .HasColumnType("double");

                    b.HasKey("Id");

                    b.ToTable("Radiation");
                });

            modelBuilder.Entity("WeatherThreading.Models.Temperature", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    b.Property<DateTime>("Date")
                        .HasColumnType("datetime(6)");

                    b.Property<long>("LocationId")
                        .HasColumnType("bigint");

                    b.Property<double>("TemperatureAverage")
                        .HasColumnType("double");

                    b.Property<double>("TemperatureMax")
                        .HasColumnType("double");

                    b.Property<double>("TemperatureMin")
                        .HasColumnType("double");

                    b.HasKey("Id");

                    b.ToTable("Temperature");
                });

            modelBuilder.Entity("WeatherThreading.Models.Wind", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    b.Property<DateTime>("Date")
                        .HasColumnType("datetime(6)");

                    b.Property<long>("LocationId")
                        .HasColumnType("bigint");

                    b.Property<double>("WindSpeedMax")
                        .HasColumnType("double");

                    b.HasKey("Id");

                    b.ToTable("Wind");
                });
#pragma warning restore 612, 618
        }
    }
}
