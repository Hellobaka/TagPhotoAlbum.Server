using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TagPhotoAlbum.Server.Data;
using TagPhotoAlbum.Server.Models;

namespace TagPhotoAlbum.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SearchController : ControllerBase
{
    private readonly AppDbContext _context;

    public SearchController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<Photo>>>> SearchPhotos([FromQuery] string q)
    {
        try
        {
            if (string.IsNullOrEmpty(q))
            {
                return BadRequest(new ApiResponse<List<Photo>>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "VALIDATION_ERROR",
                        Message = "搜索关键词不能为空"
                    }
                });
            }

            var photos = await _context.Photos
                .Where(p => p.Title.Contains(q) ||
                           p.Description.Contains(q) ||
                           p.Tags.Any(t => t.Tag.Name.Contains(q)) ||
                           p.Folder.Contains(q) ||
                           p.Location.Contains(q))
                .OrderByDescending(p => p.Date)
                .ToListAsync();

            return Ok(new ApiResponse<List<Photo>>
            {
                Success = true,
                Data = photos
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<List<Photo>>
            {
                Success = false,
                Error = new ErrorResponse
                {
                    Code = "SERVER_ERROR",
                    Message = "搜索失败",
                    Details = ex.Message
                }
            });
        }
    }
}