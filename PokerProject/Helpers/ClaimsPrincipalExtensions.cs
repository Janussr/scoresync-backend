using System.Security.Claims;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal user)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        var claim = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        if (claim == null)
            throw new Exception("User ID claim not found");

        return int.Parse(claim.Value);
    }

    public static string GetUserRole(this ClaimsPrincipal user)
    {
        return user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value ?? "";
    }
}