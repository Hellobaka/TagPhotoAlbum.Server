using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TagPhotoAlbum.Server.Data;
using TagPhotoAlbum.Server.Models;
using NLog;

namespace TagPhotoAlbum.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SearchController : ControllerBase
{
    private readonly AppDbContext _context;
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public SearchController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<Photo>>>> SearchPhotos([FromQuery] string q)
    {
        try
        {
            _logger.Info("开始搜索照片 - 关键词: {Query}", q);
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

            _logger.Info("搜索照片成功 - 关键词: {Query}, 结果数量: {Count}", q, photos.Count);

            return Ok(new ApiResponse<List<Photo>>
            {
                Success = true,
                Data = photos
            });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "搜索照片失败 - 关键词: {Query}", q);
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