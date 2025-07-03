using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ArimartEcommerceAPI.Infrastructure.Data.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string CreateToken(TblUser user)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()), // Use Id as immutable user id
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("phone", user.Phone ?? string.Empty),
            new Claim(ClaimTypes.Role, user.UserType ?? "User"),
            new Claim(JwtRegisteredClaimNames.Iat,
                      DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                      ClaimValueTypes.Integer64)
        };

        var key = _configuration["JwtSettings:Key"];
        var issuer = _configuration["JwtSettings:Issuer"];
        var audience = _configuration["JwtSettings:Audience"];
        var days = int.Parse(_configuration["JwtSettings:ExpiresInDays"] ?? "7");

        if (string.IsNullOrWhiteSpace(key) ||
            string.IsNullOrWhiteSpace(issuer) ||
            string.IsNullOrWhiteSpace(audience))
        {
            throw new InvalidOperationException("JWT configuration values are missing.");
        }

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(days),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
