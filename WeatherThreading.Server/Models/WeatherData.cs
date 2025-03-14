using System.Collections;

namespace WeatherThreading.Server
{
    public class WeatherData
    {
        public int Id { get; set; }

        public required List<int> YAxis { get; set; } = new List<int>();

        public required List<int> XAxis { get; set; } = new List<int>();

        public required string YAxisLabel { get; set; }

        public required string XAxisLabel { get; set; }

        public string? Summary { get; set; }
    }

}
