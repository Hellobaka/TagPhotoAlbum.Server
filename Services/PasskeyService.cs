using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using TagPhotoAlbum.Server.Data;
using TagPhotoAlbum.Server.Models;
using System.Text.Json.Serialization;
using NLog;

namespace TagPhotoAlbum.Server.Services;

public class PasskeyService
{
    private readonly AppDbContext _context;
    private readonly AuthService _authService;
    private readonly IConfiguration _configuration;
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public PasskeyService(AppDbContext context, AuthService authService, IConfiguration configuration)
    {
        _context = context;
        _authService = authService;
        _configuration = configuration;
    }

    public async Task<PasskeyRegistrationOptions> GenerateRegistrationOptions(string token)
    {
        _logger.Info("开始生成通行密钥注册选项");

        var challenge = GenerateChallenge();

        // 强制验证用户身份
        var existingUser = await ValidateUserByToken(token);
        if (existingUser == null)
        {
            _logger.Warn("生成通行密钥注册选项失败 - 无效的认证令牌");
            throw new UnauthorizedAccessException("无效的认证令牌，请先通过传统登录");
        }

        var userId = existingUser.Id.ToString();

        _logger.Info("验证用户身份成功 - 用户ID: {UserId}, 用户名: {Username}", userId, existingUser.Username);

        var options = new PasskeyRegistrationOptions
        {
            Challenge = challenge,  // 直接使用Base64编码的挑战，不需要再次编码
            Rp = new RelyingParty
            {
                Name = _configuration["Passkey:RelyingParty:Name"] ?? "TagPhotoAlbum",
                Id = _configuration["Passkey:RelyingParty:Id"] ?? "localhost"
            },
            User = new PasskeyUserInfo
            {
                Id = Convert.ToBase64String(Encoding.ASCII.GetBytes(existingUser.Id.ToString())),
                Name = existingUser.Username,
                DisplayName = existingUser.Name
            },
            PubKeyCredParams = new List<PublicKeyCredentialParameters>
            {
                new() { Type = "public-key", Alg = -7 }, // ES256
                new() { Type = "public-key", Alg = -257 } // RS256
            },
            AuthenticatorSelection = new AuthenticatorSelection
            {
                AuthenticatorAttachment = "platform",
                RequireResidentKey = true,
                UserVerification = "preferred"
            },
            Timeout = 60000,
            Attestation = "none"
        };

        // Store challenge in session or temporary storage
        await StoreChallenge(challenge, userId);

        _logger.Info("成功生成通行密钥注册选项 - 用户ID: {UserId}", userId);

        return options;
    }

    public async Task<PasskeyAuthenticationOptions> GenerateAuthenticationOptions(string? username = null)
    {
        _logger.Info("开始生成通行密钥认证选项 - 用户名: {Username}", username ?? "未指定");

        var challenge = GenerateChallenge();
        var allowCredentials = new List<string>();

        if (!string.IsNullOrEmpty(username))
        {
            var user = await _context.Users
                .Include(u => u.Passkeys)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user != null && user.Passkeys != null)
            {
                allowCredentials = user.Passkeys
                    .Where(p => p.IsActive)
                    .Select(p => p.CredentialId)
                    .ToList();
                _logger.Info("找到用户通行密钥 - 用户名: {Username}, 通行密钥数量: {Count}", username, allowCredentials.Count);
            }
        }

        var options = new PasskeyAuthenticationOptions
        {
            Challenge = challenge,
            Timeout = 60000,
            RelyingPartyId = _configuration["Passkey:RelyingParty:Id"] ?? "localhost",
            AllowCredentials = allowCredentials,
            UserVerification = "preferred"
        };

        // Store challenge in session or temporary storage
        await StoreChallenge(challenge, null);

        _logger.Info("成功生成通行密钥认证选项");

