using MongoDB.Driver;
using MongoAuth.Shared.Models;

namespace MongoAuth.Services
{
    public class UserFavService
    {
        private readonly MongoDBServices _mongoDBServices;

        public UserFavService(MongoDBServices mongoDBServices)
        {
            _mongoDBServices = mongoDBServices;
        }

        private string UserID;
        public List<string> FavCities { get; private set; } = new();
        private Dictionary<string, int> UserFavDetails = new();
        public int LeastFavCount { get; private set; }
        public int MostFavCount { get; private set; }
        public string FavWeather { get; private set; }

        // Fetch user's favorite cities and initialize the list and counts
        public async Task GetFavouritesAsync(string? userId)
        {
            var favourite = await _mongoDBServices.GetUserFavourites(userId);
            UserID = userId;
            if (favourite == null)
            {
                Console.WriteLine("Creating new favourite cities list");
                await _mongoDBServices.CreateFavouriteCities(userId);
                favourite = await _mongoDBServices.GetUserFavourites(userId);
            }

            if (favourite.favCity.Any())
            {
                UserFavDetails = favourite.favCity.ToDictionary(
                    entry => entry.Key,
                    entry => Convert.ToInt32(entry.Value)
                );

                FavCities = UserFavDetails.Keys.ToList();
                LeastFavCount = UserFavDetails.Values.Min();
                MostFavCount = UserFavDetails.Values.Max();
                FavWeather = favourite.favWeather;
                Console.WriteLine("Favourite cities list initialized");
                foreach (var city in FavCities)
                {
                    Console.WriteLine($"Favourite City : {city}");
                }
            }
            else
            {
                Console.WriteLine("No favourite cities found");
            }
            Console.WriteLine($"Favourite Weather : {FavWeather}");
        }

        // Insert or update a city in the favorites dictionary
        public async Task InsertNewCityAsync(string userId, string city)
        {
            //if (!UserFavDetails.Any())
            //{
            //    await GetFavouritesAsync(userId);
            //}

            if (UserFavDetails.ContainsKey(city))
            {
                UserFavDetails[city]++;
            }
            else
            {
                // Find the city with the least search count and replace it
                if (UserFavDetails.Count < 5)
                {
                    UserFavDetails[city] = 1;
                }
                else
                {
                    var leastSearchedCities = UserFavDetails
                        .Where(x => x.Value == LeastFavCount)
                        .Select(x => x.Key)
                        .ToList();

                    // Randomly select one of the least searched cities
                    var cityToReplace = leastSearchedCities[new Random().Next(leastSearchedCities.Count)];

                    UserFavDetails.Remove(cityToReplace);
                    UserFavDetails[city] = 1;
                }
            }

            // Update the database with the modified dictionary
            var favCity = UserFavDetails.ToDictionary(
                    entry => entry.Key,
                    entry => (object)entry.Value
                );

            await _mongoDBServices.UpdateFavouriteCities(userId, favCity);

            // Update local variables
            FavCities = UserFavDetails.Keys.ToList();
            LeastFavCount = UserFavDetails.Values.Min();
            MostFavCount = UserFavDetails.Values.Max();
            Console.WriteLine("City Added");
            foreach (var City in FavCities)
            {
                Console.WriteLine($"Favourite City : {City}");
            }
        }

        public string? GetFavCity()
        {
            if (!UserFavDetails.Any())
                return null;

            var favCity = UserFavDetails.FirstOrDefault(x => x.Value == MostFavCount).Key ?? null;
            if (favCity != null)
            {
                return favCity;
            }
            else
            {
                return null;
            }
        }

        public string? GetFavWeather()
        {
            return FavWeather;
        }
        
        public async Task UpdateFavWeather(string? favWeather)
        {
            await _mongoDBServices.UpdateFavouriteWeather(UserID, favWeather);
        }
    }
}
