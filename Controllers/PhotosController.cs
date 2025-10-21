using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
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
    private readonly ImageCompressionService _imageCompressionService;
    private readonly ExifService _exifService;

    public PhotosController(AppDbContext context, PhotoStorageService photoStorageService, ImageCompressionService imageCompressionService, ExifService exifService)
    {
        _context = context;
        _photoStorageService = photoStorageService;
        _imageCompressionService = imageCompressionService;
        _exifService = exifService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<PhotoResponse>>>> GetPhotos(
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
                query = query.Where(p => p.Tags.Any(t => tagList.Contains(t.Tag.Name)));
            }

            // Apply search query filter
            if (!string.IsNullOrEmpty(searchQuery))
            {
                query = query.Where(p =>
                    p.Title.Contains(searchQuery) ||
                    p.Description.Contains(searchQuery) ||
                    p.Folder.Contains(searchQuery) ||
                    p.Location.Contains(searchQuery) ||
                    p.Tags.Any(t => t.Tag.Name.Contains(searchQuery))
                );
            }

            var total = await query.CountAsync();
            var photos = await query
                .Include(p => p.Tags).ThenInclude(pt => pt.Tag)
                .OrderByDescending(p => p.Date)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            // 将FilePath转换为URL并转换为PhotoResponse
            var photosWithUrls = photos.Select(p => new PhotoResponse
            {
                Id = p.Id,
                FilePath = _photoStorageService.GetFileUrl(p.FilePath),
                Title = p.Title,
                Description = p.Description,
                Tags = p.Tags.Select(t => t.Tag.Name).ToList(),
                Folder = p.Folder,
                Location = p.Location,
                Date = p.Date,
                FileSizeKB = p.FileSizeKB,
                ExifData = p.ExifData,
                CompressedFilePath = _imageCompressionService.GetCompressedFileUrl(p.FilePath),
                HasCompressedImage = _imageCompressionService.CompressedFileExists(p.FilePath)
            }).ToList();

            return Ok(new ApiResponse<List<PhotoResponse>>
            {
                Success = true,
                Data = photosWithUrls,
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
            return StatusCode(500, new ApiResponse<List<PhotoResponse>>
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
    public async Task<ActionResult<ApiResponse<PhotoResponse>>> GetPhoto(int id)
    {
        try
        {
            var photo = await _context.Photos.FindAsync(id);

            if (photo == null)
            {
                return NotFound(new ApiResponse<PhotoResponse>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "NOT_FOUND",
                        Message = "照片不存在"
                    }
                });
            }

            // 将FilePath转换为URL并转换为PhotoResponse
            var photoWithUrl = new PhotoResponse
            {
                Id = photo.Id,
                FilePath = _photoStorageService.GetFileUrl(photo.FilePath),
                Title = photo.Title,
                Description = photo.Description,
                Tags = photo.Tags.Select(t => t.Tag.Name).ToList(),
                Folder = photo.Folder,
                Location = photo.Location,
                Date = photo.Date,
                FileSizeKB = photo.FileSizeKB,
                ExifData = photo.ExifData,
                CompressedFilePath = _imageCompressionService.GetCompressedFileUrl(photo.FilePath),
                HasCompressedImage = _imageCompressionService.CompressedFileExists(photo.FilePath)
            };

            return Ok(new ApiResponse<PhotoResponse>
            {
                Success = true,
                Data = photoWithUrl
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<PhotoResponse>
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
    public async Task<ActionResult<ApiResponse<PhotoResponse>>> CreatePhoto([FromBody] PhotoCreateRequest photoCreate)
    {
        try
        {
            if (string.IsNullOrEmpty(photoCreate.FilePath) || string.IsNullOrEmpty(photoCreate.Title))
            {
                return BadRequest(new ApiResponse<PhotoResponse>
                {
                    Success = false,
                    Error = new ErrorResponse
                    {
                        Code = "VALIDATION_ERROR",
                        Message = "URL和标题是必填字段"
                    }
                });
            }

            var photo = new Photo
            {
                FilePath = photoCreate.FilePath,
                Title = photoCreate.Title,
                Description = photoCreate.Description,
                Folder = photoCreate.Folder,
                Location = photoCreate.Location,
                Date = DateTime.UtcNow
            };

            // 处理标签
            if (photoCreate.Tags != null)
            {
                foreach (var tagName in photoCreate.Tags)
                {
                    var existingTag = await _context.Tags.FirstOrDefaultAsync(t => t.Name == tagName);
                    if (existingTag == null)
                    {
                        existingTag = new Tag { Name = tagName };
                        _context.Tags.Add(existingTag);
                    }

                    photo.Tags.Add(new PhotoTag { Photo = photo, Tag = existingTag });
                }
            }

            _context.Photos.Add(photo);
            await _context.SaveChangesAsync();

            // 将FilePath转换为URL并转换为PhotoResponse
            var photoWithUrl = new PhotoResponse
            {
                Id = photo.Id,
                FilePath = _photoStorageService.GetFileUrl(photo.FilePath),
                Title = photo.Title,
                Description = photo.Description,
                Tags = photo.Tags.Select(t => t.Tag.Name).ToList(),
                Folder = photo.Folder,
                Location = photo.Location,
                Date = photo.Date,
                FileSizeKB = photo.FileSizeKB,
                ExifData = photo.ExifData,
                CompressedFilePath = _imageCompressionService.GetCompressedFileUrl(photo.FilePath),
                HasCompressedImage = _imageCompressionService.CompressedFileExists(photo.FilePath)
            };

            return CreatedAtAction(nameof(GetPhoto), new { id = photo.Id }, new ApiResponse<PhotoResponse>
            {
                Success = true,
                Data = photoWithUrl
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<PhotoResponse>
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
    public async Task<ActionResult<ApiResponse<PhotoResponse>>> UpdatePhoto(int id, [FromBody] PhotoUpdateRequest photoUpdate)
    {
        try
        {
            var photo = await _context.Photos.FindAsync(id);

            if (photo == null)
            {
                return NotFound(new ApiResponse<PhotoResponse>
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
                //var newFilePath = await _photoStorageService.MoveFileAsync(photo.FilePath, photoUpdate.Folder);
                //photo.FilePath = newFilePath;
            }

            // Update fields
            photo.Title = photoUpdate.Title;
            photo.Description = photoUpdate.Description;
            photo.Folder = photoUpdate.Folder;
            photo.Location = photoUpdate.Location;

            // 处理标签更新
            if (photoUpdate.Tags != null)
            {
                // 清除现有标签
                var existingPhotoTags = _context.PhotoTags.Where(pt => pt.PhotoId == photo.Id);
                _context.PhotoTags.RemoveRange(existingPhotoTags);

                // 先保存删除操作，确保PhotoTag表清理干净
                await _context.SaveChangesAsync();

                // 添加新标签
                foreach (var tagName in photoUpdate.Tags)
                {
                    var existingTag = await _context.Tags.FirstOrDefaultAsync(t => t.Name == tagName);
                    if (existingTag == null)
                    {
                        existingTag = new Tag { Name = tagName };
                        _context.Tags.Add(existingTag);
                        // 先保存新标签以获取ID
                        await _context.SaveChangesAsync();
                    }

                    var photoTag = new PhotoTag { PhotoId = photo.Id, TagId = existingTag.Id };
                    _context.PhotoTags.Add(photoTag);
                }
            }

            await _context.SaveChangesAsync();

            // 将FilePath转换为URL并转换为PhotoResponse
            var photoWithUrl = new PhotoResponse
            {
                Id = photo.Id,
                FilePath = _photoStorageService.GetFileUrl(photo.FilePath),
                Title = photo.Title,
                Description = photo.Description,
                Tags = photo.Tags.Select(t => t.Tag.Name).ToList(),
                Folder = photo.Folder,
                Location = photo.Location,
                Date = photo.Date,
                FileSizeKB = photo.FileSizeKB,
                ExifData = photo.ExifData,
                CompressedFilePath = _imageCompressionService.GetCompressedFileUrl(photo.FilePath),
                HasCompressedImage = _imageCompressionService.CompressedFileExists(photo.FilePath)
            };

            return Ok(new ApiResponse<PhotoResponse>
            {
                Success = true,
                Data = photoWithUrl
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<PhotoResponse>
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
            // _photoStorageService.DeleteFile(photo.FilePath);

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
    public async Task<ActionResult<ApiResponse<List<PhotoResponse>>>> GetRecommendedPhotos(
        [FromQuery] int limit = 20,
        [FromQuery] string? excludeIds = null)
    {
        try
        {
            // 使用预设的筛选条件
            var query = _context.Photos
                .Where(p => p.Folder == "艺术" || p.Tags.Any(o => o.Tag.Name == "艺术") || p.Tags.Any(o => o.Tag.Name == "抽象"));

            // 排除已显示的照片ID
            if (!string.IsNullOrEmpty(excludeIds))
            {
                var excludedIds = excludeIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(id => int.TryParse(id, out var parsedId) ? parsedId : -1)
                    .Where(id => id > 0)
                    .ToList();

                if (excludedIds.Count != 0)
                {
                    query = query.Where(p => !excludedIds.Any(o => o == p.Id));
                }
            }

            var total = await query.CountAsync();

            if (total == 0)
            {
                // 如果没有符合条件的照片，返回空列表
                return Ok(new ApiResponse<List<PhotoResponse>>
                {
                    Success = true,
                    Data = []
                });
            }

            // 随机选择照片
            var photos = await query
                .Include(p => p.Tags).ThenInclude(pt => pt.Tag)
                .OrderBy(p => Guid.NewGuid())
                .Take(limit)
                .ToListAsync();

            // 将FilePath转换为URL并转换为PhotoResponse
            var photosWithUrls = photos.Select(p => new PhotoResponse
            {
                Id = p.Id,
                FilePath = _photoStorageService.GetFileUrl(p.FilePath),
                Title = p.Title,
                Description = p.Description,
                Tags = p.Tags.Select(t => t.Tag.Name).ToList(),
                Folder = p.Folder,
                Location = p.Location,
                Date = p.Date,
                FileSizeKB = p.FileSizeKB,
                ExifData = p.ExifData,
                CompressedFilePath = _imageCompressionService.GetCompressedFileUrl(p.FilePath),
                HasCompressedImage = _imageCompressionService.CompressedFileExists(p.FilePath)
            }).ToList();

            return Ok(new ApiResponse<List<PhotoResponse>>
            {
                Success = true,
                Data = photosWithUrls
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<List<PhotoResponse>>
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
    public async Task<ActionResult<ApiResponse<List<PhotoResponse>>>> GetPhotosPaginated(
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
                query = query.Where(p => p.Tags.Any(t => tagList.Contains(t.Tag.Name)));
            }

            // Apply search query filter
            if (!string.IsNullOrEmpty(searchQuery))
            {
                query = query.Where(p =>
                    p.Title.Contains(searchQuery) ||
                    p.Description.Contains(searchQuery) ||
                    p.Folder.Contains(searchQuery) ||
                    p.Location.Contains(searchQuery) ||
                    p.Tags.Any(t => t.Tag.Name.Contains(searchQuery))
                );
            }

            var total = await query.CountAsync();
            var photos = await query
                .Include(p => p.Tags).ThenInclude(pt => pt.Tag)
                .OrderByDescending(p => p.Date)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            // 将FilePath转换为URL并转换为PhotoResponse
            var photosWithUrls = photos.Select(p => new PhotoResponse
            {
                Id = p.Id,
                FilePath = _photoStorageService.GetFileUrl(p.FilePath),
                Title = p.Title,
                Description = p.Description,
                Tags = p.Tags.Select(t => t.Tag.Name).ToList(),
                Folder = p.Folder,
                Location = p.Location,
                Date = p.Date,
                FileSizeKB = p.FileSizeKB,
                ExifData = p.ExifData,
                CompressedFilePath = _imageCompressionService.GetCompressedFileUrl(p.FilePath),
                HasCompressedImage = _imageCompressionService.CompressedFileExists(p.FilePath)
            }).ToList();

            return Ok(new ApiResponse<List<PhotoResponse>>
            {
                Success = true,
                Data = photosWithUrls,
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
            return StatusCode(500, new ApiResponse<List<PhotoResponse>>
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
    public async Task<ActionResult<ApiResponse<List<PhotoResponse>>>> UploadPhotos()
    {
        try
        {
            var files = Request.Form.Files;

            if (files == null || files.Count == 0)
            {
                return BadRequest(new ApiResponse<List<PhotoResponse>>
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
                var compressedPath = await _imageCompressionService.CompressImageAsync(filePath);

                // 获取文件大小（单位：KB）
                var fileInfo = new FileInfo(filePath);
                var fileSizeKB = fileInfo.Exists ? Math.Round(fileInfo.Length / 1024.0, 2) : 0;

                // 提取EXIF信息
                var exifData = _exifService.ExtractExifData(filePath);

                // 检查是否已存在同名照片记录，如果存在则更新
                var existingPhoto = await _context.Photos
                    .FirstOrDefaultAsync(p => p.FilePath == filePath);

                if (existingPhoto != null)
                {
                    // 更新现有照片记录
                    existingPhoto.Title = Path.GetFileNameWithoutExtension(file.FileName);
                    existingPhoto.Date = DateTime.UtcNow;
                    existingPhoto.FileSizeKB = fileSizeKB;
                    existingPhoto.ExifData = exifData;
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
                        Tags = [],
                        Folder = folder,
                        Location = string.Empty,
                        Date = DateTime.UtcNow,
                        FileSizeKB = fileSizeKB,
                        ExifData = exifData
                    };

                    _context.Photos.Add(photo);
                    uploadedPhotos.Add(photo);
                }
            }

            await _context.SaveChangesAsync();

            // 将FilePath转换为URL并转换为PhotoResponse
            var uploadedPhotosWithUrls = uploadedPhotos.Select(p => new PhotoResponse
            {
                Id = p.Id,
                FilePath = _photoStorageService.GetFileUrl(p.FilePath),
                Title = p.Title,
                Description = p.Description,
                Tags = p.Tags.Select(t => t.Tag.Name).ToList(),
                Folder = p.Folder,
                Location = p.Location,
                Date = p.Date,
                FileSizeKB = p.FileSizeKB,
                CompressedFilePath = _imageCompressionService.GetCompressedFileUrl(p.FilePath),
                HasCompressedImage = _imageCompressionService.CompressedFileExists(p.FilePath)
            }).ToList();

            return Ok(new ApiResponse<List<PhotoResponse>>
            {
                Success = true,
                Data = uploadedPhotosWithUrls,
                Message = $"成功上传 {uploadedPhotos.Count} 张图片"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<List<PhotoResponse>>
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
    public async Task<ActionResult<ApiResponse<List<PhotoResponse>>>> GetUncategorizedPhotos()
    {
        try
        {
            var allPhotos = _context.Photos.Where(p => p.Folder == "未分类")
                .OrderByDescending(p => p.Date);

            // 获取未分类的照片：文件夹为"未分类"的
            var uncategorizedPhotos = await allPhotos
                .ToListAsync();

            // 将FilePath转换为URL并转换为PhotoResponse
            var uncategorizedPhotosWithUrls = uncategorizedPhotos.Select(p => new PhotoResponse
            {
                Id = p.Id,
                FilePath = _photoStorageService.GetFileUrl(p.FilePath),
                Title = p.Title,
                Description = p.Description,
                Tags = p.Tags.Select(t => t.Tag.Name).ToList(),
                Folder = p.Folder,
                Location = p.Location,
                Date = p.Date,
                CompressedFilePath = _imageCompressionService.GetCompressedFileUrl(p.FilePath),
                HasCompressedImage = _imageCompressionService.CompressedFileExists(p.FilePath),
                FileSizeKB = p.FileSizeKB
            }).ToList();

            return Ok(new ApiResponse<List<PhotoResponse>>
            {
                Success = true,
                Data = uncategorizedPhotosWithUrls
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<List<PhotoResponse>>
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