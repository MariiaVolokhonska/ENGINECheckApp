namespace WeatherApp.Models
{
    public class TempratureData
    {
        public DateTime? CreationTime { get; set; }
        public double Temperature { get; set; }
        public double Humidity { get; set; }
        public string? Info { get; set; }
    }
}
