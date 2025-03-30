namespace WeatherThreading.Services;

public class TimeRangeSplitter
{
    private const int MaxDaysPerChunk = 365; // Maximum days per API request

    public static List<(DateTime Start, DateTime End)> SplitTimeRange(DateTime startDate, DateTime endDate)
    {
        // Ensure end date is not in the future
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