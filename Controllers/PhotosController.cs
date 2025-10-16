using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TagPhotoAlbum.Server.Data;
using TagPhotoAlbum.Server.Models;

namespace TagPhotoAlbum.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PhotosController : ControllerBase
{
    private readonly AppDbContext _context;

    public PhotosController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<Photo>>>> GetPhotos(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        [FromQuery] string? folder = null,
        [FromQuery] string? location = null,
        [FromQuery] string? tags = null)
    {
        try
        {
            var query = _context.Photos.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(folder))
            {
                query = query.Where(p => p.Folder == folder);
            }

            if (!string.IsNullOrEmpty(location))
            {
                query = query.Where(p => p.Location == location);
            }

            if (!string.IsNullOrEmpty(tags))
            {
                var tagList = tags.Split(',', StringSplitOptions.RemoveEmptyEntries);
                query = query.Where(p => p.Tags.Any(t => tagList.Contains(t)));
            }

            var total = await query.CountAsync();
            var photos = await query
                .OrderByDescending(p => p.Date)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            return Ok(new ApiResponse<List<Photo>>
            {
                Success = true,
                Data = photos,
                Pagination = new PaginationInfo
                {
                    Page = page,
                    Limit = limit,
                    Total = total,
                    Pages = (int)Math.Ceiling(total / (double)limit)
                }
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
                    Message = "获取照片列表失败",
                    Details = ex.Message
                }
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<Photo>>> GetPhoto(int id)
    {
        try
        {
            var photo = await _context.Photos.FindAsync(id);

            if (photo == null)
            {
                return NotFound(new ApiResponse<Photo>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "NOT_FOUND",
                        Message = "照片不存在"
                    }
                });
            }

            return Ok(new ApiResponse<Photo>
            {
                Success = true,
                Data = photo
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<Photo>
            {
                Success = false,
                Error = new ErrorResponse
                {
                    Code = "SERVER_ERROR",
                    Message = "获取照片失败",
                    Details = ex.Message
                }
            });
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Photo>>> CreatePhoto([FromBody] Photo photo)
    {
        try
        {
            if (string.IsNullOrEmpty(photo.Url) || string.IsNullOrEmpty(photo.Title))
            {
                return BadRequest(new ApiResponse<Photo>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "VALIDATION_ERROR",
                        Message = "URL和标题是必填字段"
                    }
                });
            }

            _context.Photos.Add(photo);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPhoto), new { id = photo.Id }, new ApiResponse<Photo>
            {
                Success = true,
                Data = photo
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<Photo>
            {
                Success = false,
                Error = new ErrorResponse
                {
                    Code = "SERVER_ERROR",
                    Message = "创建照片失败",
                    Details = ex.Message
                }
            });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<Photo>>> UpdatePhoto(int id, [FromBody] Photo photoUpdate)
    {
        try
        {
            var photo = await _context.Photos.FindAsync(id);

            if (photo == null)
            {
                return NotFound(new ApiResponse<Photo>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "NOT_FOUND",
                        Message = "照片不存在"
                    }
                });
            }

            // Update fields
            photo.Title = photoUpdate.Title;
            photo.Description = photoUpdate.Description;
            photo.Tags = photoUpdate.Tags;
            photo.Folder = photoUpdate.Folder;
            photo.Location = photoUpdate.Location;

            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<Photo>
            {
                Success = true,
                Data = photo
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<Photo>
            {
                Success = false,
                Error = new ErrorResponse
                {
                    Code = "SERVER_ERROR",
                    Message = "更新照片失败",
                    Details = ex.Message
                }
            });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeletePhoto(int id)
    {
        try
        {
            var photo = await _context.Photos.FindAsync(id);

            if (photo == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "NOT_FOUND",
                        Message = "照片不存在"
                    }
                });
            }

            _context.Photos.Remove(photo);
            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = new { message = "照片删除成功" }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Error = new ErrorResponse
                {
                    Code = "SERVER_ERROR",
                    Message = "删除照片失败",
                    Details = ex.Message
                }
            });
        }
    }

    [HttpGet("recommend")]
    public async Task<ActionResult<ApiResponse<List<Photo>>>> GetRecommendedPhotos(
        [FromQuery] int count = 20)
    {
        try
        {
            // 使用预设的筛选条件：推荐艺术类照片
            // 先获取所有照片，然后在内存中进行筛选和随机选择
            var allPhotos = await _context.Photos.ToListAsync();

            // 筛选条件：文件夹为"艺术"，或者标签包含"艺术"或"抽象"
            var filteredPhotos = allPhotos
                .Where(p => p.Folder == "艺术" ||
                           (p.Tags != null && (p.Tags.Contains("艺术") || p.Tags.Contains("抽象"))))
                .Take(count)
                .ToList();

            var total = filteredPhotos.Count;

            if (total == 0)
            {
                // 如果没有符合条件的照片，返回空列表
                return Ok(new ApiResponse<List<Photo>>
                {
                    Success = true,
                    Data = new List<Photo>()
                });
            }

            // 随机选择指定数量的照片
            var photos = filteredPhotos
                .OrderBy(p => Guid.NewGuid())
                .ToList();

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
                    Message = "获取推荐照片失败",
                    Details = ex.Message
                }
            });
        }
    }
}