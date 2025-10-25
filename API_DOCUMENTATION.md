# Tag Photo Album API 文档

## 概述

本文档描述了 Tag Photo Album 应用的完整 API 接口规范。后端使用 ASP.NET Core 9.0，支持照片管理、标签分类、文件夹组织和地点标注等功能。

## 基础信息

- **基础 URL**: `http://localhost:5085/api` 或 `https://localhost:7088/api`
- **认证方式**: JWT Bearer Token
- **数据格式**: JSON
- **响应格式**: 标准化的 `ApiResponse<T>` 包装器

## 认证接口

### 安全用户登录（推荐使用）

**POST** `/api/auth/login`

**安全特性：**
- HMAC-SHA256 签名验证
- 时间戳防重放攻击（5分钟有效期）
- Nonce 防重放攻击
- 前端密码哈希

请求体：
```json
{
  "username": "string",
  "passwordHash": "string",
  "timestamp": "number",
  "nonce": "string",
  "signature": "string"
}
```

响应：
```json
{
  "success": true,
  "data": {
    "user": {
      "username": "string",
      "name": "string",
      "email": "string"
    },
    "token": "jwt_token_string",
    "serverTimestamp": "number",
    "nextNonceSeed": "string"
  }
}
```

**前端实现步骤：**
1. 获取当前时间戳和生成随机nonce
2. 使用bcrypt对密码进行哈希
3. 构建签名载荷：`username:passwordHash:timestamp:nonce`
4. 使用HMAC-SHA256计算签名
5. 发送包含所有字段的请求

### 获取Nonce种子

**GET** `/api/auth/nonce-seed`

响应：
```json
{
  "success": true,
  "data": "random_nonce_seed_string"
}
```

## 照片管理接口

### 获取照片列表（分页）

**GET** `/api/photos`

查询参数：
- `page` (可选): 页码，默认 1
- `limit` (可选): 每页数量，默认 20
- `folder` (可选): 按文件夹筛选
- `location` (可选): 按地点筛选
- `tags` (可选): 按标签筛选，多个标签用逗号分隔
- `searchQuery` (可选): 搜索关键词
- `sortBy` (可选): 排序字段，支持 `filename`、`size`、`date`（默认）
- `sortOrder` (可选): 排序顺序，`asc` 或 `desc`（默认）

响应：
```json
{
  "success": true,
  "data": [
    {
      "id": "number",
      "filePath": "string",
      "title": "string",
      "description": "string",
      "tags": ["string"],
      "folder": "string",
      "location": "string",
      "date": "string",
      "fileSizeKB": "number",
      "exifData": "object",
      "compressedFilePath": "string",
      "hasCompressedImage": "boolean"
    }
  ],
  "pagination": {
    "page": 1,
    "limit": 20,
    "total": 100,
    "pages": 5
  }
}
```

**说明：**
- 默认排除文件夹为"未分类"的照片，除非明确指定 `folder=未分类`
- 标签筛选支持多标签同时筛选
- 支持按文件名、文件大小、日期排序

### 获取单个照片

**GET** `/api/photos/{id}`

响应：
```json
{
  "success": true,
  "data": {
    "id": "number",
    "filePath": "string",
    "title": "string",
    "description": "string",
    "tags": ["string"],
    "folder": "string",
    "location": "string",
    "date": "string",
    "fileSizeKB": "number",
    "exifData": "object",
    "compressedFilePath": "string",
    "hasCompressedImage": "boolean"
  }
}
```

### 创建照片

**POST** `/api/photos`

请求体：
```json
{
  "filePath": "string",
  "title": "string",
  "description": "string",
  "tags": ["string"],
  "folder": "string",
  "location": "string"
}
```

响应：
```json
{
  "success": true,
  "data": {
    "id": "number",
    "filePath": "string",
    "title": "string",
    "description": "string",
    "tags": ["string"],
    "folder": "string",
    "location": "string",
    "date": "string",
    "fileSizeKB": "number",
    "exifData": "object",
    "compressedFilePath": "string",
    "hasCompressedImage": "boolean"
  }
}
```

### 更新照片

**PUT** `/api/photos/{id}`

请求体：
```json
{
  "title": "string",
  "description": "string",
  "tags": ["string"],
  "folder": "string",
  "location": "string"
}
```

响应：
```json
{
  "success": true,
  "data": {
    "id": "number",
    "filePath": "string",
    "title": "string",
    "description": "string",
    "tags": ["string"],
    "folder": "string",
    "location": "string",
    "date": "string",
    "fileSizeKB": "number",
    "exifData": "object",
    "compressedFilePath": "string",
    "hasCompressedImage": "boolean"
  }
}
```

