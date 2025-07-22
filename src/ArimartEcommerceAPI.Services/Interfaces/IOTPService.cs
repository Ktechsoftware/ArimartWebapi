using System.Threading.Tasks;
using ArimartEcommerceAPI.Services.Services;

public interface IOTPService
{
    Task<OtpSendResult?> SendOTPAsync(string mobileNumber);
    Task<bool> VerifyOTPAsync(string mobileNumber, string otp);
}