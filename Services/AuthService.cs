using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using TagPhotoAlbum.Server.Data;
using TagPhotoAlbum.Server.Models;

namespace TagPhotoAlbum.Server.Services;

public class AuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<User?> AuthenticateUser(string username, string password)
    {
        // For demo purposes, using simple password check
        // In production, use proper password hashing
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username && u.PasswordHash == password);

        return user;
    }

    public async Task<User?> AuthenticateUserSecure(string username, string passwordHash)
    {
        // For secure login, compare password hashes
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username && u.PasswordHash == passwordHash);

        return user;
    }

    public bool ValidateSecureRequest(SecureLoginRequest request)
    {
        // Check if request is within 5 minutes
        var requestTime = DateTimeOffset.FromUnixTimeMilliseconds(request.Timestamp).UtcDateTime;
        var currentTime = DateTime.UtcNow;
        var timeDiff = currentTime - requestTime;

        if (timeDiff.TotalMinutes > 5 || timeDiff.TotalMinutes < -5)
        {
            return false; // Request too old or from future
        }

        // Validate signature
        var payload = $"{request.Username}:{request.PasswordHash}:{request.Timestamp}:{request.Nonce}";
        var expectedSignature = ComputeHmacSignature(payload);

        return string.Equals(request.Signature, expectedSignature, StringComparison.OrdinalIgnoreCase);
    }

    private string ComputeHmacSignature(string payload)
    {
        var key = _configuration["Jwt:Key"] ?? "default-secret-key";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLower();
    }

    public string GenerateNonceSeed()
    {
        var randomBytes = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? ""));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddMinutes(Convert.ToDouble(_configuration["Jwt:ExpireMinutes"] ?? "60")),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}