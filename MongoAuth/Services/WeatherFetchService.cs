using Microsoft.AspNetCore.Components;
using MongoAuth.Shared.Models;
using static System.Net.WebRequestMethods;


namespace MongoAuth.Services
{
    public class WeatherFetchService
    {
        string apiKey;
        private readonly HttpClient _httpClient;
        UserFavService UserFavService;
        public List<WeatherForecast> forecasts = new List<WeatherForecast>();

        public WeatherFetchService(IConfiguration configuration, HttpClient httpClient, UserFavService userFavService) 
        {
            apiKey = configuration["OpenWeather:API_KEY"];
            _httpClient = httpClient;
            UserFavService = userFavService;
        }

        public async Task<List<WeatherForecast>?> GetWeatherData(string userId, string SearchCity)
        {
            if (!string.IsNullOrWhiteSpace(SearchCity))
            {
                Console.WriteLine($"Fetching weather data for {SearchCity}...");

                await UserFavService.InsertNewCityAsync(userId, SearchCity);
                //favCity = UserFavService.GetFavCity();

                var forecastList = await FetchWeatherForecastAsync(SearchCity);

                if (forecastList != null)
                {
                    Console.WriteLine($"Received weather data for {SearchCity}");

                    // Add new forecast list
                    forecasts = forecastList;
                    return forecasts;
                }
                else
                {
                    Console.WriteLine($"No weather data received for {SearchCity}");
                    return null;
                }
            }
            else
            {
                Console.WriteLine("City name is empty or invalid.");
                return null;
            }
        }

        private async Task<List<WeatherForecast>> FetchWeatherForecastAsync(string cityName)
        {
            try
            {
                //string apiKey = Configuration["OpenWeather:API_KEY"];
                var url = $"https://api.openweathermap.org/data/2.5/forecast?q={cityName}&appid={apiKey}&units=metric";
                Console.WriteLine($"Sending request to: {url}");

                var response = await _httpClient.GetFromJsonAsync<OpenWeatherResponse>(url);

                if (response != null && response.List.Any())
                {
                    return response.List.Select(item =>
                    {
                        var dateTime = DateTimeOffset.FromUnixTimeSeconds(item.Dt).DateTime;

                        return new WeatherForecast
                        {
                            Date = dateTime.Date,
                            Time = dateTime.ToString("HH:mm"),
                            TemperatureC = (int)item.Main.Temp,
                            Summary = item.Weather.FirstOrDefault()?.Description ?? "No description"
                        };
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching weather data: {ex.Message}");
            }

            return null;
        }

        public async Task<MarkupString?> NextFavAlert(string userId, string favCity, string favWeather)
        {
            Console.WriteLine("From NextFavAlert");
            var fcasts = await GetWeatherData(userId, favCity);
            Console.WriteLine($"From NextFavAlert favCasts: {fcasts}");
            if (fcasts != null)
            {
                foreach (var groupedForecast in fcasts.GroupBy(f => f.Date))
                {
                    foreach (var forecast in groupedForecast)
                    {
                        if (forecast.Summary == favWeather.ToLower())
                        {
                            Console.WriteLine($"Returning Weather Matched");
                            return new MarkupString($"The weather in {favCity} will be {favWeather} on <b>{forecast.Date.ToShortDateString()}</b> at {forecast.Time}");
                        }
                    }
                }
                return null;
            }
            return null;
        }

        public class WeatherForecast
        {
            public DateTime Date { get; set; }
            public string? Time { get; set; }
            public int TemperatureC { get; set; }
            public string? Summary { get; set; }
        }

        private class OpenWeatherResponse
        {
            public List<WeatherItem> List { get; set; } = new List<WeatherItem>();
        }

        private class WeatherItem
        {
            public long Dt { get; set; } // Unix timestamp
            public Main Main { get; set; } = new Main();
            public List<WeatherDescription> Weather { get; set; } = new List<WeatherDescription>();
        }

        private class Main
        {
            public float Temp { get; set; }
        }

        private class WeatherDescription
        {
            public string Description { get; set; } = string.Empty;
        }
    }

}


