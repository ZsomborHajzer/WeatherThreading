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

        var existingDates = await _context.Temperature
            .Where(t => t.LocationId == location.Id)
            .Select(t => t.Date.Date)
            .ToListAsync();

        var temperatureAverages = new double[temperatureMaxList.Count];

        Parallel.For(0, temperatureMaxList.Count, i =>
        {
            temperatureAverages[i] = (temperatureMaxList[i] + temperatureMinList[i]) / 2;
        });

        for (int i = 0; i < temperatureMaxList.Count; i++)
        {
            var date = DateTime.Parse(timeList[i]).Date;
            
            if (existingDates.Contains(date))
                continue;

            var temperature = new Temperature
            {
                LocationId = location.Id,
                TemperatureMax = temperatureMaxList[i],
                TemperatureMin = temperatureMinList[i],
                TemperatureAverage = temperatureAverages[i],
                Date = date
            };
            temperatures.Add(temperature);
        }

        if (temperatures.Any())
        {
            _context.Temperature.AddRange(temperatures);
            await _context.SaveChangesAsync();
        }
    }

    public async Task AddPrecipitationHoursBulk(WeatherDataResponse weatherData, List<string> timeList, Location location)
    {
        var precipitationHours = new List<PrecipitationHours>();
        var precipitationHoursList = weatherData.Daily["precipitation_hours"].Cast<double>().ToList();

        var existingDates = await _context.PrecipitationHours
            .Where(p => p.LocationId == location.Id)
            .Select(p => p.Date.Date)
            .ToListAsync();

        for (int i = 0; i < precipitationHoursList.Count; i++)
        {
            var date = DateTime.Parse(timeList[i]).Date;

            if (existingDates.Contains(date))
                continue;

            var precipitationHour = new PrecipitationHours
            {
                LocationId = location.Id,
                PrecipitationHoursValue = precipitationHoursList[i],
                Date = date
            };
            precipitationHours.Add(precipitationHour);
        }

        if (precipitationHours.Any())
        {
            _context.PrecipitationHours.AddRange(precipitationHours);
            await _context.SaveChangesAsync();
        }
    }

    public async Task AddPrecipitationSumBulk(WeatherDataResponse weatherData, List<string> timeList, Location location)
    {
        var precipitationSums = new List<Precipitation>();
        var precipitationSumList = weatherData.Daily["precipitation_sum"].Cast<double>().ToList();

        var existingDates = await _context.Precipitation
            .Where(p => p.LocationId == location.Id)
            .Select(p => p.Date.Date)
            .ToListAsync();

        for (int i = 0; i < precipitationSumList.Count; i++)
        {
            var date = DateTime.Parse(timeList[i]).Date;

            if (existingDates.Contains(date))
                continue;

            var precipitation = new Precipitation
            {
                LocationId = location.Id,
                PrecipitationSum = precipitationSumList[i],
                Date = date
            };
            precipitationSums.Add(precipitation);
        }

        if (precipitationSums.Any())
        {
            _context.Precipitation.AddRange(precipitationSums);
            await _context.SaveChangesAsync();
        }
    }

    public async Task AddRadiationBulk(WeatherDataResponse weatherData, List<string> timeList, Location location)
    {
        var radiations = new List<Radiation>();
        var shortWaveRadiationSumList = weatherData.Daily["shortwave_radiation_sum"].Cast<double>().ToList();

        var existingDates = await _context.Radiation
            .Where(r => r.LocationId == location.Id)
            .Select(r => r.Date.Date)
            .ToListAsync();

        for (int i = 0; i < shortWaveRadiationSumList.Count; i++)
        {
            var date = DateTime.Parse(timeList[i]).Date;

            if (existingDates.Contains(date))
                continue;

            var radiation = new Radiation
            {
                LocationId = location.Id,
                ShortWaveRadiationSum = shortWaveRadiationSumList[i],
                Date = date
            };
            radiations.Add(radiation);
        }

        if (radiations.Any())
        {
            _context.Radiation.AddRange(radiations);
            await _context.SaveChangesAsync();
        }
    }

    public async Task AddWindBulk(WeatherDataResponse weatherData, List<string> timeList, Location location)
    {
        var winds = new List<Wind>();
        var windSpeedList = weatherData.Daily["wind_speed_10m_max"].Cast<double>().ToList();

        var existingDates = await _context.Wind
            .Where(w => w.LocationId == location.Id)
            .Select(w => w.Date.Date)
            .ToListAsync();

        for (int i = 0; i < windSpeedList.Count; i++)
        {
            var date = DateTime.Parse(timeList[i]).Date;

            if (existingDates.Contains(date))
                continue;

            var wind = new Wind
            {
                LocationId = location.Id,
                WindSpeedMax = windSpeedList[i],
                Date = date
            };
            winds.Add(wind);
        }

        if (winds.Any())
        {
            _context.Wind.AddRange(winds);
            await _context.SaveChangesAsync();
        }
    }
}