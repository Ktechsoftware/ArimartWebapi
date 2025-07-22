using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ArimartEcommerceAPI.Infrastructure.Data;
using ArimartEcommerceAPI.Infrastructure.Data.Models;

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

        private string GenerateOTP()
        {
            // Check if in test mode and use fixed OTP
            bool isTestMode = _configuration.GetValue<bool>("OTP:TestMode", false);
            if (isTestMode)
            {
                return "123456"; // Fixed OTP for testing
            }

            // Production: Generate random OTP
            Random rnd = new Random();
            return rnd.Next(100000, 999999).ToString(); // 6-digit OTP
        }

        public async Task<OtpSendResult> SendOTPAsync(string mobileNumber)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(mobileNumber) || mobileNumber.Length != 10)
                {
                    string msg = "Invalid mobile number format.";
                    _logger.LogWarning(msg);
                    return new OtpSendResult { Success = false, ErrorMessage = msg };
                }

                string otp = GenerateOTP();
                bool isTestMode = _configuration.GetValue<bool>("OTP:TestMode", false);

                if (isTestMode)
                {
                    await SaveOTPToDatabase(mobileNumber, otp);
                    return new OtpSendResult { Success = true, Otp = otp };
                }

                string apiKey = _configuration.GetValue<String>("Fast2SMS:ApiKey", "");
                if (string.IsNullOrEmpty(apiKey))
                {
                    //string msg = "Fast2SMS API key not configured.";
                    apiKey = "WhH3ity1CkzLa9Kcw86YRQorXjflebDJuNx7gOUSBMEsZq54AIuAYqd6ExS1czQylIsoOCrZtR5jHLpU";
                }

                string message = $"Arimart: Your OTP for verification is: {otp}. Valid for {OTP_EXPIRY_MINUTES} minutes.";
                string url = "https://www.fast2sms.com/dev/bulkV2?authorization=" + apiKey + "&message=" + message + "&language=english&route=q&numbers=" + mobileNumber;

                using HttpResponseMessage response = await _httpClient.GetAsync(url);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("SMS failed. Status: {StatusCode}, Body: {Body}", response.StatusCode, responseBody);
                    return new OtpSendResult { Success = false, ErrorMessage = $"Fast2SMS error: {responseBody}" };
                }

                await SaveOTPToDatabase(mobileNumber, otp);
                return new OtpSendResult { Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while sending OTP");
                return new OtpSendResult { Success = false, ErrorMessage = ex.Message };
            }
        }


        public async Task<bool> VerifyOTPAsync(string mobileNumber, string otp)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(mobileNumber) || string.IsNullOrWhiteSpace(otp))
                {
                    _logger.LogWarning("Mobile number or OTP is empty");
                    return false;
                }

                // Find user in User table (where OTP is stored)
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserContact == mobileNumber);

                if (user == null || string.IsNullOrEmpty(user.LastOtp))
                {
                    _logger.LogWarning("User not found or no OTP generated for mobile: {MobileNumber}", mobileNumber);
                    return false;
                }

                // Check if OTP matches
                if (user.LastOtp.Trim() != otp.Trim())
                {
                    _logger.LogWarning("OTP mismatch for mobile: {MobileNumber}. Expected: {Expected}, Received: {Received}",
                        mobileNumber, user.LastOtp, otp);
                    return false;
                }

                // Check if OTP is expired
                if (user.OtpTime.HasValue)
                {
                    double minutes = DateTime.Now.Subtract(user.OtpTime.Value).TotalMinutes;
                    if (minutes > OTP_EXPIRY_MINUTES)
                    {
                        _logger.LogWarning("OTP expired for mobile: {MobileNumber}. Elapsed minutes: {Minutes}",
                            mobileNumber, minutes);

                        // Clear expired OTP
                        await ClearOTPFromDatabase(mobileNumber);
                        return false;
                    }
                }
                else
                {
                    _logger.LogError("OTP time missing for mobile: {MobileNumber}", mobileNumber);
                    return false;
                }

                // OTP is valid - clear it after successful verification
                await ClearOTPFromDatabase(mobileNumber);

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
                _logger.LogInformation("Attempting to save OTP for mobile: {MobileNumber}", mobileNumber);

                // Check if user already exists in Users table
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserContact == mobileNumber);

                if (existingUser != null)
                {
                    // User exists - update OTP details
                    _logger.LogInformation("Updating OTP for existing user: {MobileNumber}, UserType: {UserType}",
                        mobileNumber, existingUser.UserType);

                    existingUser.LastOtp = otp;
                    existingUser.OtpTime = DateTime.Now;
                }
                else
                {
                    // No user exists - create temporary OTP-only user with all required fields
                    _logger.LogInformation("Creating new temporary OTP user for: {MobileNumber}", mobileNumber);

                    var tempUser = new User
                    {
                        UserContact = mobileNumber,
                        LastOtp = otp,
                        OtpTime = DateTime.Now,
                        UserType = "OTP_TEMP",
                        FullName = "OTP_USER", // Temporary placeholder
                        UserEmail = $"otp_{mobileNumber}@temp.com", // Temporary email
                        UserGender = "Not Specified",
                        Password = "", // Empty password for OTP-only user
                        Token = "",
                        Dob = "1900-01-01", // Default DOB
                        Ftoken = "",
                        Stoken = "",
                        Utoken = "",
                        IsActive = "Y",
                        IsAdmin = 0,
                        AtDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        LoggingCount = "0"
                    };

                    _context.Users.Add(tempUser);
                }

                // Save changes to database
                int affectedRows = await _context.SaveChangesAsync();

                if (affectedRows > 0)
                {
                    _logger.LogInformation("OTP saved successfully for mobile: {MobileNumber}. Affected rows: {Rows}",
                        mobileNumber, affectedRows);

                    // Log the actual OTP for testing purposes (remove in production)
                    bool isTestMode = _configuration.GetValue<bool>("OTP:TestMode", false);
                    if (isTestMode)
                    {
                        _logger.LogInformation("TEST MODE - Generated OTP: {OTP} for mobile: {MobileNumber}",
                            otp, mobileNumber);
                    }
                }
                else
                {
                    _logger.LogWarning("No rows affected while saving OTP for mobile: {MobileNumber}", mobileNumber);
                }
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error while saving OTP for mobile: {MobileNumber}. Error: {Error}",
                    mobileNumber, dbEx.Message);
                throw new InvalidOperationException($"Failed to save OTP due to database error: {dbEx.Message}", dbEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while saving OTP for mobile: {MobileNumber}. Error: {Error}",
                    mobileNumber, ex.Message);
                throw new InvalidOperationException($"Failed to save OTP: {ex.Message}", ex);
            }
        }

        private async Task ClearOTPFromDatabase(string mobileNumber)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserContact == mobileNumber);

                if (user != null)
                {
                    user.LastOtp = null;
                    user.OtpTime = null;

                    // If this was a temporary OTP user, remove it completely
                    if (user.UserType == "OTP_TEMP")
                    {
                        _context.Users.Remove(user);
                        _logger.LogInformation("Removed temporary OTP user: {MobileNumber}", mobileNumber);
                    }
                    else
                    {
                        _logger.LogInformation("Cleared OTP for existing user: {MobileNumber}", mobileNumber);
                    }

                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing OTP from database for {MobileNumber}", mobileNumber);
                throw;
            }
        }

        // Helper method to clean up expired OTPs (call this periodically)
        public async Task CleanupExpiredOTPsAsync()
        {
            try
            {
                var expiredTime = DateTime.Now.AddMinutes(-OTP_EXPIRY_MINUTES);

                var expiredOtpUsers = await _context.Users
                    .Where(u => u.OtpTime.HasValue && u.OtpTime.Value < expiredTime)
                    .ToListAsync();

                foreach (var user in expiredOtpUsers)
                {
                    user.LastOtp = null;
                    user.OtpTime = null;

                    // Remove temporary OTP users
                    if (user.UserType == "OTP_TEMP")
                    {
                        _context.Users.Remove(user);
                    }
                }

                if (expiredOtpUsers.Any())
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Cleaned up {Count} expired OTP records", expiredOtpUsers.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during OTP cleanup");
            }
        }
    }

    public class OtpSendResult
    {
        public bool Success { get; set; }
        public string? Otp { get; set; } // null in production
        public string? ErrorMessage { get; set; }
    }

}