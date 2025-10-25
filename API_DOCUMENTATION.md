# Tag Photo Album API 文档

## 概述

本文档描述了 Tag Photo Album 应用的完整 API 接口规范。应用使用 Axios 作为 HTTP 客户端，支持照片管理、标签分类、文件夹组织和地点标注等功能。

## 基础信息

- **基础 URL**: `http://localhost:5085/api`
- **认证方式**: JWT Token
- **数据格式**: JSON
- **超时时间**: 10秒

## 认证接口

### 用户登录（传统方式 - 向后兼容）

**POST** `/auth/login`

请求体：
```json
{
  "username": "string",
  "password": "string"
}
```

响应：
```json
{
  "success": true,
  "user": {
    "username": "string",
    "name": "string",
    "email": "string"
  },
  "token": "jwt_token_string"
}
```

**安全说明：**
- 此接口使用明文密码传输，建议使用安全登录接口
- 仅用于向后兼容，新应用应使用安全登录接口

### 安全用户登录（推荐使用）

**POST** `/auth/secure-login`

**安全特性：**
- HMAC-SHA256 签名验证
- 时间戳防重放攻击
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
  "user": {
    "username": "string",
    "name": "string",
    "email": "string"
  },
  "token": "jwt_token_string",
  "serverTimestamp": "number",
  "nextNonceSeed": "string"
}
```

**前端实现步骤：**
1. 获取当前时间戳和生成随机nonce
2. 使用bcrypt对密码进行哈希
3. 构建签名载荷：`username:passwordHash:timestamp:nonce`
4. 使用HMAC-SHA256计算签名
5. 发送包含所有字段的请求

### 获取Nonce种子

**GET** `/auth/nonce-seed`

响应：
```json
{
  "success": true,
  "data": "random_nonce_seed_string"
}
```

### 验证令牌有效性

**GET** `/auth/validate-token`

请求头：
```
Authorization: Bearer {jwt_token}
```

响应：
```json
{
  "success": true,
  "data": {
    "message": "令牌有效"
  }
}
```

## 照片管理接口

### 获取照片列表（分页）

**GET** `/photos`

查询参数：
- `page` (可选): 页码，默认 1
- `limit` (可选): 每页数量，默认 20
- `folder` (可选): 按文件夹筛选
- `location` (可选): 按地点筛选
- `tags` (可选): 按标签筛选，多个标签用逗号分隔
- `searchQuery` (可选): 搜索关键词

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

**前端使用说明：**
- 标签、文件夹、地点页面使用分页加载，首次加载第1页（20张照片）
- 滚动到底部时自动加载下一页
- 支持懒加载，减少初始加载时间
- 所有照片响应包含压缩图片信息，客户端可根据网络状况选择加载原图或压缩图

### 分页获取照片列表（前端专用）

**前端 API 方法**: `getPhotosPaginated(page, limit, filters)`

**参数说明：**
- `page` (可选): 页码，默认 1
- `limit` (可选): 每页数量，默认 20
- `filters` (可选): 筛选条件对象
  - `tags` (可选): 标签数组，如 `['风景', '人物']`
  - `folder` (可选): 文件夹名称
  - `location` (可选): 地点名称
  - `searchQuery` (可选): 搜索关键词

**使用示例：**
```javascript
// 加载第一页照片
photoApi.getPhotosPaginated(1, 20)

// 按标签筛选
photoApi.getPhotosPaginated(1, 20, {
  tags: ['风景', '人物']
})

// 按文件夹和搜索关键词筛选
photoApi.getPhotosPaginated(1, 20, {
  folder: '旅行',
  searchQuery: '海边'
})
```

**前端集成说明：**
- 该方法是对基础 `/photos` 接口的封装，提供更便捷的筛选参数处理
- 自动将标签数组转换为逗号分隔的字符串
- 支持组合筛选条件
- 主要用于标签、文件夹、地点页面的懒加载功能
- 所有返回的照片数据包含压缩图片信息，便于客户端优化加载

### 获取单个照片

**GET** `/photos/:id`

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
    "compressedFilePath": "string",
    "hasCompressedImage": "boolean"
  }
}
```

### 创建照片

**POST** `/photos`

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
    "compressedFilePath": "string",
    "hasCompressedImage": "boolean"
  }
}
```

### 更新照片

**PUT** `/photos/:id`

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
    "compressedFilePath": "string",
    "hasCompressedImage": "boolean"
  }
}
```

### 删除照片

**DELETE** `/photos/:id`

响应：
```json
{
  "success": true,
  "message": "照片删除成功"
}
```

## 元数据接口

### 获取所有标签（包含使用次数）

**GET** `/metadata/tags`

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

**GET** `/metadata/folders`

响应：
```json
{
  "success": true,
  "data": ["string"]
}
```

### 获取文件夹数量

**GET** `/metadata/folders/count`

