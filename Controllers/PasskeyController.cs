using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TagPhotoAlbum.Server.Data;
using TagPhotoAlbum.Server.Models;
using TagPhotoAlbum.Server.Services;

namespace TagPhotoAlbum.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PasskeyController : ControllerBase
{
    private readonly PasskeyService _passkeyService;
    private readonly AppDbContext _context;

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
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var options = await _passkeyService.GenerateRegistrationOptions(token);

            return Ok(new ApiResponse<PasskeyRegistrationOptions>
            {
                Success = true,
                Data = options
            });
        }
        catch (UnauthorizedAccessException ex)
        {
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
            var options = await _passkeyService.GenerateAuthenticationOptions(username);

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
    public async Task<ActionResult<ApiResponse<PasskeyRegistrationResult>>> Register([FromBody] PasskeyRegistrationRequestWithChallenge request)
    {
        try
        {
            var result = await _passkeyService.RegisterPasskey(request.Response);

            if (!result.Success)
            {
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

            return Ok(new ApiResponse<PasskeyRegistrationResult>
            {
                Success = true,
                Data = result
            });
        }
        catch (Exception ex)
        {
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
            var result = await _passkeyService.AuthenticateWithPasskey(request.Response);

            if (!result.Success)
            {
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

            return Ok(new ApiResponse<PasskeyAuthenticationResult>
            {
                Success = true,
                Data = result
            });
        }
        catch (Exception ex)
        {
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
            var passkeys = await _passkeyService.GetUserPasskeys(userId);

            return Ok(new ApiResponse<List<GetPasskeysResponse>>
            {
                Success = true,
                Data = passkeys
            });
        }
        catch (Exception ex)
        {
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
            var result = await _passkeyService.DeletePasskey(passkeyId, userId);

            if (!result)
            {
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

            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = true
            });
        }
        catch (Exception ex)
        {
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
public class PasskeyRegistrationRequestWithChallenge
{
    public PasskeyRegistrationResponse Response { get; set; } = new();
    public string Challenge { get; set; } = string.Empty;
}

public class PasskeyAuthenticationRequestWithChallenge
{
    public PasskeyAuthenticationResponse Response { get; set; } = new();
    public string Challenge { get; set; } = string.Empty;
}