using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;

namespace MongoAuth.Client.Services
{
    public class JwtAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private AuthenticationState _cachedAuthState;

        public JwtAuthenticationStateProvider(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            if (_cachedAuthState != null)
                return Task.FromResult(_cachedAuthState);

            var token = _httpContextAccessor.HttpContext?.Request.Cookies["auth_token"];

            if (string.IsNullOrEmpty(token))
            {
                _cachedAuthState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                return Task.FromResult(_cachedAuthState);
            }

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                var claims = jwtToken.Claims.ToList();
                claims.Add(new Claim(ClaimTypes.Name, jwtToken.Claims.FirstOrDefault(c => c.Type == "unique_name")?.Value ?? ""));

                var identity = new ClaimsIdentity(claims, "jwt");
                var principal = new ClaimsPrincipal(identity);

                _cachedAuthState = new AuthenticationState(principal);
                return Task.FromResult(_cachedAuthState);
            }
            catch
            {
                _cachedAuthState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                return Task.FromResult(_cachedAuthState);
            }
        }

        public void NotifyAuthenticationStateChanged()
        {
            _cachedAuthState = null;
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
    }
}