响应：
```json
{
  "success": true,
  "data": "number"
}
```

### 获取所有地点

**GET** `/metadata/locations`

响应：
```json
{
  "success": true,
  "data": ["string"]
}
```

### 获取地点数量

**GET** `/metadata/locations/count`

响应：
```json
{
  "success": true,
  "data": "number"
}
```

## 搜索接口

### 搜索照片

**GET** `/search`

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
      "date": "string"
    }
  ]
}
```

### 获取推荐照片

**GET** `/photos/recommend`

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
      "compressedFilePath": "string",
      "hasCompressedImage": "boolean"
    }
  ]
}
```

**前端使用说明：**
- 推荐页面现在支持分页懒加载，可以滚动加载更多推荐照片
- 使用 `excludeIds` 参数避免重复显示已加载的照片
- 每次请求返回随机推荐的艺术类照片（文件夹为"艺术"或标签包含"艺术"、"抽象"）
- 推荐照片同样包含压缩图片信息，优化加载体验

**使用示例：**
```javascript
// 加载任意推荐照片
GET /api/photos/recommend?limit=20

// 加载新的数据但是排除已显示的ID为1,2,3的照片
GET /api/photos/recommend?excludeIds=1,2,3
```

### 上传图片

**POST** `/photos/upload`

请求头：
- `Content-Type: multipart/form-data`

请求体：
- `files` (必需): 图片文件数组

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
      "date": "string"
    }
  ],
  "message": "成功上传 5 张图片"
}
```

### 获取未分类照片（支持分页）

**GET** `/photos/uncategorized`

查询参数：
- `page` (可选): 页码，默认 1
- `limit` (可选): 每页数量，默认 20

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

**前端使用说明：**
- 未分类页面现在支持分页加载，首次只加载第1页（20张照片）
- 滚动到底部时自动加载下一页
- 支持懒加载，减少初始加载时间
- 未分类照片同样包含压缩图片信息，便于快速预览

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
- `PERMISSION_DENIED`: 权限不足
- `SERVER_ERROR`: 服务器内部错误

## 前端集成

### API 服务配置

前端 API 服务位于 `src/api/photoApi.js`，包含以下主要功能：

1. **Axios 实例配置**
   - 基础 URL: `http://localhost:5085/api`
   - 超时时间: 10秒
   - 请求/响应拦截器

2. **API 方法**
   - `login(credentials)`: 用户登录（传统方式）
   - `secureLogin(secureCredentials)`: 安全用户登录（推荐使用）
   - `getNonceSeed()`: 获取nonce种子
   - `validateToken()`: 验证令牌有效性
   - `getPhotos(params)`: 获取照片列表
   - `getPhotosPaginated(page, limit)`: 分页获取照片列表（用于懒加载）
   - `getPhoto(id)`: 获取单个照片
   - `createPhoto(photoData)`: 创建照片
   - `updatePhoto(id, photoData)`: 更新照片
   - `deletePhoto(id)`: 删除照片
   - `getTags()`: 获取所有标签（包含使用次数）
   - `getFolders()`: 获取所有文件夹
   - `getFoldersCount()`: 获取文件夹数量
   - `getLocations()`: 获取所有地点
   - `getLocationsCount()`: 获取地点数量
   - `searchPhotos(query)`: 搜索照片
   - `getRecommendPhotos(page, limit, excludeIds)`: 获取推荐照片（支持分页和去重）
   - `uploadPhotos(formData)`: 上传图片（支持多文件）
   - `getUncategorizedPhotos(page, limit)`: 获取未分类照片（支持分页）

### 状态管理

应用使用 Pinia 进行状态管理：

1. **认证状态** (`src/stores/authStore.js`)
   - 用户信息管理
   - 登录状态维护
   - Token 存储

2. **照片状态** (`src/stores/photoStore.js`)
   - 照片数据管理
   - 标签、文件夹、地点元数据
   - 筛选和搜索功能
   - 推荐照片功能
   - 未分类照片管理
   - 图片上传功能
   - 懒加载功能（分页加载、滚动加载）
   - 加载状态和错误处理
   - **新增方法**：
     - `loadFirstPage()`: 加载第一页数据
     - `loadMorePhotos()`: 加载更多照片（懒加载）
     - `loadMoreUncategorizedPhotos()`: 加载更多未分类照片（懒加载）
     - `getTagsData()`: 按需获取标签数据
     - `getFoldersData()`: 按需获取文件夹数据
     - `getLocationsData()`: 按需获取地点数据

### 主要组件

- **Home.vue**: 主界面，包含照片展示、筛选、推荐和未分类功能
- **Login.vue**: 登录界面
- **Sidebar.vue**: 侧边栏导航，包含标签、文件夹、地点、推荐和未分类页面
- **FilterStatus.vue**: 筛选状态显示组件
- **PhotoGrid.vue**: 瀑布流照片展示组件（支持懒加载）
- **PhotoDialog.vue**: 照片详情对话框组件
- **CategorizeDialog.vue**: 分类对话框组件，支持批量分类操作
- **UploadZone.vue**: 拖拽上传组件，支持单张、多张和文件夹上传
- 使用 Material Web Components 构建 UI

