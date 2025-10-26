# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

TagPhotoAlbum.Server is an ASP.NET Core 9.0 backend API for a photo management application. It provides photo storage, tagging, categorization, and search functionality with JWT-based authentication.

## Development Commands

### Build and Run
- `dotnet build` - Build the project
- `dotnet run` - Run the development server (defaults to http://localhost:5085 and https://localhost:7088)
- `dotnet watch` - Run with hot reload for development

### Database
- Uses SQLite with Entity Framework Core
- Database file: `tagphotoalbum.db`
- Database is automatically created and seeded on first run via `SeedData.Initialize()`

## Architecture

### Project Structure
- **Controllers/**: API endpoints organized by resource type
  - `PhotosController.cs`: Photo CRUD operations, upload, uncategorized photos, recommendations
  - `AuthController.cs`: User authentication (login, secure login, passkey)
  - `SearchController.cs`: Photo search functionality
  - `MetadataController.cs`: Tags, folders, and locations metadata
  - `PasskeyController.cs`: WebAuthn passkey authentication
- **Models/**: Data models and API response types
  - `Photo.cs`: Photo entity with file path, title, tags, folder, location, date
  - `User.cs`: User entity for authentication
  - `ApiResponse.cs`: Standard API response wrapper with success flag, data, error, pagination, and message
  - `Passkey.cs`: WebAuthn passkey storage
- **Data/**: Database context and seed data
  - `AppDbContext.cs`: Entity Framework context with SQLite configuration
  - `SeedData.cs`: Initial sample photos and user data
- **Services/**: Business logic services
  - `AuthService.cs`: Authentication and JWT token generation
  - `PhotoStorageService.cs`: External file storage management with multiple path support
  - `ImageCompressionService.cs`: Image compression with configurable quality
  - `ExifService.cs`: EXIF metadata extraction
  - `PasskeyService.cs`: WebAuthn passkey management

### API Design
- **Base URL**: `/api`
- **Authentication**: JWT Bearer tokens required for all endpoints except `/api/auth/login` and `/api/auth/secure-login`
- **Response Format**: Standardized `ApiResponse<T>` wrapper with success flag, data, error details, and optional pagination
- **Error Handling**: Consistent error responses with error codes and messages

### Key Features
- **Photo Management**: Full CRUD operations with filtering by folder, location, and tags
- **File Upload**: Multi-file upload with external storage system
  - Files stored in multiple configurable external directories (default: `E:\图`)
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
- **Image Compression**: Automatic image compression with configurable quality
- **EXIF Extraction**: Automatic extraction of EXIF metadata from uploaded images
- **Passkey Authentication**: WebAuthn support for passwordless authentication

### Security
- JWT authentication with configurable issuer, audience, and signing key
- Secure login with HMAC-SHA256 signature verification
- WebAuthn passkey authentication support
- CORS configured to allow any origin for development
- All controllers except Auth require `[Authorize]` attribute

### Data Storage
- Photos store absolute file paths in `FilePath` field
- Tags stored using many-to-many relationship with `PhotoTag` join table
- Automatic database creation and seeding on application startup
- External file storage with configurable paths

### Configuration
Key configuration in `appsettings.json`:
- **Server URLs**: `Server:Urls` - Comma-separated list of URLs to listen on (e.g., `"http://localhost:5085;https://localhost:7088"`)
- **HTTPS Certificate**: `Server:Certificate` - Certificate configuration using modern .NET 9 X509CertificateLoader
  - `Path` - Path to certificate file (.pem or .pfx)
  - `KeyPath` - Path to private key file (.pem) - required for separate PEM files
  - `Password` - Certificate password (for PFX files)
- **External Storage Paths**: `PhotoStorage:ExternalStoragePaths` array
- **Image Compression**: `ImageCompression:Quality`, `EnableCompress`
- **JWT Settings**: `Jwt:Key`, `Issuer`, `Audience`, `ExpireMinutes`
- **Passkey Settings**: `Passkey:RelyingParty` configuration
- **Recommend Tags**: `RecommendTags` array for recommendation filtering

### Environment-specific Configuration
- `appsettings.Development.json` - Development environment settings
- `appsettings.Production.json` - Production environment settings
- Environment-specific settings override base `appsettings.json`

## Important Notes

- **External Storage**: Files are stored in multiple configured external directories (default: `E:\图`)
- **Database Storage**: Photos store absolute file paths in `FilePath` field
- **File Organization**: Files are automatically organized by folder structure
- **File Movement**: When folder is changed, files are automatically moved to the new folder directory
- **Static File Serving**: External storage is served via `/external` URL path
- **URL Generation**: URLs are dynamically generated from file paths using `PhotoStorageService.GetFileUrl()`
- **Default Photo List**: (`GET /api/photos`) excludes photos in "未分类" folder unless explicitly filtered by folder
- **Uncategorized Photos**: Defined as photos in "未分类" folder
- **File Cleanup**: Empty directories are automatically cleaned up when files are moved or deleted
- **Configuration**: External storage paths are configurable via `appsettings.json` as an array
- **Data Migration**: Existing data can be discarded as test data is not important
- **Authentication**: Supports traditional login, secure login with HMAC signatures, and WebAuthn passkeys
- **Image Compression**: Automatic compression with configurable quality (default: 60%)
- **EXIF Data**: Automatically extracted from uploaded images and stored in database
- **Logging**: Uses NLog for comprehensive logging throughout the application

## Development Workflow

1. **Database Setup**: Database is automatically created and seeded on first run
2. **External Storage**: Configure storage paths in `appsettings.json` before first run
3. **Authentication**: Use `/api/auth/login` for traditional login or `/api/auth/secure-login` for enhanced security
4. **File Upload**: Upload files via `/api/photos/upload` endpoint with multipart/form-data
5. **Photo Management**: Use standard CRUD endpoints for photo operations with filtering and pagination

## API Endpoints

### Authentication
- `POST /api/auth/login` - Traditional username/password login
- `POST /api/auth/secure-login` - Secure login with HMAC signature
- `GET /api/auth/nonce-seed` - Get nonce seed for secure login
- `GET /api/auth/validate-token` - Validate JWT token

### Photos
- `GET /api/photos` - Get photos with pagination and filtering
- `GET /api/photos/{id}` - Get single photo
- `POST /api/photos` - Create photo
- `PUT /api/photos/{id}` - Update photo
- `DELETE /api/photos/{id}` - Delete photo
- `POST /api/photos/upload` - Upload photos
- `GET /api/photos/recommend` - Get recommended photos
- `GET /api/photos/uncategorized` - Get uncategorized photos

### Metadata
- `GET /api/metadata/tags` - Get all tags with usage counts
- `GET /api/metadata/folders` - Get all folders
- `GET /api/metadata/locations` - Get all locations

### Search
- `GET /api/search` - Search photos by query

### Passkeys
- `POST /api/passkey/register` - Register passkey
- `POST /api/passkey/authenticate` - Authenticate with passkey
- `GET /api/passkey` - Get user passkeys
- `DELETE /api/passkey/{id}` - Delete passkey