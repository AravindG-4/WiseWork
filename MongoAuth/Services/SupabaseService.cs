using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using MongoAuth.Components;
using MongoAuth.Shared.Models;
using Supabase.Interfaces;
using static Supabase.Postgrest.Constants;

namespace MongoAuth.Services
{
    public class SupabaseService
    {
        Supabase.Client _supabaseClient;
        IHttpContextAccessor _httpContextAccessor;
        UserContext _userContext;
        //AuthenticationStateProvider Auth;
        public SupabaseService(Supabase.Client supabase, IHttpContextAccessor httpContextAccessor, UserContext userContext)
        { 
            _supabaseClient = supabase;
            _httpContextAccessor = httpContextAccessor;
            _userContext = userContext;
            //Auth = auth;
        }
        public async Task<bool> RegisterUser(string email, string password, string username)
        {
            Console.WriteLine("Hitting Register User");
            var response = await _supabaseClient.Auth.SignUp(email, password);
            Console.WriteLine("Register User Response : " + response?.User);

            if (response?.User != null)
            {
                Console.WriteLine("If Success");
                // After successful registration, add additional user information to the DB
                var temp_id = Guid.Parse(response.User.Id);
                if (temp_id == null)
                {
                    Console.WriteLine("ID is null");
                }
                Console.WriteLine("ID is not null");
                var newUser = new MongoAuth.Shared.Models.User
                {
                    id = temp_id, 
                    email = email,
                    name = username,
                    role = "user",
                    created_at = DateTime.UtcNow 

                };

                Console.WriteLine("Inserting User: " + newUser);
                Console.WriteLine("Inserting User: " + newUser.id);
                Console.WriteLine("Inserting User: " + newUser.name);
                Console.WriteLine("Inserting User: " + newUser.email);
                Console.WriteLine("Inserting User: " + newUser.role);
                Console.WriteLine("Inserting User: " + newUser.created_at);

                var insertResponse = await _supabaseClient.From<MongoAuth.Shared.Models.User>().Insert(newUser);

                if (insertResponse != null)
                {
                    Console.WriteLine("User Inserted Successfully");
                    return true;
                }
                else
                {
                    Console.WriteLine("Failed to Insert User");
                    return false;
                }
            }

            return false; 

        }

        public async Task<string?> LoginAsync(string email, string password)
        {
            Console.WriteLine("Hitting LoginAsync");
            try
            {
                var response = await _supabaseClient.Auth.SignIn(email, password);

                if (response != null)
                {
                    var accesstoken = response.AccessToken;
                    var refreshtoken = response.RefreshToken;

                    Console.WriteLine("Access Token: " + accesstoken);
                    Console.WriteLine("Refresh Token: " + refreshtoken);

                    //await Task.Delay(1000);

                    var Response = await _supabaseClient.From<MongoAuth.Shared.Models.User>()
                    .Filter("email", Operator.Equals, email)
                    .Get();

                    // Check if any user was found
                    if (Response.Models != null && Response.Models.Any())
                    {
                        var user = Response.Models.First();
                        Console.WriteLine($"User found: {user.name} with email {user.email}");
                        //((AuthenticationProvider)Auth).SetUser(user);
                    }
                    return accesstoken;
                }
                return null;
            }
            catch (Exception ex) {
                Console.WriteLine("Error: " + ex.Message);
                return null;
            }
            }      

        public async Task LogoutAsync()
        {
            Console.WriteLine("Logging out user and clearing session...");
            await _supabaseClient.Auth.SignOut();
        }

        public async Task<User?> GetUserByEmail(string email)
        {
            try
            {
                var response = await _supabaseClient.From<User>()
                    .Filter("email", Operator.Equals, email)
                    .Get();

                if (response.Models != null && response.Models.Any())
                {
                    return response.Models.First();
                }
                else
                {
                    Console.WriteLine($"No user found with email: {email}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching user by email: {ex.Message}");
            }

            return null;
        }

    }
}
