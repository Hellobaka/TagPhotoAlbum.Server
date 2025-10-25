using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using TagPhotoAlbum.Server.Data;
using TagPhotoAlbum.Server.Models;
using NLog;

namespace TagPhotoAlbum.Server.Services;

public class AuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public AuthService(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<User?> AuthenticateUser(string username, string password)
    {
        _logger.Info("开始用户认证 - 用户名: {Username}", username);

        // For demo purposes, using simple password check
        // In production, use proper password hashing
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username && u.PasswordHash == password);

        if (user != null)
        {
            _logger.Info("用户认证成功 - 用户名: {Username}", username);
        }
        else
        {
            _logger.Warn("用户认证失败 - 用户名: {Username}", username);
        }

        return user;
    }

    public async Task<User?> AuthenticateUserSecure(string username, string passwordHash)
    {
        _logger.Info("开始安全用户认证 - 用户名: {Username}", username);

        // For secure login, compare password hashes
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username && u.PasswordHash == passwordHash);

        if (user != null)
        {
            _logger.Info("安全用户认证成功 - 用户名: {Username}", username);
        }
        else
        {
            _logger.Warn("安全用户认证失败 - 用户名: {Username}", username);
        }

        return user;
    }

    public bool ValidateSecureRequest(SecureLoginRequest request)
    {
        _logger.Info("开始验证安全请求 - 用户名: {Username}", request.Username);

        // Check if request is within 5 minutes
        var requestTime = DateTimeOffset.FromUnixTimeMilliseconds(request.Timestamp).UtcDateTime;
        var currentTime = DateTime.UtcNow;
        var timeDiff = currentTime - requestTime;

        if (timeDiff.TotalMinutes > 5 || timeDiff.TotalMinutes < -5)
        {
            _logger.Warn("安全请求验证失败 - 请求时间超出范围 - 用户名: {Username}", request.Username);
            return false; // Request too old or from future
        }

        // Validate signature
        var payload = $"{request.Username}:{request.PasswordHash}:{request.Timestamp}:{request.Nonce}";
        var expectedSignature = ComputeHmacSignature(payload);

        var isValid = string.Equals(request.Signature, expectedSignature, StringComparison.OrdinalIgnoreCase);

        if (isValid)
        {
            _logger.Info("安全请求验证成功 - 用户名: {Username}", request.Username);
        }
        else
        {
            _logger.Warn("安全请求验证失败 - 签名不匹配 - 用户名: {Username}", request.Username);
        }

        return isValid;
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
        _logger.Info("开始生成Nonce种子");

        var randomBytes = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        var nonceSeed = Convert.ToBase64String(randomBytes);

        _logger.Info("成功生成Nonce种子");

        return nonceSeed;
    }

    public string GenerateJwtToken(User user)
    {
        _logger.Info("开始生成JWT令牌 - 用户ID: {UserId}, 用户名: {Username}", user.Id, user.Username);

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

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        _logger.Info("成功生成JWT令牌 - 用户ID: {UserId}, 用户名: {Username}", user.Id, user.Username);

        return tokenString;
    }
}