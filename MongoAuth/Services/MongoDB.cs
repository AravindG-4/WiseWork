using MongoDB.Driver;
using MongoAuth.Shared.Models;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using System.Threading.Tasks;
using BCrypt.Net;
using Newtonsoft.Json.Linq;

namespace MongoAuth.Services
{
    public class MongoDBServices
    {
        private readonly IMongoCollection<ToDo> _todoCollection;
        public readonly IMongoCollection<User> _usersCollection;
        public readonly IMongoCollection<Favourites> _favCollection;

        public MongoDBServices(IConfiguration configuration)
        {
            var MongoUrl = configuration["MongoDB:URL"];
            var DatabaseName = configuration["MongoDB:DBNAME"];
            var ToDoCollectionName = configuration["MongoDB:TODO:COLLECTION"];
            var UserCollectionName = configuration["MongoDB:USERS:COLLECTION"];
            var FavCollectionName = configuration["MongoDB:FAVOURITES:COLLECTION"];

            Console.WriteLine("Service Constructor");
            var client = new MongoClient(MongoUrl);
            Console.WriteLine("Connection string works");
            var database = client.GetDatabase(DatabaseName);
            Console.WriteLine("Database get");
            _todoCollection = database.GetCollection<ToDo>(ToDoCollectionName);
            _usersCollection = database.GetCollection<User>(UserCollectionName);
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
        public async Task<User> GetUserByEmail(string email)
        {
            return await _usersCollection.Find(user => user.Email == email).FirstOrDefaultAsync();
        }

        public async Task<User> GetUserById(string userid)
        {
            return await _usersCollection.Find(user => user.Id == userid).FirstOrDefaultAsync();
        }

        //public async Task<User> GetUserByToken(string token)
        //{
        //    return await _usersCollection.Find(user => user.JwtToken == token).FirstOrDefaultAsync();
        //}

        public async Task<User> RegisterUser(User user)
        {
            // Check if user already exists
            var existingUser = await _usersCollection.Find(u => u.Email == user.Email || u.Username == user.Username).FirstOrDefaultAsync();
            if (existingUser != null)
                throw new Exception("User with this email or username already exists");

            // Hash password
            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);

            // Set default role if not specified
            if (string.IsNullOrEmpty(user.Role))
                user.Role = "user";

            await _usersCollection.InsertOneAsync(user);
            return user;
        }

        //public async Task UpdateUserToken(User user, string token)
        //{
        //    var id = user.Id;
        //    var filter = Builders<User>.Filter.Eq(User => User.Id, id);
        //    var update = Builders<User>.Update.Set(User => User.JwtToken, token);

        //    await _usersCollection.UpdateOneAsync(filter, update);
        //}

        //public async Task RemoveUserToken(string userid)
        //{
        //    var filter = Builders<User>.Filter.Eq(User => User.Id, userid);
        //    var update = Builders<User>.Update.Set(User => User.JwtToken, null);

        //    await _usersCollection.UpdateOneAsync(filter, update);
        //}


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
