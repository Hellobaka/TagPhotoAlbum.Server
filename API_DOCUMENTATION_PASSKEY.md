# 通行密钥 (Passkey) API 文档

## 概述

通行密钥是一种基于 WebAuthn 标准的现代认证方法，允许用户使用生物识别（指纹、面部识别）或设备 PIN 码进行身份验证，无需密码。

## API 端点

### 1. 获取注册选项

**端点**: `POST /api/passkey/registration-options`

**认证**: 需要 JWT Token (Authorization Header)

**请求体**: 空

**说明**:
- **强制要求**: 必须先通过传统登录接口登录，获取 JWT Token
- 只能为已登录的现有用户添加通行密钥
- 系统会从 JWT Token 中自动获取用户信息

**响应**:
```json
{
  "success": true,
  "data": {
    "challenge": "base64-encoded-challenge",
    "rp": {
      "name": "TagPhotoAlbum",
      "id": "localhost"
    },
    "user": {
      "id": "1",
      "name": "user123",
      "displayName": "张三"
    },
    "pubKeyCredParams": [
      { "type": "public-key", "alg": -7 },
      { "type": "public-key", "alg": -257 }
    ],
    "authenticatorSelection": {
      "authenticatorAttachment": "platform",
      "requireResidentKey": true,
      "userVerification": "preferred"
    },
    "timeout": 60000,
    "attestation": "none"
  }
}
```

### 2. 获取认证选项

**端点**: `POST /api/passkey/authentication-options`

**请求体**:
```json
"user123"
```

**响应**:
```json
{
  "success": true,
  "data": {
    "challenge": "base64-encoded-challenge",
    "timeout": 60000,
    "relyingPartyId": "localhost",
    "allowCredentials": ["credential-id-1", "credential-id-2"],
    "userVerification": "preferred"
  }
}
```

### 3. 注册通行密钥

**端点**: `POST /api/passkey/register`

**请求体**:
```json
{
  "response": {
    "id": "credential-id",
    "rawId": "raw-credential-id",
    "response": {
      "clientDataJSON": "base64-client-data",
      "attestationObject": "base64-attestation-object",
      "transports": ["internal"]
    },
    "type": "public-key"
  },
  "challenge": "original-challenge"
}
```

**响应**:
```json
{
  "success": true,
  "data": {
    "success": true,
    "credentialId": "credential-id"
  }
}
```

### 4. 使用通行密钥认证

**端点**: `POST /api/passkey/authenticate`

**请求体**:
```json
{
  "response": {
    "id": "credential-id",
    "rawId": "raw-credential-id",
    "response": {
      "clientDataJSON": "base64-client-data",
      "authenticatorData": "base64-authenticator-data",
      "signature": "base64-signature",
      "userHandle": "base64-user-handle"
    },
    "type": "public-key"
  },
  "challenge": "original-challenge"
}
```

**响应**:
```json
{
  "success": true,
  "data": {
    "success": true,
    "token": "jwt-token",
    "user": {
      "id": 1,
      "username": "user123",
      "name": "张三",
      "email": "user@example.com"
    }
  }
}
```

### 5. 获取用户通行密钥列表

**端点**: `GET /api/passkey/user-passkeys`

**认证**: 需要 JWT Token

**响应**:
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "userId": 1,
      "credentialId": "credential-id",
      "deviceType": "Platform Authenticator",
      "deviceName": "Default Device",
      "createdAt": "2024-01-01T00:00:00Z",
      "lastUsedAt": "2024-01-01T12:00:00Z",
      "isActive": true
    }
  ]
}
```


### 6. 删除通行密钥

**端点**: `DELETE /api/passkey/{passkeyId}`

**认证**: 需要 JWT Token

**响应**:
```json
{
  "success": true,
  "data": true
}
```

## 使用流程

### 为现有用户添加通行密钥
1. 用户通过传统登录接口登录，获取 JWT Token
2. 调用 `/api/passkey/registration-options` 获取注册选项（需要 Authorization Header）
3. 在客户端使用 `navigator.credentials.create()` 创建通行密钥
4. 调用 `/api/passkey/register` 完成注册

### 使用通行密钥登录
1. 调用 `/api/passkey/authentication-options` 获取认证选项
2. 在客户端使用 `navigator.credentials.get()` 进行认证
3. 调用 `/api/passkey/authenticate` 完成认证并获取 JWT Token

## 与传统登录的关联机制

### 用户关联
- 通行密钥通过 `UserId` 外键与现有用户表关联
- 一个用户可以拥有多个通行密钥（多设备支持）
- 通行密钥认证成功后返回与密码登录相同的 JWT Token

### WebAuthn User 字段说明
根据 WebAuthn 标准，注册选项中的 User 字段包含三个必需属性：

- **`id`** (User ID): 必需，数据库主键（不能是邮箱/手机号），用于唯一标识用户
- **`name`** (User Name): 必需，用于区分凭证的用户名（通常是登录用户名）
- **`displayName`** (User Display Name): 必需，用户友好的显示名称

### 身份验证流程
1. **强制要求**: 必须先通过传统登录才能添加通行密钥
2. **现有用户添加通行密钥**: 用户先通过密码登录，然后添加通行密钥
3. **混合认证**: 用户可以选择使用密码或通行密钥登录

### 数据一致性
- 通行密钥注册时强制验证用户身份（通过 JWT Token）
- 确保通行密钥与正确的用户账户关联
- 支持用户管理自己的通行密钥（查看、删除）

### 安全策略
- **强制认证**: 注册通行密钥必须提供有效的 JWT Token
- **防止滥用**: 无法通过通行密钥创建新用户，只能为现有用户添加
- **身份验证**: 系统从 JWT Token 中自动获取用户信息，确保数据一致性

## 安全特性

- **防钓鱼攻击**: 依赖方 ID 绑定到特定域名
- **防重放攻击**: 使用一次性挑战 (challenge)
- **生物识别**: 支持指纹、面部识别等生物特征
- **设备绑定**: 通行密钥与特定设备绑定

## 配置

在 `appsettings.json` 中添加以下配置：

```json
{
  "Passkey": {
    "RelyingParty": {
      "Name": "TagPhotoAlbum",
      "Id": "localhost"
    }
  }
}
```

**注意**: 在生产环境中，`RelyingParty.Id` 应设置为实际的域名。

## 浏览器支持

通行密钥需要现代浏览器支持：
- Chrome 67+
- Firefox 60+
- Safari 13+
- Edge 79+

## 移动设备支持

- iOS 16+ (iPhone/iPad)
- Android 9+ (需要 Google Play Services)
- Windows Hello (Windows 10+)