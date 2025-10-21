namespace TagPhotoAlbum.Server.Models;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public ErrorResponse? Error { get; set; }
    public PaginationInfo? Pagination { get; set; }
    public string? Message { get; set; }
}

public class ErrorResponse
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public object? Details { get; set; }
}

public class PaginationInfo
{
    public int Page { get; set; }
    public int Limit { get; set; }
    public int Total { get; set; }
    public int Pages { get; set; }
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class SecureLoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public long Timestamp { get; set; }
    public string Nonce { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
}

public class LoginResponse
{
    public User User { get; set; } = new();
    public string Token { get; set; } = string.Empty;
}

public class SecureLoginResponse
{
    public User User { get; set; } = new();
    public string Token { get; set; } = string.Empty;
    public long ServerTimestamp { get; set; }
    public string NextNonceSeed { get; set; } = string.Empty;
}