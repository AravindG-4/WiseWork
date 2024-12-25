using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using Microsoft.JSInterop;
using BCrypt.Net;
using MongoAuth.Shared.Models;
using MongoAuth.Services;
using System.IdentityModel.Tokens.Jwt;
using MongoAuth.Components;
using System.ComponentModel.Design;
using Microsoft.AspNetCore.Components;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Configuration;
using static Supabase.Postgrest.Constants;


namespace MongoAuth.Services
{
    class AuthenticationProvider : AuthenticationStateProvider
    {
        public User? User { get; private set; } = new();

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly SupabaseService _supabaseService;

        private AuthenticationState? _cachedAuthState = null;

        public AuthenticationProvider(
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        SupabaseService supabaseService)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _supabaseService= supabaseService;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            //await Task.Delay(1000);

            if (_cachedAuthState != null)
            {
                Console.WriteLine($"Using Cached Auth State : {_cachedAuthState.User.Identity?.Name}");
                return _cachedAuthState;
            }

            var authState = await FetchAuthState();
            _cachedAuthState = authState;

            Console.WriteLine($"Fetched New Auth State : {authState.User.Identity?.Name}");
            return authState;
        }

        public async Task SetUser(User? user)
        {
            Console.WriteLine("Hitting SetUser");
            Console.WriteLine("UserName: " + user?.name);
            if (user == null)
            {
                User = new User();
            }
            else
            {
                User = user;
            }
            _cachedAuthState = null;
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
        public async Task<AuthenticationState> FetchAuthState()
        {
            var cookies = _httpContextAccessor.HttpContext.Request.Cookies;
            var token = cookies.ContainsKey("auth_token") ? cookies["auth_token"] : null;

            if (string.IsNullOrEmpty(token))
            {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            // Decode the JWT token and check validity.
            var tokenHandler = new JwtSecurityTokenHandler();
            JwtSecurityToken jwtToken;
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);

            try
            {
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
                var email = principal.FindFirst(ClaimTypes.Email)?.Value;
                Console.WriteLine("Email extracted from JWT: " + email);
                jwtToken = tokenHandler.ReadJwtToken(token);

                var user = await _supabaseService.GetUserByEmail(email);
                if (user != null)
                {
                    User = user;

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.name),
                        new Claim(ClaimTypes.Email, user.email),
                        new Claim(ClaimTypes.Role, user.role ?? "user")
                    };

                    // Create a ClaimsIdentity and ClaimsPrincipal from the claims
                    var identity = new ClaimsIdentity(claims, "jwt");
                    var newPrincipal = new ClaimsPrincipal(identity);

                    return new AuthenticationState(newPrincipal);
                }
            }
            catch
            {
                Console.WriteLine("Token Invalid");
            }

            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }
}