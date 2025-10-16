using Microsoft.AspNetCore.Mvc;
using TagPhotoAlbum.Server.Models;
using TagPhotoAlbum.Server.Services;

namespace TagPhotoAlbum.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var user = await _authService.AuthenticateUser(request.Username, request.Password);

            if (user == null)
            {
                return Unauthorized(new ApiResponse<LoginResponse>
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

            return Ok(new ApiResponse<LoginResponse>
            {
                Success = true,
                Data = new LoginResponse
                {
                    User = new User
                    {
                        Username = user.Username,
                        Name = user.Name,
                        Email = user.Email
                    },
                    Token = token
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<LoginResponse>
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