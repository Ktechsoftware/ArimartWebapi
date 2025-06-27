using ArimartEcommerceAPI.Infrastructure.Data.Models;

public interface ITokenService
{
    string CreateToken(User user);
}
