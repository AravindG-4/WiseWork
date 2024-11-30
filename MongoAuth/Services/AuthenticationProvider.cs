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


namespace MongoAuth.Services
{
    class AuthenticationProvider : AuthenticationStateProvider
    {
        public User User { get; private set; } = new();

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly JwtTokenService _tokenService;
        private readonly MongoDBServices _mongoDBServices;
        private readonly IConfiguration _configuration;

        private AuthenticationState _cachedAuthState;

        public AuthenticationProvider(
        IHttpContextAccessor httpContextAccessor,
        JwtTokenService tokenService,
        MongoDBServices mongoDBServices,
        IConfiguration configuration)
        {
            _httpContextAccessor = httpContextAccessor;
            _tokenService = tokenService;
            _mongoDBServices = mongoDBServices;
            _configuration = configuration;
        }

        // This sets the Authentication State with User Roles
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            if (_cachedAuthState != null)
                return _cachedAuthState;

            var cookies = _httpContextAccessor.HttpContext.Request.Cookies;
            var token = cookies.ContainsKey("auth_token") ? cookies["auth_token"] : null;
            Console.WriteLine("Got Token : " + token);
            if (string.IsNullOrEmpty(token))
            {
                _cachedAuthState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                return _cachedAuthState;
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
                jwtToken = tokenHandler.ReadJwtToken(token);
                Console.WriteLine("Token : " + jwtToken);
                var user = await _mongoDBServices.GetUserByToken(token);
                if (user != null)
                {
                    User = user;
                    _cachedAuthState = new AuthenticationState(principal);
                    return _cachedAuthState;
                }
            }
            catch
            {
                Console.WriteLine("Token Invalid");
            }
                _cachedAuthState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                return _cachedAuthState;
        }

        public void SetUser(User? user)
        {
            Console.WriteLine("Hitting SetUser");
            if (user == null)
            {
                //User.Username = null;
                User = new User();
                _cachedAuthState = null;
            }
            else
            {
                User = user;
                _cachedAuthState = null;
            }
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
    }
}