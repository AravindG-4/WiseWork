using MongoAuth.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace MongoAuth.Services;

public class CustomAuthorizeAttribute : AuthorizeAttribute
{
    private readonly string[] _roles;

    public CustomAuthorizeAttribute(params string[] roles)
    {
        _roles = roles;
        Roles = string.Join(",", roles);
    }

    public bool IsAuthorized(ClaimsPrincipal user)
    {
        if (!user.Identity.IsAuthenticated)
            return false;

        if (_roles == null || _roles.Length == 0)
            return true;

        return _roles.Any(role => user.IsInRole(role));
    }
}
