using System;
using System.Collections.Generic;

namespace ArimartEcommerceAPI.Infrastructure.Data.Models;

public partial class User
{
    public int UserId { get; set; }

    public string FullName { get; set; } = null!;

    public string UserContact { get; set; } = null!;

    public string UserEmail { get; set; } = null!;

    public string UserGender { get; set; } = null!;

    public string? IsActive { get; set; }

    public string LastOtp { get; set; } = null!;

    public string? OtpTime { get; set; }

    public short IsAdmin { get; set; }

    public string Password { get; set; } = null!;

    public string Token { get; set; } = null!;

    public string? AtDate { get; set; }

    public string Dob { get; set; } = null!;

    public string UserType { get; set; } = null!;

    public string Ftoken { get; set; } = null!;

    public string Stoken { get; set; } = null!;

    public string? LoggingCount { get; set; }

    public string Utoken { get; set; } = null!;
}
