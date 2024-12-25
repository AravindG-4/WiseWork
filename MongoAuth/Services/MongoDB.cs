using MongoDB.Driver;
using MongoAuth.Shared.Models;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using System.Threading.Tasks;
using BCrypt.Net;
using Newtonsoft.Json.Linq;
using Supabase;
using Microsoft.AspNetCore.Components.Authorization;
using static Supabase.Postgrest.Constants;
using Supabase.Gotrue;
namespace MongoAuth.Services
{
    public class MongoDBServices
    {
        private readonly IMongoCollection<ToDo> _todoCollection;
        public readonly IMongoCollection<Favourites> _favCollection;
        public readonly Supabase.Client _supabaseClient;
        public readonly IHttpContextAccessor _httpContextAccessor;

        public MongoDBServices(IConfiguration configuration, Supabase.Client supabaseClient, IHttpContextAccessor httpContextAccessor)
        {
            var MongoUrl = configuration["MongoDB:URL"];
            var DatabaseName = configuration["MongoDB:DBNAME"];
            var ToDoCollectionName = configuration["MongoDB:TODO:COLLECTION"];
            var FavCollectionName = configuration["MongoDB:FAVOURITES:COLLECTION"];

            _supabaseClient = supabaseClient;
            _httpContextAccessor = httpContextAccessor;
            //Auth = auth;

            Console.WriteLine("Service Constructor");
            var client = new MongoClient(MongoUrl);
            Console.WriteLine("Connection string works");
            var database = client.GetDatabase(DatabaseName);
            Console.WriteLine("Database get");
            _todoCollection = database.GetCollection<ToDo>(ToDoCollectionName);
            _favCollection = database.GetCollection<Favourites>(FavCollectionName);
            Console.WriteLine("Collection get");
        }

        //ToDo Operations and Services
        public async Task<List<ToDo>> ReadPending()
        {
            var Tasks = await _todoCollection.Find(task => task.Completed == false).ToListAsync();
            return Tasks;
        }

        public async Task<List<ToDo>> ReadCompleted()
        {
            var Tasks = await _todoCollection.Find(task => task.Completed == true).ToListAsync();
            return Tasks;
        }

        public async Task CreateToDo(ToDo task)
        {
            await _todoCollection.InsertOneAsync(task);
        }

        public async Task CompleteToDo(ToDo task)
        {
            var id = task.Id;
            var filter = Builders<ToDo>.Filter.Eq(task => task.Id, id);
            var update = Builders<ToDo>.Update.Set(task => task.Completed, true);

            await _todoCollection.UpdateOneAsync(filter, update);
        }

        public async Task RemoveToDo(ToDo task)
        {
            var id = task.Id;
            var filter = Builders<ToDo>.Filter.Eq(task => task.Id, id);
            await _todoCollection.DeleteOneAsync(filter);
        }


        //User Operations and Services
        public async Task<Favourites?> GetUserFavourites(string userId)
        {
            var favourite = await _favCollection.Find(f => f.userId == userId).FirstOrDefaultAsync();
            if (favourite == null)
            {
                Console.WriteLine("Null");
                return null;
            }
            else
            {
                Console.WriteLine("Favourites Cities from DB: " + favourite.favCity.GetType());
                return favourite;
            }
        }

        public async Task UpdateFavouriteCities(string userId, Dictionary<string, object> favCity)
        {
            var filter = Builders<Favourites>.Filter.Eq(User => User.userId, userId);
            var update = Builders<Favourites>.Update.Set(User => User.favCity, favCity);

            await _favCollection.UpdateOneAsync(filter, update);
        }

        public async Task CreateFavouriteCities(string userId)
        {
            var document = new Favourites
            {
                userId = userId,
                favCity = new Dictionary<string, object>()
            };
            await _favCollection.InsertOneAsync(document);
        }

        public async Task UpdateFavouriteWeather(string userId, string? favWeather)
        {
            var filter = Builders<Favourites>.Filter.Eq(User => User.userId, userId);
            var update = Builders<Favourites>.Update.Set(User => User.favWeather, favWeather);

            await _favCollection.UpdateOneAsync(filter, update);
        }
    }
}
