using ArimartEcommerceAPI.Infrastructure.Data.Models;

public interface ITokenService
{
    string CreateToken(TblUser user);
    string CreateToken(TblDeliveryuser deliveryuser);
}
