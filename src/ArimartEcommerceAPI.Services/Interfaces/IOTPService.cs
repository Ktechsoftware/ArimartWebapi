using System.Threading.Tasks;

public interface IOTPService
{
    Task<string?> SendOTPAsync(string mobileNumber);
    Task<bool> VerifyOTPAsync(string mobileNumber, string otp);
}