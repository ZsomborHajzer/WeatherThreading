using WeatherThreading.Models;
using Microsoft.EntityFrameworkCore;

namespace WeatherThreading.Services;

public class DBHandler
{
    private readonly WeatherContext _context;

    private List<string> TimeList { get; set; } = new List<string>();

    public DBHandler(WeatherContext context)
    {
        _context = context;
    }

    public async Task<Location> GetOrCreateLocationAsync(string locationName, double latitude, double longitude)
    {
        var location = await _context.Location.FirstOrDefaultAsync(l => l.LocationName == locationName);
        if (location == null)
        {
            location = new Location
            {
                LocationName = locationName,
                Latitude = latitude,
                Longitude = longitude
            };
            _context.Location.Add(location);
            await _context.SaveChangesAsync();
        }
        return location;
    }

    public async Task AddTemperatureBulk(WeatherDataResponse weatherData, List<string> timeList, Location location)
    {
        var temperatures = new List<Temperature>();

        var temperatureMaxList = weatherData.Daily["temperature_2m_max"].Cast<double>().ToList();
        var temperatureMinList = weatherData.Daily["temperature_2m_min"].Cast<double>().ToList();

        var temperatureAverages = new double[temperatureMaxList.Count];

        //! PLINQ to calculate the average temperature
        Parallel.For(0, temperatureMaxList.Count, i =>
        {
            temperatureAverages[i] = (temperatureMaxList[i] + temperatureMinList[i]) / 2;
        });

        for (int i = 0; i < temperatureMaxList.Count; i++)
        {
            var temperature = new Temperature
            {
                LocationId = location.Id,
                TemperatureMax = temperatureMaxList[i],
                TemperatureMin = temperatureMinList[i],
                TemperatureAverage = (temperatureMaxList[i] + (weatherData.Daily.ContainsKey("temperature_2m_min") ? weatherData.Daily["temperature_2m_min"].Cast<double>().ToList()[i] : 0)) / 2,
                Date = DateTime.Parse(timeList[i])
            };
            temperatures.Add(temperature);
        }

        _context.Temperature.AddRange(temperatures);
        await _context.SaveChangesAsync();
    }

    public async Task AddPrecipitationHoursBulk(WeatherDataResponse weatherData, List<string> timeList, Location location)
    {
        var precipitationHours = new List<PrecipitationHours>();
        var precipitationHoursList = weatherData.Daily["precipitation_hours"].Cast<double>().ToList();

        for (int i = 0; i < precipitationHoursList.Count; i++)
        {
            var precipitationHour = new PrecipitationHours
            {
                LocationId = location.Id,
                PrecipitationHoursValue = precipitationHoursList[i],
                Date = DateTime.Parse(timeList[i])
            };
            precipitationHours.Add(precipitationHour);
        }
        _context.PrecipitationHours.AddRange(precipitationHours);
        await _context.SaveChangesAsync();
    }

        public async Task AddPrecipitationSumBulk(WeatherDataResponse weatherData, List<string> timeList, Location location)
    {
        var precipitationSums = new List<Precipitation>();
        var precipitationSumList = weatherData.Daily["precipitation_sum"].Cast<double>().ToList();
        for (int i = 0; i < precipitationSumList.Count; i++)
        {
            var precipitation = new Precipitation
            {
                LocationId = location.Id,
                PrecipitationSum = precipitationSumList[i],
                Date = DateTime.Parse(timeList[i])
            };
            precipitationSums.Add(precipitation);
        }
        _context.Precipitation.AddRange(precipitationSums);
        await _context.SaveChangesAsync();
    }

    public async Task AddRadiationBulk(WeatherDataResponse weatherData, List<string> timeList, Location location)
    {
        var radiations = new List<Radiation>();
        var shortWaveRadiationSumList = weatherData.Daily["shortwave_radiation_sum"].Cast<double>().ToList();

        for (int i = 0; i < shortWaveRadiationSumList.Count; i++)
        {
            var radiation = new Radiation
            {
                LocationId = location.Id,
                ShortWaveRadiationSum = shortWaveRadiationSumList[i],
                Date = DateTime.Parse(timeList[i])
            };
            radiations.Add(radiation);
        }

        _context.Radiation.AddRange(radiations);
        await _context.SaveChangesAsync();
    }

    public async Task AddWindBulk(WeatherDataResponse weatherData, List<string> timeList, Location location)
    {
        var winds = new List<Wind>();

        var windSpeedList = weatherData.Daily["wind_speed_10m_max"].Cast<double>().ToList();

        for (int i = 0; i < windSpeedList.Count; i++)
        {
            var wind = new Wind
            {
                LocationId = location.Id,
                WindSpeedMax = windSpeedList[i],
                Date = DateTime.Parse(timeList[i])
            };
            winds.Add(wind);
        }
        _context.Wind.AddRange(winds);
        await _context.SaveChangesAsync();
    }
}