# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

TagPhotoAlbum.Server is an ASP.NET Core 9.0 backend API for a photo management application. It provides photo storage, tagging, categorization, and search functionality with JWT-based authentication.

## Development Commands

### Build and Run
- `dotnet build` - Build the project
- `dotnet run` - Run the development server (defaults to https://localhost:7000 and http://localhost:5000)
- `dotnet watch` - Run with hot reload for development

### Database
- Uses SQLite with Entity Framework Core
- Database file: `tagphotoalbum.db`
- Database is automatically created and seeded on first run via `SeedData.Initialize()`

## Architecture

### Project Structure
- **Controllers/**: API endpoints organized by resource type
  - `PhotosController.cs`: Photo CRUD operations, upload, uncategorized photos, recommendations
  - `AuthController.cs`: User authentication (login)
  - `SearchController.cs`: Photo search functionality
  - `MetadataController.cs`: Tags, folders, and locations metadata
- **Models/**: Data models and API response types
  - `Photo.cs`: Photo entity with URL, title, tags, folder, location, date
  - `User.cs`: User entity for authentication
  - `ApiResponse.cs`: Standard API response wrapper with success flag, data, error, pagination, and message
- **Data/**: Database context and seed data
  - `AppDbContext.cs`: Entity Framework context with SQLite configuration
  - `SeedData.cs`: Initial sample photos and user data
- **Services/**: Business logic services
  - `AuthService.cs`: Authentication and JWT token generation
  - `PhotoStorageService.cs`: External file storage management with multiple path support

### API Design
- **Base URL**: `/api`
- **Authentication**: JWT Bearer tokens required for all endpoints except `/api/auth/login`
- **Response Format**: Standardized `ApiResponse<T>` wrapper with success flag, data, error details, and optional pagination
- **Error Handling**: Consistent error responses with error codes and messages

### Key Features
- **Photo Management**: Full CRUD operations with filtering by folder, location, and tags
- **File Upload**: Multi-file upload with external storage system
  - Files stored in multiple configurable external directories (default: `D:\Photos\Album`, `D:\Photos\Archive`)
  - Preserves original filenames
  - Automatically overwrites existing files with same name
  - Updates existing photo records when re-uploading same file
  - Files organized by folder structure
- **External Storage**: Configurable external file storage with static file serving
  - URL format: `/external/{folder}/{filename}`
  - Automatic file movement when folder is changed
  - Automatic cleanup of empty directories
  - Support for multiple storage paths with automatic path detection
- **Search**: Full-text search across title, description, tags, folder, and location
- **Metadata**: Dynamic extraction of tags, folders, and locations from existing photos
- **Uncategorized Photos**: Photos in "未分类" folder (folder-based classification)
- **Recommendations**: Art-focused photo recommendations with random selection

### Security
- JWT authentication with configurable issuer, audience, and signing key
- CORS configured to allow any origin for development
- All controllers except Auth require `[Authorize]` attribute

### Data Storage
- Photos stored with relative URLs pointing to files in `wwwroot/uploads/`
- Tags stored as JSON-serialized string arrays in SQLite
- Automatic database creation and seeding on application startup

## Important Notes

- **External Storage**: Files are stored in multiple configured external directories (default: `D:\Photos\Album`, `D:\Photos\Archive`)
- **Database Storage**: Photos store absolute file paths in `FilePath` field (replaced URL field)
- **File Organization**: Files are automatically organized by folder structure
- **File Movement**: When folder is changed, files are automatically moved to the new folder directory
- **Static File Serving**: External storage is served via `/external` URL path
- **URL Generation**: URLs are dynamically generated from file paths using `PhotoStorageService.GetFileUrl()`
- **Default Photo List**: (`GET /api/photos`) excludes photos in "未分类" folder unless explicitly filtered by folder
- **Uncategorized Photos**: Defined as photos in "未分类" folder
- **File Cleanup**: Empty directories are automatically cleaned up when files are moved or deleted
- **Configuration**: External storage paths are configurable via `appsettings.json` as an array
- **Data Migration**: Existing data can be discarded as test data is not important
- **Authentication**: Uses simple password comparison (consider implementing proper password hashing for production)