        return options;
    }

    public async Task<PasskeyRegistrationResult> RegisterPasskey(PasskeyRegistrationResponse response, string deviceName)
    {
        _logger.Info("开始注册通行密钥");

        try
        {
            // Parse and validate registration response
            var clientDataJson = Base64UrlDecode(response.Response.ClientDataJSON);
            var clientData = JsonSerializer.Deserialize<ClientData>(clientDataJson);
            if (clientData == null || string.IsNullOrEmpty(clientData.Challenge))
            {
                _logger.Warn("注册通行密钥失败 - 无效的挑战");
                return new PasskeyRegistrationResult { Success = false, Error = "无效的挑战" };
            }
            string challenge = clientData.Challenge;

            _logger.Info("解析客户端数据成功 - 挑战: {Challenge}", challenge);

            // Validate challenge FIRST before parsing client data
            var storedChallenge = await GetStoredChallenge(challenge);
            if (storedChallenge == null)
            {
                _logger.Warn("注册通行密钥失败 - 无效的挑战: {Challenge}", challenge);
                return new PasskeyRegistrationResult { Success = false, Error = "无效的挑战" };
            }

            // 修复挑战验证逻辑：客户端发送的挑战已经是Base64编码的，不需要再次编码
            if (clientData == null || clientData.Challenge != challenge)
            {
                _logger.Warn("注册通行密钥失败 - 挑战验证失败: {Challenge}", challenge);
                return new PasskeyRegistrationResult { Success = false, Error = "挑战验证失败" };
            }

            // Extract credential information
            var credentialId = response.Id;
            var publicKey = response.Response.AttestationObject;
            var userHandle = int.Parse(storedChallenge?.UserId ?? "");

            _logger.Info("提取凭证信息成功 - 凭证ID: {CredentialId}, 用户句柄: {UserHandle}", credentialId, userHandle);

            // Find user
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userHandle);
            if (user == null)
            {
                _logger.Warn("注册通行密钥失败 - 用户不存在: {UserHandle}", userHandle);
                return new PasskeyRegistrationResult { Success = false, Error = "用户不存在" };
            }

            // Create passkey record
            var passkey = new Passkey
            {
                UserId = user.Id,
                CredentialId = credentialId,
                PublicKey = publicKey,
                UserHandle = userHandle,
                Counter = 0,
                DeviceType = "Platform Authenticator",
                DeviceName = string.IsNullOrEmpty(deviceName) ? "Default Device" : deviceName,
                CreatedAt = DateTime.UtcNow,
                LastUsedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Passkeys.Add(passkey);
            await _context.SaveChangesAsync();

            _logger.Info("保存通行密钥记录成功 - 凭证ID: {CredentialId}, 用户ID: {UserId}", credentialId, user.Id);

            // Clean up stored challenge
            await RemoveChallenge(challenge);

            _logger.Info("成功注册通行密钥 - 凭证ID: {CredentialId}", credentialId);

            return new PasskeyRegistrationResult
            {
                Success = true,
                CredentialId = credentialId
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "注册通行密钥时发生异常");
            return new PasskeyRegistrationResult { Success = false, Error = ex.Message };
        }
    }

    public async Task<PasskeyAuthenticationResult> AuthenticateWithPasskey(PasskeyAuthenticationResponse response)
    {
        _logger.Info("开始通行密钥认证");

        try
        {
            // Parse and validate authentication response
            var clientDataJson = Base64UrlDecode(response.Response.ClientDataJSON);
            var clientData = JsonSerializer.Deserialize<ClientData>(clientDataJson);
            if (clientData == null || string.IsNullOrEmpty(clientData.Challenge))
            {
                _logger.Warn("通行密钥认证失败 - 无效的挑战");
                return new PasskeyAuthenticationResult { Success = false, Error = "无效的挑战" };
            }
            string challenge = clientData.Challenge;

            _logger.Info("解析客户端数据成功 - 挑战: {Challenge}", challenge);

            // Validate challenge
            var storedChallenge = await GetStoredChallenge(challenge);
            if (storedChallenge == null)
            {
                _logger.Warn("通行密钥认证失败 - 无效的挑战: {Challenge}", challenge);
                return new PasskeyAuthenticationResult { Success = false, Error = "无效的挑战" };
            }

            if (clientData == null || clientData.Challenge != challenge)
            {
                _logger.Warn("通行密钥认证失败 - 挑战验证失败: {Challenge}", challenge);
                return new PasskeyAuthenticationResult { Success = false, Error = "挑战验证失败" };
            }

            // Find passkey by credential ID
            var passkey = await _context.Passkeys
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.CredentialId == response.Id && p.IsActive);

            if (passkey == null)
            {
                _logger.Warn("通行密钥认证失败 - 无效的通行密钥: {CredentialId}", response.Id);
                return new PasskeyAuthenticationResult { Success = false, Error = "无效的通行密钥" };
            }

            _logger.Info("找到通行密钥 - 凭证ID: {CredentialId}, 用户ID: {UserId}", response.Id, passkey.UserId);

            // Verify signature (simplified - in production use proper cryptographic verification)
            if (!VerifySignature(response, passkey.PublicKey))
            {
                _logger.Warn("通行密钥认证失败 - 签名验证失败: {CredentialId}", response.Id);
                return new PasskeyAuthenticationResult { Success = false, Error = "签名验证失败" };
            }

            // Update passkey usage
            passkey.LastUsedAt = DateTime.UtcNow;
            passkey.Counter++;
            await _context.SaveChangesAsync();

            _logger.Info("更新通行密钥使用记录 - 凭证ID: {CredentialId}, 计数器: {Counter}", response.Id, passkey.Counter);

            // Generate JWT token
            var token = _authService.GenerateJwtToken(passkey.User);

            // Clean up stored challenge
            await RemoveChallenge(challenge);

            _logger.Info("通行密钥认证成功 - 凭证ID: {CredentialId}, 用户ID: {UserId}", response.Id, passkey.UserId);

            return new PasskeyAuthenticationResult
            {
                Success = true,
                Token = token,
                User = passkey.User
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "通行密钥认证时发生异常");
            return new PasskeyAuthenticationResult { Success = false, Error = ex.Message };
        }
    }

    public async Task<List<GetPasskeysResponse>> GetUserPasskeys(int userId)
    {
        _logger.Info("开始获取用户通行密钥 - 用户ID: {UserId}", userId);

        var r = await _context.Passkeys
            .Where(p => p.UserId == userId && p.IsActive)
            .OrderByDescending(p => p.LastUsedAt)
            .ToListAsync();

        _logger.Info("成功获取用户通行密钥 - 用户ID: {UserId}, 数量: {Count}", userId, r.Count);

        return r.Select(x => new GetPasskeysResponse { Id = x.Id, DeviceName = x.DeviceName, CreateAt = x.CreatedAt, LastUsedAt = x.LastUsedAt }).ToList();
    }

    public async Task<bool> DeletePasskey(int passkeyId, int userId)
    {
        _logger.Info("开始删除通行密钥 - 通行密钥ID: {PasskeyId}, 用户ID: {UserId}", passkeyId, userId);

        var passkey = await _context.Passkeys
            .FirstOrDefaultAsync(p => p.Id == passkeyId && p.UserId == userId);

        if (passkey == null)
        {
            _logger.Warn("删除通行密钥失败 - 通行密钥不存在: {PasskeyId}", passkeyId);
            return false;
        }

        passkey.IsActive = false;
        await _context.SaveChangesAsync();

        _logger.Info("成功删除通行密钥 - 通行密钥ID: {PasskeyId}, 用户ID: {UserId}", passkeyId, userId);

        return true;
    }

    private string GenerateChallenge()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes).TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private string GenerateUserId()
    {
        var randomBytes = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private async Task StoreChallenge(string challenge, string? userId)
    {
        var challengeData = new ChallengeData
        {
            Challenge = challenge,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        Challenges[challenge] = challengeData;
        await Task.CompletedTask;
    }

    private async Task<ChallengeData?> GetStoredChallenge(string challenge)
    {
        if (Challenges.TryGetValue(challenge, out var challengeData))
        {
            // Check if challenge is still valid (5 minutes)
            if (DateTime.UtcNow - challengeData.CreatedAt < TimeSpan.FromMinutes(5))
            {
                return challengeData;
            }
            Challenges.Remove(challenge);
        }
        await Task.CompletedTask;
        return null;
    }

    private async Task RemoveChallenge(string challenge)
    {
        Challenges.Remove(challenge);
        await Task.CompletedTask;
    }

    private async Task<User?> ValidateUserByToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "");

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidAudience = _configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(key)
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
            {
                return await _context.Users.FindAsync(userId);
            }
        }
        catch
        {
            // Token 验证失败
        }

        return null;
    }

    private string Base64UrlDecode(string base64Url)
    {
        var base64 = base64Url.Replace('-', '+').Replace('_', '/');
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        var decodedBytes = Convert.FromBase64String(base64);
        var decodedString = Encoding.UTF8.GetString(decodedBytes);

        return decodedString;
    }

    private bool VerifySignature(PasskeyAuthenticationResponse response, string publicKey)
    {
        // Simplified signature verification
        // In production, use proper cryptographic libraries to verify the signature
        // against the authenticator data and client data JSON
        return true; // Placeholder for demo
    }

    // Simple in-memory storage for challenges (replace with proper storage in production)
    private static readonly Dictionary<string, ChallengeData> Challenges = new();
}

// Helper classes for challenge storage
public class ChallengeData
{
    public string Challenge { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ClientData
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    [JsonPropertyName("challenge")]
    public string Challenge { get; set; } = string.Empty;
    [JsonPropertyName("origin")]
    public string Origin { get; set; } = string.Empty;
    [JsonPropertyName("crossOrigin")]
    public bool CrossOrigin { get; set; }
    [JsonPropertyName("user")]
    public PasskeyUserInfo? User { get; set; }
}