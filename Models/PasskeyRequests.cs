namespace TagPhotoAlbum.Server.Models;

public class PasskeyRegistrationRequest
{
    // 现在只需要从JWT Token中获取用户信息
}

public class PasskeyRegistrationOptions
{
    public string Challenge { get; set; } = string.Empty;
    public RelyingParty Rp { get; set; } = new();
    public PasskeyUserInfo User { get; set; } = new();
    public List<PublicKeyCredentialParameters> PubKeyCredParams { get; set; } = new();
    public AuthenticatorSelection AuthenticatorSelection { get; set; } = new();
    public int Timeout { get; set; } = 60000;
    public string Attestation { get; set; } = "none";
}

public class PasskeyAuthenticationOptions
{
    public string Challenge { get; set; } = string.Empty;
    public int Timeout { get; set; } = 60000;
    public string RelyingPartyId { get; set; } = string.Empty;
    public List<string> AllowCredentials { get; set; } = new();
    public string UserVerification { get; set; } = "preferred";
}

public class PasskeyRegistrationResponse
{
    public string Id { get; set; } = string.Empty;
    public string RawId { get; set; } = string.Empty;
    public AuthenticatorAttestationResponse Response { get; set; } = new();
    public string Type { get; set; } = "public-key";
}

public class PasskeyAuthenticationResponse
{
    public string Id { get; set; } = string.Empty;
    public string RawId { get; set; } = string.Empty;
    public AuthenticatorAssertionResponse Response { get; set; } = new();
    public string Type { get; set; } = "public-key";
}

public class RelyingParty
{
    public string Name { get; set; } = "TagPhotoAlbum";
    public string Id { get; set; } = "localhost";
}

public class PasskeyUserInfo
{
    public string Id { get; set; } = string.Empty;        // User ID - 必需，数据库主键，不能是邮箱/手机号
    public string Name { get; set; } = string.Empty;      // User Name - 必需，用于区分凭证的用户名
    public string DisplayName { get; set; } = string.Empty; // User Display Name - 必需，用户友好的显示名称
}

public class PublicKeyCredentialParameters
{
    public string Type { get; set; } = "public-key";
    public int Alg { get; set; } = -7; // ES256
}

public class AuthenticatorSelection
{
    public string AuthenticatorAttachment { get; set; } = "platform";
    public bool RequireResidentKey { get; set; } = true;
    public string UserVerification { get; set; } = "preferred";
}

public class AuthenticatorAttestationResponse
{
    public string ClientDataJSON { get; set; } = string.Empty;
    public string AttestationObject { get; set; } = string.Empty;
    public List<string> Transports { get; set; } = new();
}

public class AuthenticatorAssertionResponse
{
    public string ClientDataJSON { get; set; } = string.Empty;
    public string AuthenticatorData { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public string? UserHandle { get; set; }
}

public class PasskeyRegistrationResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? CredentialId { get; set; }
}

public class PasskeyAuthenticationResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? Token { get; set; }
    public User? User { get; set; }
}