
using Microsoft.EntityFrameworkCore;
using WeatherThreading.Models;

namespace WeatherThreading.Services;

public class TimeRangeTools
{
    private const int MaxDaysPerChunk = 365; // Maximum days per API request

    public static List<(DateTime Start, DateTime End)> SplitTimeRange(DateTime startDate, DateTime endDate)
    {
        /*
            * This creates a list of time ranges from the user input
            * It defaults the first date to the first day of the selected year
            * and the last date to the last day of the selected year
        */
        startDate = new DateTime(startDate.Year, 1, 1);
        endDate = new DateTime(endDate.Year, 12, 31);

        var currentDate = DateTime.UtcNow.Date;
        if (endDate > currentDate)
        {
            endDate = currentDate;
        }

        var chunks = new List<(DateTime Start, DateTime End)>();
        var currentStart = startDate;

        while (currentStart < endDate)
        {
            var currentEnd = currentStart.AddDays(MaxDaysPerChunk);
            if (currentEnd > endDate)
            {
                currentEnd = endDate;
            }

            chunks.Add((currentStart, currentEnd));
            currentStart = currentEnd.AddDays(1);
        }

        return chunks;
    }

    public async static Task<bool> AreDatesContinuousAsync(WeatherDataRequest request, WeatherContext _context)
    {
        {
            var expectedDates = Enumerable.Range(0, (request.EndDate - request.StartDate).Days + 1)
                                        .Select(offset => request.StartDate.Date.AddDays(offset))
                                        .ToHashSet();

            IEnumerable<DateTime> existingDates;

            var parameterKey = request.Parameters.FirstOrDefault();

            if (string.IsNullOrEmpty(parameterKey) || !ParameterMappings.TableNameMapping.ContainsKey(parameterKey))
            {
                throw new ArgumentException("Invalid or missing parameter in request.");
            }

            var locationObject = await _context.Location
                .FirstOrDefaultAsync(l => l.LocationName == request.Location);

            if (locationObject == null)
            {
                throw new ArgumentException($"Location '{request.Location}' not found.");
            }

            var locationId = locationObject.Id;

            var tableName = ParameterMappings.TableNameMapping[parameterKey];

            switch (tableName)
            {
                case "Temperature":
                    existingDates = await _context.Temperature
                        .Where(x => x.LocationId == locationId && x.Date >= request.StartDate && x.Date <= request.EndDate)
                        .Select(x => x.Date.Date)
                        .Distinct()
                        .ToListAsync();
                    break;

                case "Precipitation":
                    existingDates = await _context.Precipitation
                        .Where(x => x.LocationId == locationId && x.Date >= request.StartDate && x.Date <= request.EndDate)
                        .Select(x => x.Date.Date)
                        .Distinct()
                        .ToListAsync();
                    break;

                case "PrecipitationHours":
                    existingDates = await _context.PrecipitationHours
                        .Where(x => x.LocationId == locationId && x.Date >= request.StartDate && x.Date <= request.EndDate)
                        .Select(x => x.Date.Date)
                        .Distinct()
                        .ToListAsync();
                    break;

                case "Radiation":
                    existingDates = await _context.Radiation
                        .Where(x => x.LocationId == locationId && x.Date >= request.StartDate && x.Date <= request.EndDate)
                        .Select(x => x.Date.Date)
                        .Distinct()
                        .ToListAsync();
                    break;

                case "Wind":
                    existingDates = await _context.Wind
                        .Where(x => x.LocationId == locationId && x.Date >= request.StartDate && x.Date <= request.EndDate)
                        .Select(x => x.Date.Date)
                        .Distinct()
                        .ToListAsync();
                    break;

                default:
                    throw new ArgumentException($"Invalid table name: {tableName}");
            }

            var existingDateSet = existingDates.ToHashSet();

            return expectedDates.SetEquals(existingDateSet);
        }
    }
}
