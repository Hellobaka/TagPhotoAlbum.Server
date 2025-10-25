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
public class MetadataController : ControllerBase
{
    private readonly AppDbContext _context;
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public MetadataController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("tags")]
    public async Task<ActionResult<ApiResponse<TagsResponse>>> GetTags()
    {
        try
        {
            _logger.Info("开始获取标签列表");
            // 获取每个标签的使用次数
            var tagCounts = await _context.PhotoTags
                .GroupBy(pt => pt.Tag.Name)
                .Select(g => new TagInfo
                {
                    Name = g.Key,
                    Count = g.Count()
                })
                .OrderBy(t => t.Name)
                .ToListAsync();

            var response = new TagsResponse
            {
                Tags = tagCounts,
                TotalCount = tagCounts.Count
            };

            _logger.Info("成功获取标签列表 - 标签数量: {Count}", tagCounts.Count);

            return Ok(new ApiResponse<TagsResponse>
            {
                Success = true,
                Data = response
            });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "获取标签列表失败");
            return StatusCode(500, new ApiResponse<TagsResponse>
            {
                Success = false,
                Error = new ErrorResponse
                {
                    Code = "SERVER_ERROR",
                    Message = "获取标签失败",
                    Details = ex.Message
                }
            });
        }
    }


    [HttpGet("folders")]
    public async Task<ActionResult<ApiResponse<List<string>>>> GetFolders()
    {
        try
        {
            _logger.Info("开始获取文件夹列表");
            var folders = await _context.Photos
                .Where(p => !string.IsNullOrEmpty(p.Folder))
                .Select(p => p.Folder)
                .Distinct()
                .OrderBy(f => f)
                .ToListAsync();

            _logger.Info("成功获取文件夹列表 - 文件夹数量: {Count}", folders.Count);

            return Ok(new ApiResponse<List<string>>
            {
                Success = true,
                Data = folders
            });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "获取文件夹列表失败");
            return StatusCode(500, new ApiResponse<List<string>>
            {
                Success = false,
                Error = new ErrorResponse
                {
                    Code = "SERVER_ERROR",
                    Message = "获取文件夹失败",
                    Details = ex.Message
                }
            });
        }
    }

    [HttpGet("folders/count")]
    public async Task<ActionResult<ApiResponse<int>>> GetFoldersCount()
    {
        try
        {
            _logger.Info("开始获取文件夹数量");
            var foldersCount = await _context.Photos
                .Where(p => !string.IsNullOrEmpty(p.Folder))
                .Select(p => p.Folder)
                .Distinct()
                .CountAsync();

            _logger.Info("成功获取文件夹数量 - 数量: {Count}", foldersCount);

            return Ok(new ApiResponse<int>
            {
                Success = true,
                Data = foldersCount
            });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "获取文件夹数量失败");
            return StatusCode(500, new ApiResponse<int>
            {
                Success = false,
                Error = new ErrorResponse
                {
                    Code = "SERVER_ERROR",
                    Message = "获取文件夹数量失败",
                    Details = ex.Message
                }
            });
        }
    }

    [HttpGet("locations")]
    public async Task<ActionResult<ApiResponse<List<string>>>> GetLocations()
    {
        try
        {
            _logger.Info("开始获取地点列表");
            var locations = await _context.Photos
                .Where(p => !string.IsNullOrEmpty(p.Location))
                .Select(p => p.Location)
                .Distinct()
                .OrderBy(l => l)
                .ToListAsync();

            _logger.Info("成功获取地点列表 - 地点数量: {Count}", locations.Count);

            return Ok(new ApiResponse<List<string>>
            {
                Success = true,
                Data = locations
            });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "获取文件夹列表失败");
            return StatusCode(500, new ApiResponse<List<string>>
            {
                Success = false,
                Error = new ErrorResponse
                {
                    Code = "SERVER_ERROR",
                    Message = "获取地点失败",
                    Details = ex.Message
                }
            });
        }
    }

    [HttpGet("locations/count")]
    public async Task<ActionResult<ApiResponse<int>>> GetLocationsCount()
    {
        try
        {
            _logger.Info("开始获取地点数量");
            var locationsCount = await _context.Photos
                .Where(p => !string.IsNullOrEmpty(p.Location))
                .Select(p => p.Location)
                .Distinct()
                .CountAsync();

            _logger.Info("成功获取地点数量 - 数量: {Count}", locationsCount);

            return Ok(new ApiResponse<int>
            {
                Success = true,
                Data = locationsCount
            });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "获取文件夹数量失败");
            return StatusCode(500, new ApiResponse<int>
            {
                Success = false,
                Error = new ErrorResponse
                {
                    Code = "SERVER_ERROR",
                    Message = "获取地点数量失败",
                    Details = ex.Message
                }
            });
        }
    }
}