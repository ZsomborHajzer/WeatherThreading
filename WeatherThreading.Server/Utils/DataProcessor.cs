using WeatherThreading.Models;

namespace WeatherThreading.Services;


public static class DataProcessor
{
    public static WeatherDataResponse MergeResults(WeatherData[] results)
    {
        if (results.Length == 0)
        {
            throw new Exception("No weather data results to merge");
        }


        var mergedResponse = new WeatherDataResponse
        {
            Latitude = results[0].Latitude,
            Longitude = results[0].Longitude,
            Daily = new Dictionary<string, List<object>>()
        };

        foreach (var result in results)
        {
            if (!mergedResponse.Daily.ContainsKey("Date"))
            {
                mergedResponse.Daily["Date"] = new List<object>();
            }
            mergedResponse.Daily["Date"].AddRange(result.Daily.Time.Select(t => (object)t));

            // Merge temperature data
            if (result.Daily.Temperature2mMax.Any())
            {
                if (!mergedResponse.Daily.ContainsKey("temperature_2m_max"))
                {
                    mergedResponse.Daily["temperature_2m_max"] = new List<object>();
                }
                mergedResponse.Daily["temperature_2m_max"].AddRange(result.Daily.Temperature2mMax.Select(t => (object)t));
            }

            if (result.Daily.Temperature2mMin.Any())
            {
                if (!mergedResponse.Daily.ContainsKey("temperature_2m_min"))
                {
                    mergedResponse.Daily["temperature_2m_min"] = new List<object>();
                }
                mergedResponse.Daily["temperature_2m_min"].AddRange(result.Daily.Temperature2mMin.Select(t => (object)t));
            }

            // Merge humidity data
            if (result.Daily.RelativeHumidity2m.Any())
            {
                if (!mergedResponse.Daily.ContainsKey("relative_humidity_2m"))
                {
                    mergedResponse.Daily["relative_humidity_2m"] = new List<object>();
                }
                mergedResponse.Daily["relative_humidity_2m"].AddRange(result.Daily.RelativeHumidity2m.Select(h => (object)h));
            }

            // Merge precipitation data
            if (result.Daily.PrecipitationSum.Any())
            {
                if (!mergedResponse.Daily.ContainsKey("precipitation_sum"))
                {
                    mergedResponse.Daily["precipitation_sum"] = new List<object>();
                }
                mergedResponse.Daily["precipitation_sum"].AddRange(result.Daily.PrecipitationSum.Select(p => (object)p));
            }

            if (result.Daily.PrecipitationHours.Any())
            {
                if (!mergedResponse.Daily.ContainsKey("precipitation_hours"))
                {
                    mergedResponse.Daily["precipitation_hours"] = new List<object>();
                }
                mergedResponse.Daily["precipitation_hours"].AddRange(result.Daily.PrecipitationHours.Select(p => (object)p));
            }

            // Merge wind speed data
            if (result.Daily.WindSpeed.Any())
            {
                if (!mergedResponse.Daily.ContainsKey("wind_speed_10m_max"))
                {
                    mergedResponse.Daily["wind_speed_10m_max"] = new List<object>();
                }
                mergedResponse.Daily["wind_speed_10m_max"].AddRange(result.Daily.WindSpeed.Select(w => (object)w));
            }

            // Merge radiation data
            if (result.Daily.ShortWaveRadiationSum.Any())
            {
                if (!mergedResponse.Daily.ContainsKey("shortwave_radiation_sum"))
                {
                    mergedResponse.Daily["shortwave_radiation_sum"] = new List<object>();
                }
                mergedResponse.Daily["shortwave_radiation_sum"].AddRange(result.Daily.ShortWaveRadiationSum.Select(s => (object)s));
            }
        }

        if (mergedResponse.Daily.ContainsKey("temperature_2m_max") && mergedResponse.Daily.ContainsKey("temperature_2m_min"))
        {
            var maxTemps = mergedResponse.Daily["temperature_2m_max"].Cast<double>().ToList();
            var minTemps = mergedResponse.Daily["temperature_2m_min"].Cast<double>().ToList();


            //calculating the average temperature using PLINQ and max processor core count
            mergedResponse.Daily["temperature_2m_avg"] = maxTemps
                .AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .Select((max, index) => (max + minTemps[index]) / 2.0)
                .Cast<object>()
                .ToList();
        }

        return mergedResponse;
    }

    public static WeatherDataGraphResponse FormatWeatherData(Dictionary<string, List<object>> result, WeatherDataRequest request)
    {
        if (!result.ContainsKey("Date") || result.Count != 2)
        {
            throw new ArgumentException("Input dictionary must contain 'Date' and one value axis.");
        }

        var valueKey = result.Keys.First(k => k != "Date");

        var dateValuePairs = result["Date"]
            .Select((x, i) => new
            {
                Date = DateTime.Parse(x.ToString()),
                Value = Convert.ToDouble(result[valueKey][i])
            })
            .Where(pair => pair.Date >= request.StartDate && pair.Date <= request.EndDate)
            .ToList();

        if (!dateValuePairs.Any())
        {
            throw new InvalidOperationException("No data points found in the specified date range.");
        }

        var chartData = dateValuePairs
            .Select(pair => new ChartDataPoint
            {
                xaxis = pair.Date,
                yaxis = pair.Value
            })
            .ToList();

        return new WeatherDataGraphResponse
        {
            XAxisTitle = "Date",
            YAxisTitle = valueKey,
            Daily = new Dictionary<string, List<ChartDataPoint>>
            {
                {"data", chartData}
            }
        };
    }
}