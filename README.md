# WasiCore 游戏框架示例项目

本项目包含多个使用 WasiCore 框架开发的游戏示例，展示框架的核心功能和最佳实践。

## SDK 位置

本项目使用的 WasiCoreSDK 位置定义在 `src/WasiCoreSDK.props` 文件中。

**当前SDK路径：**
```xml
<!-- src/WasiCoreSDK.props -->
<Project>
  <PropertyGroup>
    <WasiCoreSDKPath>E:/NE/Update/editor-master.spark.xd.com/Res/_m/wasm/wasicoresdk/78/wasicoresdk/</WasiCoreSDKPath>
  </PropertyGroup>
</Project>
```

**注意：** SDK 路径会随安装目录变化，请始终从该文件读取实际路径，不要硬编码。

## 文档位置

### 框架官方文档

位于 SDK 文件夹中：`{WasiCoreSDKPath}/docs/`

### API文档

位于 SDK 文件夹中：`{WasiCoreSDKPath}/api/`

**API列表：**
- 服务端API列表 - 服务器端可用的完整API接口
- 客户端API列表 - 客户端可用的完整API接口

**用途：**
- 快速查找可用的类和方法
- 了解服务端和客户端的API差异
- AI辅助开发时的API参考

**提示：** 使用 `#if CLIENT` 和 `#if SERVER` 条件编译时，参考对应的API列表可避免使用错误的API。

## 查找文档的步骤

1. 读取 `src/WasiCoreSDK.props` 文件获取 SDK 路径
2. 官方文档位于 `{SDK路径}/docs/`
3. API文档位于 `{SDK路径}/api/`（服务端/客户端API列表）
4. 项目示例文档在各游戏目录的 `README.md`

## 快速开始

### 1. 打开项目

使用 Visual Studio 或其他IDE打开 `src/GameEntry.csproj` 或解决方案文件。

### 2. 选择游戏模式

在 `src/GlobalConfig.cs` 中修改测试游戏模式：

```csharp
public static void OnRegisterGameClass()
{
    // ... 配置 AvailableGameModes ...

    // 设置要运行的游戏（修改这一行）
    GameDataGlobalConfig.TestGameMode = ScopeData.GameMode.SuperMario;  // 🍄 运行超级玛丽
    // GameDataGlobalConfig.TestGameMode = ScopeData.GameMode.FlappyBird;  // 🐦 运行FlappyBird
    // GameDataGlobalConfig.TestGameMode = ScopeData.GameMode.Gomoku;      // ⚫ 运行五子棋
}
```

### 3. 验证编译

**验证客户端和服务端编译：**

WasiCore框架同时包含客户端和服务端代码，需要确保两者都能正确编译。

**方法一：使用 IDE 编译**
```bash
# Visual Studio 或 Rider 中选择 "生成解决方案" 或 "Build"
```

**方法二：使用命令行编译**
```bash
# 验证客户端编译
dotnet build src/GameEntry.csproj -c Client-Debug

# 验证服务端编译
dotnet build src/GameEntry.csproj -c Server-Debug
```