## 开发说明

### 本地开发

1. 启动前端开发服务器：
   ```bash
   npm run dev
   ```

2. 启动后端模拟服务器：
   ```bash
   npm run serve
   ```

3. 访问应用：`http://localhost:3000`

### 生产部署

1. 构建前端：
   ```bash
   npm run build
   ```

2. 配置真实 API 服务器地址

3. 部署构建后的静态文件

### 环境变量

建议使用环境变量配置 API 基础 URL：

```env
VITE_API_BASE_URL=http://your-api-server.com/api
```

## 新功能说明

### 图片压缩功能

- **自动压缩**: 图片上传时自动生成压缩版本，减轻网络负担
- **智能尺寸**: 最大宽度1024px，最大高度768px，保持宽高比
- **质量优化**: 可配置压缩质量（默认80%），使用ImageMagick高质量压缩算法
- **文件组织**: 压缩图片存储在`compressed`文件夹中，保持与原文件相同的目录结构
- **格式支持**: 支持JPG、JPEG、PNG、BMP、WebP、GIF、TIFF等格式

### 安全增强

- **安全登录**: 新增安全登录接口，使用HMAC-SHA256签名验证
- **防重放攻击**: 时间戳 + nonce 机制防止请求重放
- **密码安全**: 前端bcrypt哈希 + 后端验证，避免明文传输
- **请求完整性**: HMAC签名确保请求数据完整性
- **时效性控制**: 5分钟请求有效期，防止过期请求

### 懒加载优化

- **分页加载**: 标签、文件夹、地点页面使用分页加载，首次只加载20张照片
- **滚动加载**: 使用 Intersection Observer 监听滚动，自动加载下一页
- **按需加载**: 侧边栏筛选数据按需加载，减少初始请求
- **状态管理**: 支持加载中状态、没有更多数据提示
- **推荐照片懒加载**: 推荐页面现在支持分页懒加载，可以滚动加载更多推荐照片，避免重复显示

### 刷新功能

- **全局刷新**: Header 添加刷新按钮，支持所有页面刷新
- **状态反馈**: 刷新期间显示旋转动画和禁用状态
- **成功通知**: 刷新后显示绿色 snackbar 通知获得的图片数量

### 图片上传功能

- **支持方式**: 拖拽上传、文件选择器
- **支持格式**: 单张图片、多张图片、整个文件夹
- **文件类型**: JPG、JPEG、PNG、GIF、BMP、WebP、SVG
- **进度显示**: 实时上传进度条
- **结果反馈**: 成功/失败状态通知
- **自动压缩**: 上传时自动生成压缩版本，减轻网络负担

### 未分类照片管理

- **自动识别**: 根据标签、文件夹、地点、标题自动识别未分类照片
- **批量分类**: 通过分类对话框进行批量分类操作
- **进度跟踪**: 显示分类进度 (当前/总数)
- **操作按钮**:
  - 保存并下一张: 保存当前分类并自动进入下一张
  - 下一张: 跳过当前图片，不保存分类
  - 关闭: 退出分类流程

## 安全最佳实践

### 认证安全
- **推荐使用安全登录接口** (`/auth/secure-login`) 代替传统登录
- **前端实现**: 使用bcrypt哈希密码，HMAC-SHA256计算签名
- **时间同步**: 使用服务器返回的时间戳进行时间同步
- **Nonce管理**: 确保每个nonce只使用一次

### 传输安全
- **强制HTTPS**: 生产环境必须启用HTTPS
- **请求签名**: 所有敏感请求应包含HMAC签名
- **时效控制**: 请求应在5分钟内完成处理

## 注意事项

1. 所有 API 请求都需要在请求头中包含认证 Token
2. 照片上传功能需要额外的文件上传接口
3. 分页和筛选参数支持灵活的查询需求
4. 错误处理机制确保应用稳定性
5. 拖拽上传功能需要浏览器支持 File API 和 Directory API
6. 分类对话框支持批量操作，提高分类效率
7. 懒加载功能需要浏览器支持 Intersection Observer API
8. 推荐页面现在支持分页懒加载，可以滚动加载更多推荐照片，避免重复显示
9. 未分类页面现在支持分页懒加载，可以滚动加载更多未分类照片
10. 筛选数据按需加载，减少初始请求数量
11. **图片压缩**: 上传图片时自动生成压缩版本，客户端可根据网络状况选择加载原图或压缩图
12. **压缩配置**: 压缩质量可在 `appsettings.json` 中配置，默认80%
13. **安全要求**: 生产环境必须配置HTTPS和安全密钥