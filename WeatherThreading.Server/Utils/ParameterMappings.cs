namespace WeatherThreading.Services;

public class ParameterMappings {

    public static readonly Dictionary<string, string> RequestDataMapping = new()
    {
        { "temperature", "temperature_2m_max,temperature_2m_min" },
        { "relative_humidity_2m", "relative_humidity_2m_mean" },
        { "precipitation_sum", "precipitation_sum" },
        { "precipitation_hours", "precipitation_hours" },
        { "wind_speed_10m_max", "wind_speed_10m_max" },
        { "shortwave_radiation_sum", "shortwave_radiation_sum" }
    };

    public static readonly Dictionary<string, string> TableNameMapping = new()
    {
        { "temperature", "Temperature" },
        { "precipitation_sum", "Precipitation" },
        { "precipitation_hours", "PrecipitationHours" },
        { "wind_speed_10m_max", "Wind" },
        { "shortwave_radiation_sum", "Radiation" }
    };

    public static readonly Dictionary<string, string> TableColumnMapping = new()
    {
        { "temperature", "TemperatureAverage" },
        { "precipitation_sum", "PrecipitationSum" },
        { "precipitation_hours", "PrecipitationHoursValue" },
        { "wind_speed_10m_max", "WindSpeedMax" },
        { "shortwave_radiation_sum", "ShortWaveRadiationSum" }
    };

    public static readonly Dictionary<string, string> RequestColumnsMapping = new()
    {
        { "temperature", "TemperatureAverage, Date" },
        { "precipitation_sum", "PrecipitationSum, Date" },
        { "precipitation_hours", "PrecipitationHoursValue, Date" },
        { "wind_speed_10m_max", "WindSpeedMax, Date" },
        { "shortwave_radiation_sum", "ShortWaveRadiationSum, Date" }
    };

    public static readonly Dictionary<string, (double, double)> CityMapping = new()
    {
        { "Budapest, Hungary", (47.4925, 19.051389) },
        { "Bangkok, Thailand", (13.7525, 100.494167) },
        { "Zurich, Switzerland", (47.374444,8.541111) },
        { "Los Angeles, USA", (34.05, -118.25) },
        { "Halifax, Canada", (44.6475,-63.590556) },
        { "Rome, Italy", (41.893333, 12.482778) },
		{ "London, UK", (51.507222, -0.1275) },
		{ "Riga, Latvia", (56.948889, 24.106389) },
		{ "Barcelona, Spain", (41.383333, 2.183333) },
		{ "Shanghai, China", (31.228611, 121.474722) },
		{ "Tokyo, Japan", (35.683333,139.766667) },
		{ "Paris, France", (48.856667,2.352222) },
		{ "Stockholm, Sweden", (59.329444,18.068611) },
		{ "Munich, Germany", (48.1375,11.575) },
		{ "Riyadh, Saudi Arabia", (24.633333,46.716667) }
    };
	

}