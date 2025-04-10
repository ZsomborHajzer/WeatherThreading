
namespace WeatherThreading.Services;

/*
    * This creates a list of time ranges from the user input
    * It defaults the first date to the first day of the selected year
    * and the last date to the last day of the selected year
*/
public class TimeRangeTools
{
    private const int MaxDaysPerChunk = 365; // Maximum days per API request

    public static List<(DateTime Start, DateTime End)> SplitTimeRange(DateTime startDate, DateTime endDate)
    {
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
} 
