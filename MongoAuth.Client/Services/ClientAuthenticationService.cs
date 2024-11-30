using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Authorization;

namespace MongoAuth.Client.Services
{
    public class ClientAuthenticationService
    {
        private readonly HttpClient _httpClient;
        private readonly AuthenticationStateProvider _authStateProvider;

        public ClientAuthenticationService(HttpClient httpClient, AuthenticationStateProvider authStateProvider)
        {
            _httpClient = httpClient;
            _authStateProvider = authStateProvider;
        }

        public async Task<bool> ValidateAuthenticationStateAsync()
        {
            try
            {
                var authState = await _authStateProvider.GetAuthenticationStateAsync();
                if (!authState.User.Identity.IsAuthenticated)
                {
                    return false;
                }

                // Make a call to validate the token on the server
                var response = await _httpClient.GetAsync("api/auth/validate");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}