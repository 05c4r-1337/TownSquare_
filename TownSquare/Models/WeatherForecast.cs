namespace TownSquare.Models;

public class WeatherForecast
{
    public DateTime Date { get; set; }
    public double Temperature { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public double Humidity { get; set; }
    public double WindSpeed { get; set; }
}
