using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.ComponentModel.DataAnnotations;

namespace MongoAuth.Shared.Models
{
    [Table("Users")]
    public class User : BaseModel
    {
        [PrimaryKey("id",true)]
        [Required]
        public Guid id { get; set; } // Matches int8 in Supabase

        [Column("name")]
        public string name { get; set; } = string.Empty; // User's username

        [Column("email")]
        public string email { get; set; } = string.Empty; // User's email

        [Column("role")]
        public string role { get; set; } = "user"; // User's role

        [Column("created_at")]
        public DateTime created_at { get; set; } = DateTime.UtcNow; // Creation timestamp

        //[Column("password")]
        //public string PasswordHash { get; set; } = string.Empty; // Hashed password for authent // Default role
    }
}