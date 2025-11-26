# 📸 TagPhotoAlbum.Server

> 🚀 基于 ASP.NET Core 9.0 的照片管理后端 API

## 🎯 前端项目

**Vue 3 前端应用**: [TagPhotoAlbum](https://github.com/Helloabaka/TagPhotoAlbum) 🌟

## 🚀 构建说明

```bash
# 克隆并运行
git clone https://github.com/Helloabaka/TagPhotoAlbum.Server.git
cd TagPhotoAlbum.Server
dotnet run
```

```bash
# 生成构建
Release.bat
```

**生产环境部署步骤**：
1. 运行 `Release.bat` 完成构建
2. 在构建输出目录中新建 `wwwroot` 文件夹
3. 将前端项目构建的文件放入 `wwwroot` 文件夹

> [!WARNING]
> 如果需要使用通行密钥功能，务必启用 HTTPS 支持并配置有效的 SSL 证书

🌐 **默认地址**：`http://localhost:5085` | `https://localhost:7088`

## ⚙️ 配置文件说明

编辑 `appsettings.json` 或 `appsettings.Production.json` 进行配置：

### ConnectionStrings - 数据库连接
- `DefaultConnection` - SQLite 数据库文件路径

### Server - 服务器配置
- `Urls` - 监听地址，多个地址用分号分隔
  - 本地开发：`http://localhost:5085;https://localhost:7088`
  - 监听任意地址：`http://[::]:5085;https://[::]:7088` 或 `http://0.0.0.0:5085;https://0.0.0.0:7088`
- `Certificate.Path` - 证书文件路径（.pem 或 .pfx）
- `Certificate.KeyPath` - 私钥文件路径（仅 .pem 格式需要）
- `Certificate.Password` - 证书密码（仅 .pfx 格式需要）

### Jwt - 身份验证
- `Key` - JWT 签名密钥（必须 32 位以上随机字符串）
- `Issuer` - 签发者标识
- `Audience` - 受众标识
- `ExpireMinutes` - Token 过期时间（分钟）

### Passkey - 无密码认证
- `RelyingParty.Id` - 依赖方域名（必须与实际访问域名一致）
- `RelyingParty.Name` - 依赖方显示名称

### PhotoStorage - 照片存储
- `ExternalStoragePaths` - 外部存储目录数组

### ImageCompression - 图片压缩
- `Quality` - 压缩质量（0-100）
- `CompressedFolder` - 压缩文件存储文件夹名
- `EnableCompress` - 是否启用压缩

### PhotoSync - 照片同步
- `SyncIntervalMinutes` - 同步间隔（分钟）

### RecommendTags - 推荐标签
- 首页推荐使用的标签数组

### 配置示例

**开发环境 (appsettings.json)**：
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=tagphotoalbum.db"
  },
  "Server": {
    "Urls": "http://localhost:5085;https://localhost:7088",
    "Certificate": {
      "Path": "",
      "KeyPath": "",
      "Password": ""
    }
  },
  "Jwt": {
    "Key": "your-secret-key-at-least-32-characters-long",
    "Issuer": "TagPhotoAlbum.Server",
    "Audience": "TagPhotoAlbum.Client",
    "ExpireMinutes": 43200
  },
  "Passkey": {
    "RelyingParty": {
      "Name": "TagPhotoAlbum",
      "Id": "localhost"
    }
  },
  "PhotoStorage": {
    "ExternalStoragePaths": ["E:\\Photos"]
  },
  "ImageCompression": {
    "Quality": 60,
    "CompressedFolder": "compressed",
    "EnableCompress": true
  },
  "PhotoSync": {
    "SyncIntervalMinutes": 60
  },
  "RecommendTags": ["艺术", "风景", "人物"]
}
```

**生产环境 (appsettings.Production.json)**：
```json
{
  "Server": {
    "Urls": "http://[::]:5085;https://[::]:7088",
    "Certificate": {
      "Path": "/path/to/fullchain.pem",
      "KeyPath": "/path/to/privkey.pem",
      "Password": ""
    }
  },
  "Passkey": {
    "RelyingParty": {
      "Name": "TagPhotoAlbum",
      "Id": "your-domain.com"
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
}
```

> [!INFO]
> **监听任意地址**：使用 `http://[::]:端口` (IPv6) 或 `http://0.0.0.0:端口` (IPv4) 可监听所有网络接口

---

Made with ❤️(Claude Code) using ASP.NET Core 9.0
