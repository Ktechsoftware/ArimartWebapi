using Microsoft.Extensions.Logging;

public class MockOTPService : IOTPService
{
    private readonly ILogger<MockOTPService> _logger;

    // Make this static so all instances share the same store
    private static readonly Dictionary<string, OtpData> _otpStore = new();
    private static readonly object _lockObject = new object();

    private const int OTP_EXPIRY_MINUTES = 10;

    public MockOTPService(ILogger<MockOTPService> logger)
    {
        _logger = logger;
    }

    public Task<string?> SendOTPAsync(string mobileNumber)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(mobileNumber) || mobileNumber.Length != 10)
            {
                _logger?.LogWarning("Invalid mobile number: {MobileNumber}", mobileNumber);
                return Task.FromResult<string?>(null);
            }

            string mockOtp = "123456";

            lock (_lockObject)
            {
                _otpStore[mobileNumber] = new OtpData
                {
                    Otp = mockOtp,
                    CreatedAt = DateTime.Now,
                    ExpiresAt = DateTime.Now.AddMinutes(OTP_EXPIRY_MINUTES)
                };
            }

            _logger?.LogInformation("Mock OTP sent to {MobileNumber}: {OTP}", mobileNumber, mockOtp);
            _logger?.LogInformation("OTP Store now contains {Count} entries", _otpStore.Count);

            return Task.FromResult<string?>(mockOtp);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error sending mock OTP to {MobileNumber}", mobileNumber);
            return Task.FromResult<string?>(null);
        }
    }

    public Task<bool> VerifyOTPAsync(string mobileNumber, string otp)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(mobileNumber) || string.IsNullOrWhiteSpace(otp))
            {
                _logger?.LogWarning("Mobile number or OTP is null/empty");
                return Task.FromResult(false);
            }

            CleanupExpiredOtps();

            lock (_lockObject)
            {
                _logger?.LogInformation("OTP Store contains {Count} entries before verification", _otpStore.Count);

                if (!_otpStore.TryGetValue(mobileNumber, out var storedOtpData))
                {
                    _logger?.LogWarning("No OTP found for mobile: {MobileNumber}", mobileNumber);
                    _logger?.LogWarning("Available mobile numbers in store: {MobileNumbers}",
                        string.Join(", ", _otpStore.Keys));
                    return Task.FromResult(false);
                }

                if (storedOtpData.Otp != otp)
                {
                    _logger?.LogWarning("Invalid OTP for mobile: {MobileNumber}. Expected: {Expected}, Received: {Received}",
                        mobileNumber, storedOtpData.Otp, otp);
                    return Task.FromResult(false);
                }

                if (DateTime.Now > storedOtpData.ExpiresAt)
                {
                    _logger?.LogWarning("Expired OTP for mobile: {MobileNumber}", mobileNumber);
                    _otpStore.Remove(mobileNumber);
                    return Task.FromResult(false);
                }

                _otpStore.Remove(mobileNumber);
                _logger?.LogInformation("Mock OTP verified successfully for mobile: {MobileNumber}", mobileNumber);
                return Task.FromResult(true);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error verifying mock OTP for {MobileNumber}", mobileNumber);
            return Task.FromResult(false);
        }
    }

    private void CleanupExpiredOtps()
    {
        lock (_lockObject)
        {
            var expiredKeys = _otpStore
                .Where(kvp => DateTime.Now > kvp.Value.ExpiresAt)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _otpStore.Remove(key);
            }
        }
    }

    public Dictionary<string, OtpData> GetCurrentOtps()
    {
        CleanupExpiredOtps();
        lock (_lockObject)
        {
            return new Dictionary<string, OtpData>(_otpStore);
        }
    }
}

public class OtpData
{
    public string Otp { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}