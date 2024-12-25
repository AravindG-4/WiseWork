using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using BCrypt.Net;
//using Isopoh.Cryptography.Argon2;
using MongoAuth.Shared.Models;
using MongoAuth.Services;
using static Supabase.Postgrest.Constants;
using Supabase.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MongoAuth.Components;

public class UserContext : ComponentBase
{
    [Inject] AuthenticationStateProvider Asp { get; set; } = default!;

    [Inject] IJSRuntime _jsRuntime { get; set; } = default!;

    [Inject] MongoDBServices Database { get; set; } = default!;

    [Inject] SupabaseService supabaseService { get; set; } = default!;

    [Inject] IConfiguration Configuration { get; set; } = default!;

    [Inject] NavigationManager Nav { get; set; } = default!;

    private AuthenticationProvider Auth { get; set; } = default!;

    public User User => Auth.User;

    protected override async void OnInitialized()
    {
        Console.WriteLine("From UserContext");
        Auth = (AuthenticationProvider)Asp;
        //await UserReAuthorize();
    }

    // Cookie Writer
    public async Task WriteCookie(string cookieName, string cookieValue, int durationMinutes = 1)
    {
        await _jsRuntime.InvokeVoidAsync("CookieWriter.Write", cookieName, cookieValue, DateTime.Now.AddMinutes(durationMinutes));
    }

    // Cookie Reader
    public async Task<string> ReadCookie(string cookieName)
    {
        return await _jsRuntime.InvokeAsync<string>("CookieReader.Read", cookieName);
    }

    // Cookie Remover
    public async Task DeleteCookie(string cookieName)
    {
        await _jsRuntime.InvokeVoidAsync("CookieRemover.Delete", cookieName);
    }

    public async Task<string> ModifyTokenRole(string token, string role)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var claims = jwtToken.Claims.ToList();
        var newClaims = claims.Where(c => c.Type != "role").ToList(); // Remove the existing role claim
        newClaims.Add(new Claim("role", role)); // Add the new role claim

        var key = Encoding.UTF8.GetBytes(Configuration["Jwt:Key"]);
        var signingKey = new SymmetricSecurityKey(key);
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var updatedToken = new JwtSecurityToken(
            issuer: Configuration["Jwt:Issuer"],
            //audience: "authenticated",
            claims: newClaims,
            notBefore: jwtToken.ValidFrom,
            expires: jwtToken.ValidTo,
            signingCredentials: credentials
        );

        var newToken = handler.WriteToken(updatedToken);

        Console.WriteLine("Updated Token Created: " + newToken);
        return newToken;
    }

    public async Task<bool> LoginAsync(string email, string password)

    {
        // Check if the input is empty
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password)) { return false; }

        var user = await supabaseService.GetUserByEmail(email);

        // check if the user is Valid
        if (user == null) { return false; }

        var token = await supabaseService.LoginAsync(email, password);
        if (token == null) { return false; }
        Console.WriteLine("Token Created: " + token);
        var updatedToken = await ModifyTokenRole(token, user.role ?? "user");
        await WriteCookie("auth_token", updatedToken, 60);
        Auth.SetUser(user);
        Console.WriteLine("user set");
        return true;
    }

    public async Task Logout()
    {
        Console.WriteLine("Logging out");
        await DeleteCookie("auth_token");
        await supabaseService.LogoutAsync();
        //await Task.Delay(1000);
        Console.WriteLine("Logged out");
        Auth.SetUser(null);
        //await Task.Delay(1000);
        Nav.NavigateTo("/login", forceLoad: true);
    }

    public void NavTo(string path, bool refresh = true)
    {
        Nav.NavigateTo(path, refresh);
    }

}