using Microsoft.AspNetCore.Mvc;
using TagPhotoAlbum.Server.Models;
using TagPhotoAlbum.Server.Services;
using NLog;

namespace TagPhotoAlbum.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<SecureLoginResponse>>> SecureLogin([FromBody] SecureLoginRequest request)
    {
        try
        {
            _logger.Info("开始用户登录 - 用户名: {Username}", request.Username);
            // Validate secure request
            if (!_authService.ValidateSecureRequest(request))
            {
                return Unauthorized(new ApiResponse<SecureLoginResponse>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "AUTH_ERROR",
                        Message = "请求验证失败"
                    }
                });
            }

            var user = await _authService.AuthenticateUserSecure(request.Username, request.PasswordHash);

            if (user == null)
            {
                return Unauthorized(new ApiResponse<SecureLoginResponse>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "AUTH_ERROR",
                        Message = "用户名或密码错误"
                    }
                });
            }

            var token = _authService.GenerateJwtToken(user);
            var serverTimestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var nextNonceSeed = _authService.GenerateNonceSeed();

            _logger.Info("用户登录成功 - 用户名: {Username}", request.Username);

            return Ok(new ApiResponse<SecureLoginResponse>
            {
                Success = true,
                Data = new SecureLoginResponse
                {
                    User = new User
                    {
                        Username = user.Username,
                        Name = user.Name,
                        Email = user.Email
                    },
                    Token = token,
                    ServerTimestamp = serverTimestamp,
                    NextNonceSeed = nextNonceSeed
                }
            });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "用户登录失败");
            return StatusCode(500, new ApiResponse<SecureLoginResponse>
            {
                Success = false,
                Error = new ErrorResponse
                {
                    Code = "SERVER_ERROR",
                    Message = "服务器内部错误",
                    Details = ex.Message
                }
            });
        }
    }

    [HttpGet("nonce-seed")]
    public ActionResult<ApiResponse<string>> GetNonceSeed()
    {
        try
        {
            _logger.Info("开始生成Nonce种子");
            var nonceSeed = _authService.GenerateNonceSeed();

            _logger.Info("成功生成Nonce种子");

            return Ok(new ApiResponse<string>
            {
                Success = true,
                Data = nonceSeed
            });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "生成Nonce种子失败");
            return StatusCode(500, new ApiResponse<string>
            {
                Success = false,
                Error = new ErrorResponse
                {
                    Code = "SERVER_ERROR",
                    Message = "服务器内部错误",
                    Details = ex.Message
                }
            });
        }
    }
}