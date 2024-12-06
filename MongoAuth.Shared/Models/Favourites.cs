using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;
using MongoAuth.Shared.Models;

namespace MongoAuth.Shared.Models
{
    public class Favourites
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        [Required]
        [BsonElement("userID")]
        public string userId { get; set; }

        [Required]
        [BsonElement("favCity")]
        public Dictionary<string,object> favCity { get; set; }

        [Required]
        [BsonElement("favWeather")]
        public string? favWeather { get; set; } = null;     //for now
    }
}