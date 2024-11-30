using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.JSInterop;
using MongoAuth.Shared.Models;

namespace MongoAuth.Services
{

    public class JwtTokenService
    {
        private readonly IConfiguration _configuration;
        private readonly MongoDBServices _mongoDBServices;
        private readonly IJSRuntime _jsRuntime;

        public JwtTokenService(IConfiguration configuration, MongoDBServices mongoDBServices, IJSRuntime jsRuntime)
        {
            _configuration = configuration;
            _mongoDBServices = mongoDBServices;
            _jsRuntime = jsRuntime;
        }

        public string CreateSessionToken(User user)
        {
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not found"));
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            }),
                Expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["Jwt:ExpirationMinutes"])),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            //Console.WriteLine("Token Created: " + token);
            return tokenHandler.WriteToken(token);
        }

        public async Task SetAuthCookie(string token)
        {
            await _jsRuntime.InvokeVoidAsync("CookieWriter.Write", "authToken", token, DateTime.Now.AddHours(1).ToString("R"));
        }

        public async Task<string> GetAuthCookie()
        {
            return await _jsRuntime.InvokeAsync<string>("CookieReader.Read", "authToken");
        }

        public async Task RemoveAuthCookie()
        {
            await _jsRuntime.InvokeVoidAsync("CookieRemover.Delete", "authToken");
        }
    }
}