using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TagPhotoAlbum.Server.Data;
using TagPhotoAlbum.Server.Models;
using TagPhotoAlbum.Server.Services;
using NLog;

namespace TagPhotoAlbum.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PasskeyController : ControllerBase
{
    private readonly PasskeyService _passkeyService;
    private readonly AppDbContext _context;
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public PasskeyController(PasskeyService passkeyService, AppDbContext context)
    {
        _passkeyService = passkeyService;
        _context = context;
    }

    [Authorize]
    [HttpPost("registration-options")]
    public async Task<ActionResult<ApiResponse<PasskeyRegistrationOptions>>> GetRegistrationOptions()
    {
        try
        {
            _logger.Info("开始生成通行密钥注册选项");
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var options = await _passkeyService.GenerateRegistrationOptions(token);

            _logger.Info("成功生成通行密钥注册选项");

            return Ok(new ApiResponse<PasskeyRegistrationOptions>
            {
                Success = true,
                Data = options
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.Warn("生成通行密钥注册选项失败 - 未授权: {Message}", ex.Message);
            return Unauthorized(new ApiResponse<PasskeyRegistrationOptions>
            {
                Success = false,
                Error = new ErrorResponse
                {
                    Code = "UNAUTHORIZED",
                    Message = ex.Message
                }
            });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "生成通行密钥注册选项失败");
            return StatusCode(500, new ApiResponse<PasskeyRegistrationOptions>
            {
                Success = false,
                Error = new ErrorResponse
                {
                    Code = "SERVER_ERROR",
                    Message = "生成注册选项失败",
                    Details = ex.Message
                }
            });
        }
    }

    [HttpPost("authentication-options")]
    public async Task<ActionResult<ApiResponse<PasskeyAuthenticationOptions>>> GetAuthenticationOptions([FromBody] string? username = null)
    {
        try
        {
            _logger.Info("开始生成通行密钥认证选项 - 用户名: {Username}", username ?? "未指定");
            var options = await _passkeyService.GenerateAuthenticationOptions(username);

            _logger.Info("成功生成通行密钥认证选项");

            return Ok(new ApiResponse<PasskeyAuthenticationOptions>
            {
                Success = true,
                Data = options
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<PasskeyAuthenticationOptions>
            {
                Success = false,
                Error = new ErrorResponse
                {
                    Code = "SERVER_ERROR",
                    Message = "生成认证选项失败",
                    Details = ex.Message
                }
            });
        }
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<PasskeyRegistrationResult>>> Register([FromBody] PasskeyRegistrationRequestWithDeviceName request)
    {
        try
        {
            _logger.Info("开始注册通行密钥");
            var result = await _passkeyService.RegisterPasskey(request.Response, request.DeviceName);

            if (!result.Success)
            {
                _logger.Warn("注册通行密钥失败: {Error}", result.Error ?? "未知错误");
                return BadRequest(new ApiResponse<PasskeyRegistrationResult>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "REGISTRATION_ERROR",
                        Message = result.Error ?? "注册失败"
                    }
                });
            }

            _logger.Info("成功注册通行密钥");

            return Ok(new ApiResponse<PasskeyRegistrationResult>
            {
                Success = true,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "注册通行密钥失败");
            return StatusCode(500, new ApiResponse<PasskeyRegistrationResult>
            {
                Success = false,
                Error = new ErrorResponse
                {
                    Code = "SERVER_ERROR",
                    Message = "注册通行密钥失败",
                    Details = ex.Message
                }
            });
        }
    }

    [HttpPost("authenticate")]
    public async Task<ActionResult<ApiResponse<PasskeyAuthenticationResult>>> Authenticate([FromBody] PasskeyAuthenticationRequestWithChallenge request)
    {
        try
        {
            _logger.Info("开始通行密钥认证");
            var result = await _passkeyService.AuthenticateWithPasskey(request.Response);

            if (!result.Success)
            {
                _logger.Warn("通行密钥认证失败: {Error}", result.Error ?? "未知错误");
                return Unauthorized(new ApiResponse<PasskeyAuthenticationResult>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "AUTHENTICATION_ERROR",
                        Message = result.Error ?? "认证失败"
                    }
                });
            }

            _logger.Info("通行密钥认证成功");

            return Ok(new ApiResponse<PasskeyAuthenticationResult>
            {
                Success = true,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "通行密钥认证失败");
            return StatusCode(500, new ApiResponse<PasskeyAuthenticationResult>
            {
                Success = false,
                Error = new ErrorResponse
                {
                    Code = "SERVER_ERROR",
                    Message = "通行密钥认证失败",
                    Details = ex.Message
                }
            });
        }
    }

    [Authorize]
    [HttpGet("user-passkeys")]
    public async Task<ActionResult<ApiResponse<List<GetPasskeysResponse>>>> GetUserPasskeys()
    {
        try
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "");
            _logger.Info("开始获取用户通行密钥 - 用户ID: {UserId}", userId);
            var passkeys = await _passkeyService.GetUserPasskeys(userId);

            _logger.Info("成功获取用户通行密钥 - 数量: {Count}", passkeys.Count);

            return Ok(new ApiResponse<List<GetPasskeysResponse>>
            {
                Success = true,
                Data = passkeys
            });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "获取用户通行密钥失败");
            return StatusCode(500, new ApiResponse<List<GetPasskeysResponse>>
            {
                Success = false,
                Error = new ErrorResponse
                {
                    Code = "SERVER_ERROR",
                    Message = "获取用户通行密钥失败",
                    Details = ex.Message
                }
            });
        }
    }

    [Authorize]
    [HttpDelete("{passkeyId}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeletePasskey(int passkeyId)
    {
        try
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "");
            _logger.Info("开始删除通行密钥 - 通行密钥ID: {PasskeyId}, 用户ID: {UserId}", passkeyId, userId);
            var result = await _passkeyService.DeletePasskey(passkeyId, userId);

            if (!result)
            {
                _logger.Warn("删除通行密钥失败 - 通行密钥不存在: {PasskeyId}", passkeyId);
                return NotFound(new ApiResponse<bool>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "NOT_FOUND",
                        Message = "通行密钥不存在"
                    }
                });
            }

            _logger.Info("成功删除通行密钥 - 通行密钥ID: {PasskeyId}", passkeyId);

            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = true
            });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "删除通行密钥失败 - 通行密钥ID: {PasskeyId}", passkeyId);
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Error = new ErrorResponse
                {
                    Code = "SERVER_ERROR",
                    Message = "删除通行密钥失败",
                    Details = ex.Message
                }
            });
        }
    }
}

// Additional request models
public class PasskeyRegistrationRequestWithDeviceName
{
    public PasskeyRegistrationResponse Response { get; set; } = new();
    public string DeviceName { get; set; } = string.Empty;
}

public class PasskeyAuthenticationRequestWithChallenge
{
    public PasskeyAuthenticationResponse Response { get; set; } = new();
    public string Challenge { get; set; } = string.Empty;
}