### 删除照片

**DELETE** `/api/photos/{id}`

响应：
```json
{
  "success": true,
  "data": {
    "message": "照片删除成功"
  }
}
```

### 上传图片

**POST** `/api/photos/upload`

请求头：
- `Content-Type: multipart/form-data`

请求体：
- `files` (必需): 图片文件数组

支持的文件类型：
- `.jpg`, `.jpeg`, `.png`, `.gif`, `.bmp`, `.webp`, `.svg`

响应：
```json
{
  "success": true,
  "data": [
    {
      "id": "number",
      "filePath": "string",
      "title": "string",
      "description": "string",
      "tags": ["string"],
      "folder": "string",
      "location": "string",
      "date": "string",
      "fileSizeKB": "number",
      "exifData": "object",
      "compressedFilePath": "string",
      "hasCompressedImage": "boolean"
    }
  ],
  "message": "成功上传 5 张图片"
}
```

**说明：**
- 上传的文件自动保存到外部存储
- 自动提取EXIF元数据
- 自动生成压缩版本（如果启用压缩）
- 如果文件已存在，会更新现有记录

### 获取推荐照片

**GET** `/api/photos/recommend`

查询参数：
- `limit` (可选): 每页数量，默认 20
- `excludeIds` (可选): 排除的照片ID列表，用逗号分隔

响应：
```json
{
  "success": true,
  "data": [
    {
      "id": "number",
      "filePath": "string",
      "title": "string",
      "description": "string",
      "tags": ["string"],
      "folder": "string",
      "location": "string",
      "date": "string",
      "fileSizeKB": "number",
      "exifData": "object",
      "compressedFilePath": "string",
      "hasCompressedImage": "boolean"
    }
  ]
}
```

**说明：**
- 推荐基于配置的 `RecommendTags`（如"卡通"、"CG"）
- 支持排除已显示的照片避免重复
- 返回随机推荐的艺术类照片

## 元数据接口

### 获取所有标签（包含使用次数）

**GET** `/api/metadata/tags`

响应：
```json
{
  "success": true,
  "data": {
    "tags": [
      {
        "name": "string",
        "count": "number"
      }
    ],
    "totalCount": "number"
  }
}
```

### 获取所有文件夹

**GET** `/api/metadata/folders`

响应：
```json
{
  "success": true,
  "data": ["string"]
}
```

### 获取文件夹数量

**GET** `/api/metadata/folders/count`

响应：
```json
{
  "success": true,
  "data": "number"
}
```

### 获取所有地点

**GET** `/api/metadata/locations`

响应：
```json
{
  "success": true,
  "data": ["string"]
}
```

### 获取地点数量

**GET** `/api/metadata/locations/count`

响应：
```json
{
  "success": true,
  "data": "number"
}
```

## 搜索接口

### 搜索照片

**GET** `/api/search`

查询参数：
- `q` (必需): 搜索关键词

响应：
```json
{
  "success": true,
  "data": [
    {
      "id": "number",
      "filePath": "string",
      "title": "string",
      "description": "string",
      "tags": ["string"],
      "folder": "string",
      "location": "string",
      "date": "string",
      "fileSizeKB": "number",
      "exifData": "object"
    }
  ]
}
```

**说明：**
- 支持在标题、描述、标签、文件夹、地点中搜索
- 按日期降序排列

## 通行密钥接口 (WebAuthn)

### 获取注册选项

**POST** `/api/passkey/registration-options`

请求头：
- `Authorization: Bearer {jwt_token}`

响应：
```json
{
  "success": true,
  "data": {
    "rp": {
      "name": "string",
      "id": "string"
    },
    "user": {
      "id": "string",
      "name": "string",
      "displayName": "string"
    },
    "challenge": "string",
    "pubKeyCredParams": [
      {
        "type": "public-key",
        "alg": -7
      }
    ],
    "timeout": 60000,
    "authenticatorSelection": {
      "authenticatorAttachment": "platform",
      "userVerification": "required"
    }
  }
}
```

### 获取认证选项

**POST** `/api/passkey/authentication-options`

请求体：
```json
{
  "username": "string"
}
```

响应：
```json
{
  "success": true,
  "data": {
    "challenge": "string",
    "timeout": 60000,
    "rpId": "string",
    "userVerification": "required"
  }
}
```

### 注册通行密钥

**POST** `/api/passkey/register`

