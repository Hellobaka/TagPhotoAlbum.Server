using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TagPhotoAlbum.Server.Data;
using TagPhotoAlbum.Server.Models;
using TagPhotoAlbum.Server.Services;

namespace TagPhotoAlbum.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PhotosController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly PhotoStorageService _photoStorageService;

    public PhotosController(AppDbContext context, PhotoStorageService photoStorageService)
    {
        _context = context;
        _photoStorageService = photoStorageService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<Photo>>>> GetPhotos(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        [FromQuery] string? folder = null,
        [FromQuery] string? location = null,
        [FromQuery] string? tags = null,
        [FromQuery] string? searchQuery = null)
    {
        try
        {
            var query = _context.Photos.AsQueryable();

            // 默认排除未分类照片
            if (string.IsNullOrEmpty(folder))
            {
                query = query.Where(p => p.Folder != "未分类");
            }

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

            // Apply search query filter
            if (!string.IsNullOrEmpty(searchQuery))
            {
                query = query.Where(p =>
                    p.Title.Contains(searchQuery) ||
                    p.Description.Contains(searchQuery) ||
                    p.Folder.Contains(searchQuery) ||
                    p.Location.Contains(searchQuery) ||
                    p.Tags.Any(t => t.Contains(searchQuery))
                );
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
            if (string.IsNullOrEmpty(photo.FilePath) || string.IsNullOrEmpty(photo.Title))
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

            // 如果文件夹发生变化，移动文件
            if (photo.Folder != photoUpdate.Folder)
            {
                var newFilePath = await _photoStorageService.MoveFileAsync(photo.FilePath, photoUpdate.Folder);
                photo.FilePath = newFilePath;
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

            // 删除物理文件
            _photoStorageService.DeleteFile(photo.FilePath);

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

    [HttpGet("paginated")]
    public async Task<ActionResult<ApiResponse<List<Photo>>>> GetPhotosPaginated(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        [FromQuery] string? tags = null,
        [FromQuery] string? folder = null,
        [FromQuery] string? location = null,
        [FromQuery] string? searchQuery = null)
    {
        try
        {
            var query = _context.Photos.AsQueryable();

            // 默认排除未分类照片
            if (string.IsNullOrEmpty(folder))
            {
                query = query.Where(p => p.Folder != "未分类");
            }

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

            // Apply search query filter
            if (!string.IsNullOrEmpty(searchQuery))
            {
                query = query.Where(p =>
                    p.Title.Contains(searchQuery) ||
                    p.Description.Contains(searchQuery) ||
                    p.Folder.Contains(searchQuery) ||
                    p.Location.Contains(searchQuery) ||
                    p.Tags.Any(t => t.Contains(searchQuery))
                );
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
                    Message = "获取分页照片列表失败",
                    Details = ex.Message
                }
            });
        }
    }

    [HttpPost("upload")]
    public async Task<ActionResult<ApiResponse<List<Photo>>>> UploadPhotos()
    {
        try
        {
            var files = Request.Form.Files;

            if (files == null || files.Count == 0)
            {
                return BadRequest(new ApiResponse<List<Photo>>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "VALIDATION_ERROR",
                        Message = "请选择要上传的文件"
                    }
                });
            }

            var uploadedPhotos = new List<Photo>();

            foreach (var file in files)
            {
                // 验证文件类型
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".svg" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    continue; // 跳过不支持的文件类型
                }

                // 使用外部存储保存文件
                var folder = "未分类";
                var filePath = await _photoStorageService.SaveFileAsync(file, folder);

                // 检查是否已存在同名照片记录，如果存在则更新
                var existingPhoto = await _context.Photos
                    .FirstOrDefaultAsync(p => p.FilePath == filePath);

                if (existingPhoto != null)
                {
                    // 更新现有照片记录
                    existingPhoto.Title = Path.GetFileNameWithoutExtension(file.FileName);
                    existingPhoto.Date = DateTime.UtcNow;
                    uploadedPhotos.Add(existingPhoto);
                }
                else
                {
                    // 创建新照片记录
                    var photo = new Photo
                    {
                        FilePath = filePath,
                        Title = Path.GetFileNameWithoutExtension(file.FileName),
                        Description = string.Empty,
                        Tags = new List<string>(),
                        Folder = folder,
                        Location = string.Empty,
                        Date = DateTime.UtcNow
                    };

                    _context.Photos.Add(photo);
                    uploadedPhotos.Add(photo);
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<List<Photo>>
            {
                Success = true,
                Data = uploadedPhotos,
                Message = $"成功上传 {uploadedPhotos.Count} 张图片"
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
                    Message = "上传图片失败",
                    Details = ex.Message
                }
            });
        }
    }

    [HttpGet("uncategorized")]
    public async Task<ActionResult<ApiResponse<List<Photo>>>> GetUncategorizedPhotos()
    {
        try
        {
            // 先获取所有照片，然后在客户端进行筛选
            var allPhotos = await _context.Photos.ToListAsync();

            // 获取未分类的照片：文件夹为"未分类"的
            var uncategorizedPhotos = allPhotos
                .Where(p => p.Folder == "未分类")
                .OrderByDescending(p => p.Date)
                .ToList();

            return Ok(new ApiResponse<List<Photo>>
            {
                Success = true,
                Data = uncategorizedPhotos
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
                    Message = "获取未分类照片失败",
                    Details = ex.Message
                }
            });
        }
    }
}