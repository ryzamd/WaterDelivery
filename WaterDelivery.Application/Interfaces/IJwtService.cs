using System.Security.Claims;

namespace WaterDelivery.Application.Interfaces
{
    public interface IJwtService
    {
        Task<string> GenerateAccessTokenAsync(Guid userId, string email, string role = "User");
        Task<string> GenerateRefreshTokenAsync();
        Task<ClaimsPrincipal?> ValidateTokenAsync(string token);
        Task<bool> RevokeTokenAsync(string jti);
        string GetTokenId(string token);
    }
}