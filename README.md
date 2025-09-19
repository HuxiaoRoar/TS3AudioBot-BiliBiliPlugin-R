# 🎵 TS3AudioBot Bilibili （虎啸改）插件

> 一个功能强大的 TS3AudioBot 插件，让您可以在 TeamSpeak 中直接播放 Bilibili 视频的音频内容。
（readme还没写完，先放着）

[![Forked from xxmod/TS3AudioBot-BiliBiliPlugin](https://img.shields.io/badge/Forked%20from-xxmod-blue)](https://github.com/xxmod/TS3AudioBot-BiliBiliPlugin)
[![License](https://img.shields.io/badge/license-MPL2.0-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-3.1-blue.svg)](https://dotnet.microsoft.com/download/dotnet/3.1)

## ✨ 功能特性

- 🎯 **直接播放** - 输入 BV 号即可播放 Bilibili 视频音频
- 📜 **历史记录** - 查看并播放最近观看的视频
- 🔐 **账号登录** - 支持二维码登录和 Cookie 登录
- 📝 **播放队列** - 支持添加音频到播放队列
- 🎬 **多P支持** - 完美支持多分P视频的选择播放
- 👥 **多用户** - 每个用户独立的登录状态和历史记录


## 📦 安装方法

### 方法一：快速安装（推荐）

1. **下载插件文件**

   - 下载 [BilibiliPlugin.dll](https://github.com/HuxiaoRoar/TS3AudioBot-BiliBiliPlugin-R/blob/main/bin/Release/netcoreapp3.1/BilibiliPlugin.dll)

2. **下载代理服务**

   - 下载 [bilibili-referer-proxy](https://github.com/HuxiaoRoar/Bilibili-Referer-Proxy-R/releases/download/1.0.0/Proxy-windows.zip)
3. **文件部署**

   - 将 `BilibiliPlugin.dll` 放置于 `TS3AudioBot/Plugins/` 目录下
   - 解压代理服务到任意目录
4. **权限配置**
   在 `rights.toml` 中添加以下权限：

   ```toml
   "cmd.b"
   "cmd.b.qr"
   "cmd.b.status"
   "cmd.b.login"
   "cmd.b.history"
   "cmd.b.h"
   "cmd.b.hp" 
   "cmd.b.addh" 
   "cmd.b.v" 
   "cmd.b.vp" 
   "cmd.b.add" 
   "cmd.b.addp"
   ```

### 方法二：Release 包安装

1. 前往 [Releases](https://github.com/xxmod/TS3AudioBot-BiliBiliPlugin/releases) 下载最新版本
2. 下载并解压 [bilibili-referer-proxy](https://github.com/xxmod/Bilibili-Referer-Proxy/releases/download/1.0.0/Proxy-windows.zip)
3. 根据包内说明配置 `rights.toml` 和相关文件

## 🚀 快速开始

1. **启动服务**

   ```bash
   # 启动代理服务（必须先启动）
   ./proxy.exe

   # 启动 TS3AudioBot
   ./TS3AudioBot.exe
   ```
2. **加载插件**

   ```
   !plugin lists          # 查看插件列表
   !plugin load [插件编号]  # 加载 Bilibili 插件
   ```
3. **开始使用**

   ```
   !b v BV1UT42167xb    # 播放指定视频
   !b qr                 # 二维码登录
   !b history           # 查看观看历史
   ```

## 📖 详细使用教程

### 🔐 用户登录

#### 二维码登录（推荐）

```
!b qr
```

- 发送命令后会生成二维码链接
- 使用 Bilibili APP 扫描二维码
- 系统会自动检测登录状态并保存凭据
- 每个用户的登录信息独立保存

#### Cookie 登录

```
!b login SESSDATA=你的SESSDATA; bili_jct=你的bili_jct;
```

- 手动输入 Cookie 信息进行登录
- 适合高级用户或批量部署

### 🎵 音频播放

#### 基础播放命令

```
!b bv BV1UT42167xb
```

- 直接播放指定 BV 号的视频音频
- 如果是多P视频，会显示分P列表供选择
- 单P视频会直接开始播放

#### 多P视频播放

当视频包含多个分P时：

```
方法一、（推荐)
用-号链接分P编号
!b bv BV1UT42167xb-1
# 直接播放1p

!b add BV1UT42167xb-1
# 添加1p到播放列表


方法二、
# 首先获取视频信息
!b bv BV1UT42167xb

# 系统会显示：
# 视频包含 3 个分P：
# 1. 第一集：开场
# 2. 第二集：正片
# 3. 第三集：片尾
# 
# 请使用命令 !b p [编号] 播放对应分P。

# 播放指定分P
!b vp 2    # 播放第二集
```

### 📝 播放队列管理

#### 添加到播放队列

```
!b add BV1UT42167xb
```

- 将视频音频添加到播放队列，不会立即播放
- 适合连续播放多个视频

#### 多P视频添加队列

```
# 获取视频信息
!b add BV1UT42167xb

# 添加指定分P到队列
!b addp 2    # 将第二集添加到队列
```

### 📜 历史记录功能

#### 查看观看历史

```
!b history
```

- 显示最近观看的10个视频
- 需要先登录账号
- 显示格式：编号. 视频标题

#### 播放历史视频

```
# 播放历史记录中的视频
!b h 3    # 播放历史记录第3个视频

# 如果历史视频是多P，会显示分P列表
!b hp 2   # 播放历史视频的第2个分P

# 添加历史视频到播放队列
!b addh 3  # 将历史记录第3个视频添加到队列
```

### 💡 使用技巧

1. **批量播放**

   ```
   !b add BV1111111111    # 添加第一个视频
   !b add BV2222222222    # 添加第二个视频
   !b add BV3333333333    # 添加第三个视频
   ```
2. **快速播放历史**

   ```
   !b history    # 查看历史
   !b h 1        # 直接播放第一个
   ```
3. **多P连播**

   ```
   !b bv BV1234567890    # 获取视频信息
   !b vp 1                # 播放第一P
   !b addp 2             # 添加第二P到队列
   !b addp 3             # 添加第三P到队列
   ```

## 📋 完整命令列表

| 命令                  | 参数         | 功能描述               | 示例                                            |
| --------------------- | ------------ | ---------------------- | ----------------------------------------------- |
| `!b qr`      | 无           | 生成二维码进行登录     | `!b qr`                                |
| `!b login`   | Cookie字符串 | 使用Cookie登录         | `!b login SESSDATA=xxx; bili_jct=xxx;` |
| `!b b status`   | 无            |查看当前用户登录状态         | `!b b status` |
| `!b history` | 无           | 查看最近10条观看历史   | `!b history`                           |
| `!b h`       | 历史编号     | 播放历史记录中的视频   | `!b h 3`                               |
| `!b hp`      | 分P编号      | 播放历史视频的指定分P  | `!b hp 2`                              |
| `!b addh`    | 历史编号     | 添加历史视频到播放队列 | `!b addh 3`                            |
| `!b v`      | BV号         | 播放指定BV号的视频     | `!b v BV1UT42167xb`                   |
| `!b v`      | BV号-分P编号         | 播放指定BV分p的视频     | `!b v BV1UT42167xb-1`                   |
| `!b vp`       | 分P编号      | 播放当前视频的指定分P  | `!b vp 2`                               |
| `!b add`     | BV号         | 添加视频到播放队列     | `!b add BV1UT42167xb`                  |
| `!b addp`    | 分P编号      | 添加指定分P到播放队列  | `!b addp 2`                            |

## 🛠️ 编译源代码

### 环境要求

- [.NET Core 3.1 SDK](https://dotnet.microsoft.com/zh-cn/download/dotnet/thank-you/sdk-3.1.426-windows-x64-installer)
- Visual Studio 2019+ 或 VS Code

### 编译步骤

1. **克隆仓库**

   ```bash
   git clone https://github.com/xxmod/TS3AudioBot-BiliBiliPlugin.git
   cd TS3AudioBot-BiliBiliPlugin
   ```
2. **编译项目**

   ```bash
   # Windows
   ./release.bat

   # 或使用 dotnet 命令
   dotnet build --configuration Release
   ```
3. **获取编译文件**
   编译完成后，在 `bin/Release/netcoreapp3.1/` 目录下可找到所有必要文件。

## 🔧 故障排除

### 常见问题

**Q: 提示"未能获取音频流地址"**
A: 检查代理服务 `proxy.exe` 是否正常运行，确保在端口 32181 上监听。

**Q: 登录后仍然无法查看历史记录**
A: 确认 Cookie 信息正确，可尝试重新登录或使用二维码登录。

**Q: 多P视频无法播放指定分P**
A: 确保先使用 `!b bv` 命令获取视频信息，再使用 `!b p` 命令。

**Q: 插件加载失败**
A: 检查是否正确配置了权限，确保 `Newtonsoft.Json.dll` 在正确位置。

### 获取 Cookie 方法

1. **打开浏览器**，访问 [bilibili.com](https://www.bilibili.com) 并登录
2. **打开开发者工具**（F12）
3. **切换到 Application 标签页**
4. **在左侧菜单中找到 Cookies**，展开并选择 `https://www.bilibili.com`
5. **查找以下两个值**：

   - `SESSDATA`：形如 `xxx%2Cxxx%2Cxxx*xx`
   - `bili_jct`：形如 `xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx`
6. **组合 Cookie**：

   ```
   SESSDATA=你的SESSDATA值; bili_jct=你的bili_jct值;
   ```


## 后续计划 
支持批量添加视频合集、视频分p
支持切换音乐后自动更换头像和名字。 


## 🙏 致谢

感谢以下项目和开发者：

- [`TS3AudioBot-BiliBiliPlugin`](https://github.com/xxmod/TS3AudioBot-BiliBiliPlugin) - 原版插件
- [`bilibili-API-collect`](https://github.com/SocialSisterYi/bilibili-API-collect) - 提供详细的 Bilibili API 文档
- [`ZHANGTIANYAO1/TS3AudioBot-NetEaseCloudmusic-plugin`](https://github.com/ZHANGTIANYAO1/TS3AudioBot-NetEaseCloudmusic-plugin) - 提供插件开发参考
- [`Splamy/TS3AudioBot`](https://github.com/Splamy/TS3AudioBot) - 优秀的 TeamSpeak 音频机器人框架

## 📄 许可证

本项目基于 MPL2.0 许可证开源，详见 [LICENSE](https://github.com/xxmod/TS3AudioBot-BiliBiliPlugin/blob/main/LICENSE) 文件。

## 🤝 贡献

欢迎提交 Issue 和 Pull Request 来改进这个项目！

---

**如果这个项目对您有帮助，请给个原项目 ⭐ Star 支持一下！**