请求体：
```json
{
  "response": {
    "id": "string",
    "rawId": "string",
    "type": "public-key",
    "response": {
      "clientDataJSON": "string",
      "attestationObject": "string"
    }
  },
  "deviceName": "string"
}
```

响应：
```json
{
  "success": true,
  "data": {
    "success": true,
    "message": "通行密钥注册成功"
  }
}
```

### 认证通行密钥

**POST** `/api/passkey/authenticate`

请求体：
```json
{
  "response": {
    "id": "string",
    "rawId": "string",
    "type": "public-key",
    "response": {
      "clientDataJSON": "string",
      "authenticatorData": "string",
      "signature": "string",
      "userHandle": "string"
    }
  },
  "challenge": "string"
}
```

响应：
```json
{
  "success": true,
  "data": {
    "success": true,
    "token": "jwt_token_string",
    "user": {
      "username": "string",
      "name": "string",
      "email": "string"
    }
  }
}
```

### 获取用户通行密钥

**GET** `/api/passkey/user-passkeys`

请求头：
- `Authorization: Bearer {jwt_token}`

响应：
```json
{
  "success": true,
  "data": [
    {
      "id": "number",
      "deviceName": "string",
      "deviceType": "string",
      "createdAt": "string",
      "lastUsedAt": "string"
    }
  ]
}
```

### 删除通行密钥

**DELETE** `/api/passkey/{passkeyId}`

请求头：
- `Authorization: Bearer {jwt_token}`

响应：
```json
{
  "success": true,
  "data": true
}
```

## 错误处理

所有 API 接口使用统一的错误响应格式：

```json
{
  "success": false,
  "error": {
    "code": "string",
    "message": "string",
    "details": "object"
  }
}
```

常见错误码：
- `AUTH_ERROR`: 认证失败
- `VALIDATION_ERROR`: 参数验证失败
- `NOT_FOUND`: 资源不存在
- `UNAUTHORIZED`: 未授权
- `REGISTRATION_ERROR`: 通行密钥注册失败
- `AUTHENTICATION_ERROR`: 通行密钥认证失败
- `SERVER_ERROR`: 服务器内部错误

## 文件存储

### 外部存储

- 文件存储在配置的外部目录中（默认：`E:\图`）
- 文件按文件夹组织
- 通过 `/external/{folder}/{filename}` 访问
- 支持多个存储路径

### 图片压缩

- 自动生成压缩版本
- 压缩质量可配置（默认：60%）
- 存储在 `compressed` 文件夹中
- 保持与原文件相同的目录结构

### EXIF 元数据

- 自动从上传的图片中提取EXIF信息
- 存储在数据库中的JSON格式
- 包含相机信息、拍摄时间、GPS位置等

## 安全说明

### 认证安全
- **推荐使用安全登录接口** 代替传统登录
- **HMAC-SHA256签名**: 确保请求完整性
- **时间戳验证**: 5分钟请求有效期
- **Nonce机制**: 防止重放攻击

### 传输安全
- **强制HTTPS**: 生产环境必须启用HTTPS
- **JWT令牌**: 包含用户身份信息
- **通行密钥**: 支持WebAuthn无密码认证

### 文件安全
- **文件类型验证**: 限制上传文件类型
- **外部存储**: 文件存储在配置的目录中
- **路径验证**: 防止目录遍历攻击

## 开发说明

### 本地开发

1. 启动后端服务器：
   ```bash
   dotnet run
   ```

2. 访问应用：`http://localhost:5085` 或 `https://localhost:7088`

### 配置

关键配置在 `appsettings.json`：
- **JWT设置**: `Jwt:Key`, `Issuer`, `Audience`, `ExpireMinutes`
- **外部存储**: `PhotoStorage:ExternalStoragePaths`
- **图片压缩**: `ImageCompression:Quality`, `EnableCompress`
- **通行密钥**: `Passkey:RelyingParty`
- **推荐标签**: `RecommendTags`

### 数据库

- 使用 SQLite 数据库
- 数据库文件：`tagphotoalbum.db`
- 自动创建和初始化数据
- 支持照片、标签、用户、通行密钥等实体

## 注意事项

1. 所有 API 请求（除认证接口外）都需要在请求头中包含认证 Token
2. 文件上传支持多文件同时上传
3. 分页和筛选参数支持灵活的查询需求
4. 错误处理机制确保应用稳定性
5. 通行密钥功能需要浏览器支持 WebAuthn API
6. 图片压缩功能可配置启用/禁用
7. EXIF 数据自动提取，支持常见图片格式