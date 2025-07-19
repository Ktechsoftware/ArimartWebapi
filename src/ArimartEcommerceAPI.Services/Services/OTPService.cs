using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ArimartEcommerceAPI.Infrastructure.Data;

namespace ArimartEcommerceAPI.Services.Services
{
    public class OTPService : IOTPService
    {
        private readonly ApplicationDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OTPService> _logger;
        private const int OTP_EXPIRY_MINUTES = 10; // OTP expires in 10 minutes

        public OTPService(
            ApplicationDbContext context,
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<OTPService> logger)
        {
            _context = context;
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        private static string GenerateOTP()
        {
            Random rnd = new Random();
            return rnd.Next(100000, 999999).ToString(); // 6-digit OTP for better security
        }

        public async Task<string?> SendOTPAsync(string mobileNumber)
        {
            try
            {
                // Validate mobile number
                if (string.IsNullOrWhiteSpace(mobileNumber) || mobileNumber.Length != 10)
                {
                    _logger.LogWarning("Invalid mobile number: {MobileNumber}", mobileNumber);
                    return null;
                }

                string otp = GenerateOTP();
                string message = $"Arimart: Your OTP for verification is: {otp}. Valid for {OTP_EXPIRY_MINUTES} minutes.";

                // Get API key from configuration
                //string? apiKey = _configuration["Fast2SMS:ApiKey"];
                string? apiKey = "WhH3ity1CkzLa9Kcw86YRQorXjflebDJuNx7gOUSBMEsZq54AIuAYqd6ExS1czQylIsoOCrZtR5jHLpU";
                if (string.IsNullOrEmpty(apiKey))
                {
                    throw new InvalidOperationException("Fast2SMS API key not configured.");
                }


                string url = $"https://www.fast2sms.com/dev/bulkV2?authorization={apiKey}&message={Uri.EscapeDataString(message)}&language=english&route=q&numbers={mobileNumber}";

                using HttpResponseMessage response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("SMS sent successfully to {MobileNumber}. Response: {Response}",
                        mobileNumber, responseContent);

                    // Save OTP to database
                    await SaveOTPToDatabase(mobileNumber, otp);

                    return otp; // In production, you might not want to return the actual OTP
                }
                else
                {
                    _logger.LogError("Failed to send SMS. Status: {StatusCode}, Response: {Response}",
                        response.StatusCode, await response.Content.ReadAsStringAsync());
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending OTP to {MobileNumber}", mobileNumber);
                return null;
            }
        }

        public async Task<bool> VerifyOTPAsync(string mobileNumber, string otp)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(mobileNumber) || string.IsNullOrWhiteSpace(otp))
                {
                    return false;
                }

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserContact == mobileNumber);

                if (user == null || string.IsNullOrEmpty(user.LastOtp))
                {
                    _logger.LogWarning("User not found or no OTP generated for mobile: {MobileNumber}", mobileNumber);
                    return false;
                }

                // Check if OTP matches
                if (user.LastOtp != otp)
                {
                    _logger.LogWarning("Invalid OTP for mobile: {MobileNumber}", mobileNumber);
                    return false;
                }

                if (user.LastOtp?.Trim() != otp.Trim())
                {
                    _logger.LogWarning("OTP mismatch. Entered: {Entered}, Stored: {Stored}", otp, user.LastOtp);
                    return false;
                }


                if (user.OtpTime.HasValue)
                {
                    double minutes = DateTime.Now.Subtract(user.OtpTime.Value).TotalMinutes;
                    if (minutes > OTP_EXPIRY_MINUTES)
                    {
                        _logger.LogWarning("OTP expired. Elapsed minutes: {Minutes}", minutes);
                        return false;
                    }
                }
                else
                {
                    _logger.LogError("OTP time missing for user {MobileNumber}", mobileNumber);
                    return false;
                }


                // Clear OTP after successful verification
                user.LastOtp = null;
                user.OtpTime = null;
                await _context.SaveChangesAsync();

                _logger.LogInformation("OTP verified successfully for mobile: {MobileNumber}", mobileNumber);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying OTP for {MobileNumber}", mobileNumber);
                return false;
            }
        }

        private async Task SaveOTPToDatabase(string mobileNumber, string otp)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserContact == mobileNumber);

                if (user != null)
                {
                    user.LastOtp = otp;
                    user.OtpTime = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
                else
                {
                    _logger.LogWarning("User not found for mobile number: {MobileNumber}", mobileNumber);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving OTP to database for {MobileNumber}", mobileNumber);
                throw;
            }
        }
    }
}