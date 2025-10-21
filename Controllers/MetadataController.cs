using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TagPhotoAlbum.Server.Data;
using TagPhotoAlbum.Server.Models;

namespace TagPhotoAlbum.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MetadataController : ControllerBase
{
    private readonly AppDbContext _context;

    public MetadataController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("tags")]
    public async Task<ActionResult<ApiResponse<List<string>>>> GetTags()
    {
        try
        {
            var photos = await _context.Photos.ToListAsync();
            var tags = photos
                .SelectMany(p => p.Tags)
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            return Ok(new ApiResponse<List<string>>
            {
                Success = true,
                Data = tags.Select(x => x.Tag.Name).ToList()
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<List<string>>
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

    [HttpGet("tags/count")]
    public async Task<ActionResult<ApiResponse<int>>> GetTagsCount()
    {
        try
        {
            var photos = await _context.Photos.ToListAsync();
            var tagsCount = photos
                .SelectMany(p => p.Tags)
                .Distinct()
                .Count();

            return Ok(new ApiResponse<int>
            {
                Success = true,
                Data = tagsCount
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<int>
            {
                Success = false,
                Error = new ErrorResponse
                {
                    Code = "SERVER_ERROR",
                    Message = "获取标签数量失败",
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
            var folders = await _context.Photos
                .Where(p => !string.IsNullOrEmpty(p.Folder))
                .Select(p => p.Folder)
                .Distinct()
                .OrderBy(f => f)
                .ToListAsync();

            return Ok(new ApiResponse<List<string>>
            {
                Success = true,
                Data = folders
            });
        }
        catch (Exception ex)
        {
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
            var foldersCount = await _context.Photos
                .Where(p => !string.IsNullOrEmpty(p.Folder))
                .Select(p => p.Folder)
                .Distinct()
                .CountAsync();

            return Ok(new ApiResponse<int>
            {
                Success = true,
                Data = foldersCount
            });
        }
        catch (Exception ex)
        {
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
            var locations = await _context.Photos
                .Where(p => !string.IsNullOrEmpty(p.Location))
                .Select(p => p.Location)
                .Distinct()
                .OrderBy(l => l)
                .ToListAsync();

            return Ok(new ApiResponse<List<string>>
            {
                Success = true,
                Data = locations
            });
        }
        catch (Exception ex)
        {
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
            var locationsCount = await _context.Photos
                .Where(p => !string.IsNullOrEmpty(p.Location))
                .Select(p => p.Location)
                .Distinct()
                .CountAsync();

            return Ok(new ApiResponse<int>
            {
                Success = true,
                Data = locationsCount
            });
        }
        catch (Exception ex)
        {
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