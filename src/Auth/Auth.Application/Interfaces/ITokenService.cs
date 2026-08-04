using Auth.Domain;

namespace Auth.Application.Interfaces
{
    public interface ITokenService
    {
        string GenerateAccessToken(Account account);
        string GenerateRefreshToken();
    }
}