namespace WeatherApp.Models
{
    public class TemperatureForDb
    {
        public DateTime? creationTime { get; set; }
        public int temperature { get; set; }
        public string? pointInfo { get; set; }
    }
}
